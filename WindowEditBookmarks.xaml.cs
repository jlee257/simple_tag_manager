using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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

namespace SimpleTagManager
{
    /// <summary>
    /// Interaction logic for WindowEditBookmarks.xaml
    /// </summary>
    public partial class WindowEditBookmarks : Window
    {
        MainWindow mainWindow;
        WindowBookmarks windowBookmarks;
        BookmarkManager bookmarkManager;
        BookmarkItem bookmark;
        bool isNew;
        bool addStraight;

        public WindowEditBookmarks(WindowBookmarks windowBookmarks,
            bool isNew, BookmarkItem bookmark = null)
        {
            InitializeComponent();
            this.windowBookmarks = windowBookmarks;
            this.isNew = isNew;
            this.addStraight = false;

            if (!isNew)
            {
                this.bookmark = bookmark;

                textBoxBookmark.Text = bookmark.Title;
                textBoxDirectory.Text = bookmark.Directory;
                if (bookmark.Type == 0)
                {
                    radioButtonFolder.IsChecked = true;
                }
                else
                {
                    radioButtonSearch.IsChecked = true;
                    textBoxAndTags.Text = bookmark.AndQueryString;
                    textBoxOrTags.Text = bookmark.OrQueryString;
                    comboBoxOption1.Text = bookmark.OptionAString();
                    comboBoxOption2.Text = bookmark.OptionBString();
                }
            }
        }

        public WindowEditBookmarks(MainWindow mainWindow, BookmarkManager bookmarkManager, string directory, string and, string or, string optionA, string optionB)
        {
            InitializeComponent();

            isNew = true;
            addStraight = true;
            this.mainWindow = mainWindow;
            this.bookmarkManager = bookmarkManager;
            textBoxDirectory.Text = (directory == null) ? "" : directory;
            radioButtonSearch.IsChecked = true;
            textBoxAndTags.Text = (and == null) ? "" : and;
            textBoxOrTags.Text = (or == null) ? "" : or;
            comboBoxOption1.Text = (optionA == null) ? "" : optionA;
            comboBoxOption2.Text = (optionB == null) ? "" : optionB;
        }



        private void radioButtonFolder_Checked(object sender, RoutedEventArgs e)
        {
            textBoxAndTags.IsEnabled = false;
            textBoxOrTags.IsEnabled = false;
            comboBoxOption1.IsEnabled = false;
            comboBoxOption2.IsEnabled = false;
        }

        private void radioButtonSearch_Checked(object sender, RoutedEventArgs e)
        {

            textBoxAndTags.IsEnabled = true;
            textBoxOrTags.IsEnabled = true;
            comboBoxOption1.IsEnabled = true;
            comboBoxOption2.IsEnabled = true;
        }


        private int OptionAtoInt()
        {
            int optionA = -1;
            if (comboBoxOption1.Text.Equals("Search all subfolders"))
            {
                optionA = 0;
            }
            else if (comboBoxOption1.Text.Equals("Search current folder"))
            {
                optionA = 1;
            }
            return optionA;
        }

        private int OptionBtoInt()
        {
            int optionB = -1;
            if (comboBoxOption2.Text.Equals("Search files and folders"))
            {
                optionB = 0;
            }
            else if (comboBoxOption2.Text.Equals("Search files only"))
            {
                optionB = 1;
            }
            else if (comboBoxOption2.Text.Equals("Search folders only"))
            {
                optionB = 2;
            }
            return optionB;

        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxBookmark.Text == null||
                textBoxBookmark.Text.Trim() == "" ||
                textBoxBookmark.Text.Trim().Length > 40)
            {
                MessageBox.Show("Invalid Title (too long or to short");
                return;
            }
            
            if (!Directory.Exists(textBoxDirectory.Text) && !File.Exists(textBoxDirectory.Text))
            {
                MessageBox.Show("Folder does not exist\n" + textBoxDirectory.Text);
                return;
            }

            if (!isNew && !windowBookmarks.AddToDelete(bookmark))
            {
                MessageBox.Show("Can't find this bookmark");
                this.Close();
                return;
            }

            if (radioButtonSearch.IsChecked == true &&
                textBoxAndTags.Text.Trim().Equals("") &&
                textBoxOrTags.Text.Trim().Equals(""))
            {
                MessageBox.Show("No search input");
                return;
            }

            if (addStraight)
            {
                buttonOKAddStraight();
            }
            else
            {
                buttonOKAddToBookmarkManager();
            }

            
        }

        private void buttonOKAddStraight()
        {
            if (radioButtonFolder.IsChecked == true)
            {
                BookmarkItem newBookmark = new BookmarkItem(
                    -1,
                    textBoxBookmark.Text.Trim(),
                    textBoxDirectory.Text,
                    0);
                bookmarkManager.AddBookmark(newBookmark);
                mainWindow.PopulateListViewBookmarks();
            }
            else if (radioButtonSearch.IsChecked == true)
            {
                BookmarkItem newBookmark = new BookmarkItem(
                    -1,
                    textBoxBookmark.Text.Trim(),
                    textBoxDirectory.Text,
                    1,
                    textBoxAndTags.Text,
                    textBoxOrTags.Text,
                    OptionAtoInt(),
                    OptionBtoInt());
                bookmarkManager.AddBookmark(newBookmark);
                mainWindow.PopulateListViewBookmarks();
            }
            else
            {
                MessageBox.Show("UnknownError");
                return;
            }
            this.Close();
        }

        private void buttonOKAddToBookmarkManager()
        {
            if (radioButtonFolder.IsChecked == true)
            {
                BookmarkItem newBookmark = new BookmarkItem(
                    -1,
                    textBoxBookmark.Text.Trim(),
                    textBoxDirectory.Text,
                    0);
                windowBookmarks.bookmarks.Add(newBookmark);
            }
            else if (radioButtonSearch.IsChecked == true)
            {
                BookmarkItem newBookmark = new BookmarkItem(
                    -1,
                    textBoxBookmark.Text.Trim(),
                    textBoxDirectory.Text,
                    1,
                    textBoxAndTags.Text,
                    textBoxOrTags.Text,
                    OptionAtoInt(),
                    OptionBtoInt());
                windowBookmarks.bookmarks.Add(newBookmark);
            }
            else
            {
                MessageBox.Show("UnknownError");
                return;
            }
            this.Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd =
                new System.Windows.Forms.FolderBrowserDialog();

            fbd.Description = "Folder Browser";
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBoxDirectory.Text = fbd.SelectedPath;
            }
        }
    }



}
