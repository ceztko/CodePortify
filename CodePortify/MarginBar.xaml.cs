using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CodePortify
{
    public partial class MarginBar : UserControl
    {
        public MarginBar()
        {
            InitializeComponent();
        }

        public bool IgnoreProject
        {
            get { return saveOptionChbx.SelectedIndex == (int)SaveOption.IgnoreProject; }
        }

        public NormalizationSettings Settings
        {
            get
            {
                NormalizationSettings ret = new NormalizationSettings();
                if (saveOptionChbx.SelectedIndex == (int)SaveOption.IgnoreExtension)
                {
                    ret.Ignore = true;
                    return ret;
                }

                ret.NewLineCharacter = (NewLineCharacter)newlineCharCbx.SelectedIndex;
                ret.SaveUTF8 = (bool)saveUTF8Btn.IsChecked;
                ret.Operation = (bool)tabifyBtn.IsChecked ? TabifyOperation.Tabify : TabifyOperation.Untabify;

                return ret;
            }
        }

        private void tabifyBtn_Checked(object sender, RoutedEventArgs e)
        {
            untabifyBtn.IsChecked = false;
        }

        private void untabifyBtn_Checked(object sender, RoutedEventArgs e)
        {
            tabifyBtn.IsChecked = false;
        }
    }

    enum SaveOption
    {
        SaveAndApply = 0,
        IgnoreProject,
        IgnoreExtension
    }
}
