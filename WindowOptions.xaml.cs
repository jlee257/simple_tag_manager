using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.Diagnostics;

namespace SimpleTagManager
{
    /// <summary>
    /// Interaction logic for WindowOptions.xaml
    /// </summary>
    public partial class WindowOptions : Window
    {
        readonly bool writeDebug = false;
        private Dictionary<string, string> customDefaultPrograms;


        public WindowOptions(Dictionary<string, string> customDefaultPrograms)
        {
            InitializeComponent();
            InitializeSettings();
            this.customDefaultPrograms = customDefaultPrograms;
        }



        private void InitializeSettings()
        {
            Debug.WriteLineIf(writeDebug,
                "InitializeSettings() is called", this.GetType().Name);
            Debug.Indent();

            bool showHidden = Properties.Settings.Default.ShowHidden;
            if (showHidden)
            {
                checkBoxShowHidden.IsChecked = true;
                Debug.WriteLineIf(writeDebug,
                    "ShowHidden Checked", this.GetType().Name);
            }
            else
            {
                checkBoxShowHidden.IsChecked = false;
                Debug.WriteLineIf(writeDebug,
                    "ShowHidden Unchecked", this.GetType().Name);
            }


            bool useCustomProgram = Properties.Settings.Default.UseCustomProgram;
            if (useCustomProgram)
            {
                checkBoxUseCustomDefault.IsChecked = true;
                Debug.WriteLineIf(writeDebug,
                    "ShowHidden Checked", this.GetType().Name);
            }
            else
            {
                checkBoxUseCustomDefault.IsChecked = false;
                Debug.WriteLineIf(writeDebug,
                    "ShowHidden Unchecked", this.GetType().Name);
            }


            Debug.Unindent();
        }






        // Options button actions

        private void buttonCustomizeDefault_Click(object sender, RoutedEventArgs e)
        {
            WindowCustomizeProgram w = new WindowCustomizeProgram(customDefaultPrograms);
            w.Show();
        }


        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ShowHidden = (checkBoxShowHidden.IsChecked == true);
            Properties.Settings.Default.UseCustomProgram = (checkBoxUseCustomDefault.IsChecked == true);
            Debug.WriteLineIf(writeDebug,
                "buttonOK_Click is called. Settings has been saved", this.GetType().Name);
            Properties.Settings.Default.Save();
            this.Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLineIf(writeDebug,
                "buttonCancel_Click is called", this.GetType().Name);
            this.Close();
        }
    }
}
