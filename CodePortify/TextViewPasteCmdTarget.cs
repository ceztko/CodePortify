using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio;
using System.Windows;

namespace CodePortify
{
    class TextViewPasteCmdTarget : OleCommandTarget
    {
        private TextViewIdentity _identity;
        private TextViewMarginSite _site;

        public TextViewPasteCmdTarget(TextViewMarginSite site, TextViewIdentity identity)
        {
            _site = site;
            _identity = identity;
        }

        public override int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt,
            IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == VSConstants.CMDSETID.StandardCommandSet97_guid &&
                nCmdID == (uint)VSConstants.VSStd97CmdID.Paste)
            {
                // It's a paste command
                string pasteText = Clipboard.GetText(TextDataFormat.UnicodeText)
                    ?? Clipboard.GetText(TextDataFormat.Text);

                if (!string.IsNullOrEmpty(pasteText))
                {
                    NormalizationSettings settings = MainSite.SettingsCache[_identity];
                    if (settings != null)
                    {
                        char[] processed = TextOperations.Normalize(pasteText.ToCharArray(), settings);
                        _site.Paste(new string(processed));
                    }
                }

                return VSConstants.S_OK;
            }

            return exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }
    }
}
