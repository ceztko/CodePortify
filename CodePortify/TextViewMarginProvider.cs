using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text;
using System.Windows;
using System.Reflection;
using Microsoft.VisualStudio.Shell;
using EnvDTE80;
using EnvDTE;
using System.Diagnostics;

namespace CodePortify
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name("Margin")] // Needed
    [MarginContainer(PredefinedMarginNames.Top)]
    [ContentType("any")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    public sealed class TextViewMarginProvider : IWpfTextViewMarginProvider
    {
        [Import]
        private ITextDocumentFactoryService _TextDocumentFactoryService = null;

        [Import]
        private IEditorOperationsFactoryService _OperationsFactory = null;

        [Import]
        private ITextUndoHistoryRegistry _UndoHistoryRegistry = null;

        public TextViewMarginProvider() { }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost textViewHost, IWpfTextViewMargin containerMargin)
        {
            IWpfTextView view = textViewHost.TextView;

            ITextDocument document;
            if (!_TextDocumentFactoryService.TryGetTextDocument(view.TextDataModel.DocumentBuffer, out document))
                return null;

            ITextUndoHistory history;
            if (!_UndoHistoryRegistry.TryGetHistory(view.TextBuffer, out history))
            {
                Debug.Fail("Unexpected: couldn't get an undo history for the given text buffer");
                return null;
            }

            return new TextViewMarginSite(view, document, _OperationsFactory.GetEditorOperations(view), history);
        }
    }
}
