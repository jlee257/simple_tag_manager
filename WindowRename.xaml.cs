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

using System.IO;

namespace SimpleTagManager
{
    /// <summary>
    /// Interaction logic for WindowRename.xaml
    /// </summary>
    public partial class WindowRename : Window
    {
        MainWindow mainWindow;
        FileTransferHandler fileTransferHandler;
        FileSystemInfo info;

        public WindowRename(MainWindow mainWindow, FileTransferHandler fileTransferHandler, FileSystemInfo info)
        {
            InitializeComponent();

            this.mainWindow = mainWindow;
            this.fileTransferHandler = fileTransferHandler;
            this.info = info;
            textBox.Text = info.Name;
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            string oldName = info.FullName;
            int end = info.FullName.LastIndexOf(info.Name);
            string newName = System.IO.Path.Combine(info.FullName.Substring(0, end - 1), textBox.Text);
            if (fileTransferHandler.Rename(info, textBox.Text))
            {
                if (info.Attributes.HasFlag(FileAttributes.Directory))
                {
                    DirectoryInfo newInfo = new DirectoryInfo(newName);
                    mainWindow.FindUpdateRow(oldName, newInfo);
                }
                else
                {
                    FileInfo newInfo = new FileInfo(newName);
                    mainWindow.FindUpdateRow(oldName, newInfo);
                }
            }
            this.Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
