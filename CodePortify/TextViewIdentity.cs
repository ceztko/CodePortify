// Copyright (c) 2013 Francesco Pretto
// This file is subject to to the MS-PL license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using System.IO;
using Microsoft.VisualStudio.Shell;

namespace CodePortify
{
    public class TextViewIdentity
    {
        private HiearchyItemPair _pair;
        private IWpfTextView _WpfTextView;
        private IVsTextView _VsTextView;

        private string _FilePath;

        public TextViewIdentity(IVsTextView vstextView, IWpfTextView wpfTextView)
        {
            IVsWindowFrame windowFrame = vstextView.GetWindowFrame();

            _pair = windowFrame.GetHiearchyItemPair();

            ITextDocument document = wpfTextView.TextBuffer.GetTextDocument();
            _FilePath = document.FilePath;

            _VsTextView = vstextView;
            _WpfTextView = wpfTextView;
        }

        public string FilePath
        {
            get { return _FilePath; }
        }

        public string Extension
        {
            get { return Path.GetExtension(_FilePath); }
        }

        public IVsHierarchy Hierarchy
        {
            get { return _pair.Hierarchy; }
        }

        public uint ItemId
        {
            get { return _pair.ItemId; }
        }

        public IWpfTextView WpfTextView
        {
            get { return _WpfTextView; }
        }

        public IVsTextView VsTextView
        {
            get { return _VsTextView; }
        }
    }
}
