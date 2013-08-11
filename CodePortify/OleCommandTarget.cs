using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.OLE.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TextManager.Interop;

namespace CodePortify
{
    public class OleCommandTarget : IOleCommandTarget
    {
        private IOleCommandTarget _Next;

        public OleCommandTarget()
        {
            _Next = null;
        }

        public virtual int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (_Next == null)
                // CHECK-ME or exeception?
                return VSConstants.S_OK;
                
            return exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        public virtual int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (_Next == null)
                // CHECK-ME or exeception?
                return VSConstants.S_OK;

            return queryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        #region Private helpers

        protected int exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            return _Next.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        protected int queryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return _Next.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        /// <summary>
        /// Get the char for a <see cref="VSConstants.VSStd2KCmdID.TYPECHAR"/> command.
        /// </summary>
        /// <param name="pvaIn">The "pvaIn" arg passed to <see cref="Exec"/>.</param>
        protected char GetTypedChar(IntPtr pvaIn)
        {
            return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
        }

        #endregion

        #region Properties

        public IOleCommandTarget Next
        {
            get { return _Next; }
            internal set { _Next = value; }
        }

        #endregion // Properties
    }

    public static class CommandTargetUtils
    {
        public static void AddCommandFilter(this IVsTextView textView, OleCommandTarget filter)
        {
            IOleCommandTarget next;
            int hr = textView.AddCommandFilter(filter, out next);
            Marshal.ThrowExceptionForHR(hr);
            filter.Next = next;
        }
    }
}
