// Copyright (c) 2013 Francesco Pretto
// This file is subject to the MS-PL license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodePortify
{
    public class NormalizationSettings
    {
        public bool Ignore;
        public TabifyOperation Operation;
        public bool SaveUTF8;
        public NewLineCharacter NewLineCharacter;
    }

    public enum TabifyOperation
    {
        Tabify,
        Untabify
    }

    public enum NewLineCharacter
    {
        LF,
        CR,
        CRLF
    }

    public partial class Extensions
    {
        public static string ToNewlineString(this NewLineCharacter character)
        {
            switch (character)
            {
                case NewLineCharacter.CRLF:
                    return "\r\n";
                case NewLineCharacter.LF:
                    return "\n";
                case NewLineCharacter.CR:
                    return "\r";
                default:
                    throw new Exception();
            }
        }
    }
}
