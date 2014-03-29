// Copyright (c) 2009-2012 Noah Richards
// Copyright (c) 2013 Francesco Pretto
// This file is subject to to the MS-PL license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace CodePortify
{
    public class TextOperations
    {
        public static char[] Normalize(char[] text, NormalizationSettings settings)
        {
            return normalize(text, settings.NewLineCharacter);
        }

        public static bool Normalize(ITextView textView, NormalizationSettings settings)
        {
            ITextBuffer buffer = textView.TextDataModel.DocumentBuffer;
            int tabSize = textView.Options.GetOptionValue(DefaultOptions.TabSizeOptionId);
            string newlineReplacement = settings.NewLineCharacter.ToNewlineString();

            if (settings.Operation == TabifyOperation.Tabify)
                return tabify(buffer, tabSize, newlineReplacement);
            else
                return untabify(buffer, tabSize, newlineReplacement);
        }

        private static char[] normalize(char[] text, NewLineCharacter replacement)
        {
            List<char> normalized = new List<char>(text.Length);
            bool previousCR = false;
            for (int it = 0; it < text.Length; it++)
            {
                char c = text[it];
                switch (c)
                {
                    case '\r':
                    {
                        if (previousCR)
                        {
                            addNewlineCharacter(normalized, replacement);
                            continue;
                        }
                        else
                        {
                            previousCR = true;
                            continue;
                        }
                    }
                    case '\n':
                    {
                        if (previousCR)
                        {
                            addNewlineCharacter(normalized, replacement);
                            previousCR = false;
                            continue;
                        }
                        else
                        {
                            addNewlineCharacter(normalized, replacement);
                            continue;
                        }
                    }
                    default:
                    {
                        if (previousCR)
                        {
                            addNewlineCharacter(normalized, replacement);
                            previousCR = false;
                        }

                        normalized.Add(c);
                        continue;
                    }
                }
            }

            if (previousCR)
                addNewlineCharacter(normalized, replacement);

            return normalized.ToArray();
        }

        public static bool tabify(ITextBuffer textBuffer, int tabSize, string newlineReplacement)
        {
            using (ITextEdit edit = textBuffer.CreateEdit())
            {
                foreach (var line in edit.Snapshot.Lines)
                {
                    bool tabsAfterSpaces = false;
                    int column = 0;
                    int spanLength = 0;
                    int countOfLargestRunOfSpaces = 0;
                    int countOfCurrentRunOfSpaces = 0;

                    for (int i = line.Start; i < line.End; i++)
                    {
                        char ch = edit.Snapshot[i];

                        // Increment column or break, depending on the character
                        if (ch == ' ')
                        {
                            countOfCurrentRunOfSpaces++;
                            countOfLargestRunOfSpaces = Math.Max(countOfLargestRunOfSpaces, countOfCurrentRunOfSpaces);

                            column++;
                            spanLength++;
                        }
                        else if (ch == '\t')
                        {
                            if (countOfLargestRunOfSpaces > 0)
                                tabsAfterSpaces = true;

                            countOfCurrentRunOfSpaces = 0;

                            column += tabSize - (column % tabSize);
                            spanLength++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Only do a replace if this will have any effect
                    if (tabsAfterSpaces || countOfLargestRunOfSpaces >= tabSize)
                    {
                        int tabCount = column / tabSize;
                        int spaceCount = column % tabSize;

                        string newWhitespace = string.Format("{0}{1}",
                                                             new string('\t', tabCount),
                                                             new string(' ', spaceCount));

                        if (!edit.Replace(new Span(line.Start, spanLength), newWhitespace))
                            return false;
                    }

                    // Do newline normalization
                    if (line.LineBreakLength != 0 && line.GetLineBreakText() != newlineReplacement)
                    {
                        bool success = edit.Replace((int)line.End, line.LineBreakLength, newlineReplacement);
                        if (!success)
                            return false;
                    }
                }

                edit.Apply();

                return !edit.Canceled;
            }
        }

        private static bool untabify(ITextBuffer buffer, int tabSize, string newlineReplacement)
        {
            using (ITextEdit edit = buffer.CreateEdit())
            {
                foreach (var line in edit.Snapshot.Lines)
                {
                    bool hasTabs = false;
                    int column = 0;
                    int spanLength = 0;

                    for (int i = line.Start; i < line.End; i++)
                    {
                        char ch = edit.Snapshot[i];

                        if (ch == '\t')
                        {
                            hasTabs = true;

                            column += tabSize - (column % tabSize);
                            spanLength++;
                        }
                        else if (ch == ' ')
                        {
                            spanLength++;
                            column++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Only do a replace if this will have any effect
                    if (hasTabs)
                    {
                        string newWhitespace = new string(' ', column);

                        if (!edit.Replace(new Span(line.Start, spanLength), newWhitespace))
                            return false;
                    }

                    // Do newline normalization
                    if (line.LineBreakLength != 0 && line.GetLineBreakText() != newlineReplacement)
                    {
                        bool success = edit.Replace((int)line.End, line.LineBreakLength, newlineReplacement);
                        if (!success)
                            return false;
                    }
                }

                edit.Apply();
                return !edit.Canceled;
            }
        }

        private static void addNewlineCharacter(List<char> normalized, NewLineCharacter character)
        {
            switch (character)
            {
                case NewLineCharacter.CRLF:
                    normalized.Add('\r');
                    normalized.Add('\n');
                    break;
                case NewLineCharacter.LF:
                    normalized.Add('\n');
                    break;
                case NewLineCharacter.CR:
                    normalized.Add('\r');
                    break;
            }
        }
    }
}
