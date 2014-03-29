// Copyright (c) 2009-2012 Noah Richards
// Copyright (c) 2013 Francesco Pretto
// This file is subject to to the MS-PL license

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Reflection;
using System.Text;
using System.IO;

namespace CodePortify
{
    sealed class TextViewMarginSite : ContentControl, IWpfTextViewMargin
    {
        #region Members

        public const string MARGIN_BAR_NAME = "CodePortifyBar";
        public const int MARGIN_BAR_HEIGHT = 27;
        public const int ANIMATION_DURATION = 175;

        private static MethodInfo _insertText;

        private bool _disposed;

        private IWpfTextView _textView;
        private ITextDocument _document;
        private IEditorOperations _operations;
        private ITextUndoHistory _undoHistory;
        private MarginBar _marginBar;

        private TextViewIdentity _identity;
        private NormalizationSettings _settings;

        #endregion // Members

        #region Constructors

        static TextViewMarginSite()
        {
            Type type = Type.GetType("Microsoft.VisualStudio.Text.Operations.Implementation.EditorOperations, Microsoft.VisualStudio.Platform.VSEditor, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            
            // Get the non-public delegate that will allow to correctly paste inside
            // an EditorOperations object
            _insertText = type.GetMethod(
                "InsertText", BindingFlags.Instance | BindingFlags.NonPublic, null,
                 new Type[] { typeof(string), typeof(bool), typeof(string) },
                new ParameterModifier[0]);
        }

        public TextViewMarginSite(IWpfTextView textView, ITextDocument document,
            IEditorOperations editorOperations, ITextUndoHistory undoHistory)
        {
            _disposed = false;

            _textView = textView;
            _document = document;
            _operations = editorOperations;
            _undoHistory = undoHistory;

            MarginBar marginBar = new MarginBar();
            marginBar.Accept.Click += new RoutedEventHandler(Accept_Click);
            marginBar.Hide.Click += new RoutedEventHandler(Hide_Click);

            // Initially hidden
            this.Height = 0;
            this.Content = marginBar;
            this.Name = MARGIN_BAR_NAME;

            textView.GotAggregateFocus += new EventHandler(textView_GotAggregateFocus);

            _marginBar = marginBar;
        }

        #endregion // Constructors

        #region Inquiry

        public void Paste(string text)
        {
            // This is the correct way to programmatically paste as of VS2010/VS2012
            _insertText.Invoke(_operations, new object[] { text, true, "Paste" });
        }

        #endregion // Inquiry

        #region Event Handlers

        void Hide_Click(object sender, RoutedEventArgs e)
        {
            closeMarginBar();
        }

        void Accept_Click(object sender, RoutedEventArgs e)
        {
            closeMarginBar();

            if (_marginBar.IgnoreProject)
                return;

            // Get and store new settings
            NormalizationSettings settings = _marginBar.Settings;
            MainSite.SettingsCache[_identity] = settings;
            _settings = _marginBar.Settings;

            if (settings.Ignore)
                return;

            configureForNormalization();
            closeMarginBar();
        }

        void _textView_Closed(object sender, EventArgs e)
        {
            disableMarginBar();
        }

        void _document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if ((e.FileActionType & FileActionTypes.ContentLoadedFromDisk) != 0 ||
                (e.FileActionType & FileActionTypes.ContentSavedToDisk) != 0)
            {
                normalize();
            }
        }

        void textView_GotAggregateFocus(object sender, EventArgs e)
        {
            _textView.GotAggregateFocus -= textView_GotAggregateFocus;

            IVsTextView vsTextView = MainSite.EditorAdaptersFactory.GetViewAdapter(_textView);
            TextViewIdentity identity = new TextViewIdentity(vsTextView, _textView);
            _identity = identity;

            NormalizationSettings settings = MainSite.SettingsCache[identity];
            if (settings == null)
            {
                _textView.Closed += new EventHandler(_textView_Closed);
                showMarginBar();
                return;
            }

            _settings = settings;

            if (settings.Ignore)
                return;

            configureForNormalization();
        }

        #endregion // Event Handlers

        #region Private methods

        void disableMarginBar()
        {
            // Safe general clean-up
            _document.FileActionOccurred -= _document_FileActionOccurred;
            _textView.GotAggregateFocus -= textView_GotAggregateFocus;
            _textView.Closed -= _textView_Closed;
            _identity = null;
        }

        void closeMarginBar()
        {
            // Since we're going to be closing, make sure focus is back in the editor
            _textView.VisualElement.Focus();

            changeHeightTo(0);
        }

        void showMarginBar()
        {
            changeHeightTo(MARGIN_BAR_HEIGHT);
            string extension = _identity.Extension == Path.GetFileName(_identity.FilePath)
                ? _identity.Extension : "." + _identity.Extension;
            _marginBar.ExtensionLbl.Content = extension;
        }

        void changeHeightTo(int newHeight)
        {
            if (_textView.Options.GetOptionValue(DefaultWpfViewOptions.EnableSimpleGraphicsId))
            {
                this.Height = newHeight;
            }
            else
            {
                DoubleAnimation animation = new DoubleAnimation(this.Height, newHeight,
                    new Duration(TimeSpan.FromMilliseconds(ANIMATION_DURATION)));
                Storyboard.SetTarget(animation, this);
                Storyboard.SetTargetProperty(animation, new PropertyPath(StackPanel.HeightProperty));

                Storyboard storyboard = new Storyboard();
                storyboard.Children.Add(animation);

                storyboard.Begin(this);
            }
        }

        void performActionInUndo(Func<bool> action)
        {
            ITrackingPoint anchor = _textView.TextSnapshot.CreateTrackingPoint(
                _textView.Selection.AnchorPoint.Position, PointTrackingMode.Positive);
            ITrackingPoint active = _textView.TextSnapshot.CreateTrackingPoint(
                _textView.Selection.ActivePoint.Position, PointTrackingMode.Positive);
            bool empty = _textView.Selection.IsEmpty;
            TextSelectionMode mode = _textView.Selection.Mode;

            using (var undo = _undoHistory.CreateTransaction("Normalization"))
            {
                _operations.AddBeforeTextBufferChangePrimitive();

                if (!action())
                {
                    undo.Cancel();
                    return;
                }

                ITextSnapshot after = _textView.TextSnapshot;

                _operations.SelectAndMoveCaret(new VirtualSnapshotPoint(anchor.GetPoint(after)), 
                                               new VirtualSnapshotPoint(active.GetPoint(after)), 
                                               mode, 
                                               EnsureSpanVisibleOptions.ShowStart);

                _operations.AddAfterTextBufferChangePrimitive();

                undo.Complete();
            }
        }

        void normalize()
        {
            performActionInUndo(() =>
            {
                return TextOperations.Normalize(_textView, _settings);
            });
        }

        private void configureForNormalization()
        {
            TextViewPasteCmdTarget filter = new TextViewPasteCmdTarget(this, _identity);
            _identity.VsTextView.AddCommandFilter(filter);

            _textView.Options.SetOptionValue(DefaultOptions.NewLineCharacterOptionId,
                _settings.NewLineCharacter.ToNewlineString());
            _textView.Options.SetOptionValue(DefaultOptions.ReplicateNewLineCharacterOptionId, false);

            if (_settings.Operation == TabifyOperation.Tabify)
                _textView.Options.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, false);
            else
                _textView.Options.SetOptionValue(DefaultOptions.ConvertTabsToSpacesOptionId, true);

            if (_settings.SaveUTF8)
            {
                // Set UTF-8 without BOM encoding
                var utf8WithoutBom = new UTF8Encoding(false);
                _document.Encoding = utf8WithoutBom;
            }

            _document.FileActionOccurred += new EventHandler<TextDocumentFileActionEventArgs>(_document_FileActionOccurred);

        }

        #endregion // Private methods

        #region IWpfTextViewMargin Members

        public FrameworkElement VisualElement
        {
            get { return this; }
        }

        #endregion

        #region ITextViewMargin Members

        public double MarginSize
        {
            get { return this.ActualHeight; }
        }

        public bool Enabled
        {
            get { return !_disposed; }
        }

        public ITextViewMargin GetTextViewMargin(string marginName)
        {
            return (marginName == TextViewMarginSite.MARGIN_BAR_NAME) ? (IWpfTextViewMargin)this : null;
        }

        public void Dispose()
        {
            disableMarginBar();
        }

        #endregion
    }
}
