using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for WindowBookmarks.xaml
    /// </summary>
    public partial class WindowBookmarks : Window
    {
        readonly bool writeDebug = true;

        private MainWindow mainWindow;
        private BookmarkManager bookmarkManager;
        public ObservableCollection<BookmarkItem> bookmarks =
            new ObservableCollection<BookmarkItem>();


        private List<int> deleteList = new List<int>();


        public WindowBookmarks(MainWindow mainWindow, BookmarkManager bookmarkManager)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.bookmarkManager = bookmarkManager;
            PopulateListViewBookmarks();
            deleteList.Clear();
            listViewBookmarks.ItemsSource = bookmarks;
        }


        // Populating bookmarks view
        private void PopulateListViewBookmarks()
        {
            bookmarks = bookmarkManager.GetBookmarks();
            listViewBookmarks.ItemsSource = bookmarks;
        }

        private void listViewBookmarks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listViewBookmarks.SelectedItem is BookmarkItem)
            {
                BookmarkItem item = (BookmarkItem)listViewBookmarks.SelectedItem;
                labelTitle.Content = item.Title;
                labelDirectory.Content = item.Directory;
                if (item.Type == 0)
                {
                    labelType.Content = "Folder";
                    labelAndTags.Visibility = Visibility.Hidden;
                    labelOrTags.Visibility = Visibility.Hidden;
                    labelOptionA.Visibility = Visibility.Hidden;
                    labelOptionB.Visibility = Visibility.Hidden;
                    textBlock4.Visibility = Visibility.Hidden;
                    textBlock5.Visibility = Visibility.Hidden;
                    textBlock6.Visibility = Visibility.Hidden;
                }
                else
                {
                    labelType.Content = "Search";
                    labelAndTags.Content = item.AndQueryString;
                    labelOrTags.Content = item.OrQueryString;
                    labelOptionA.Content = item.OptionAString();
                    labelOptionB.Content = item.OptionBString();

                    labelAndTags.Visibility = Visibility.Visible;
                    labelOrTags.Visibility = Visibility.Visible;
                    labelOptionA.Visibility = Visibility.Visible;
                    labelOptionB.Visibility = Visibility.Visible;
                    textBlock4.Visibility = Visibility.Visible;
                    textBlock5.Visibility = Visibility.Visible;
                    textBlock6.Visibility = Visibility.Visible;
                }
            }
            else
            {
                Debug.WriteLineIf(writeDebug,
                    "Error:Item is not selected", this.GetType().Name);
            }
        }



        // Bookmarks operations
        public bool AddToDelete(BookmarkItem bookmark)
        {
            labelTitle.Content = "";
            labelDirectory.Content = "";
            labelType.Content = "";
            labelAndTags.Content = "";
            labelOrTags.Content = "";
            labelOptionA.Content = "";
            labelOptionB.Content = "";


            int i = bookmark.Id;
            if (bookmarks.Remove(bookmark))
            {
                if (i > -1)
                {
                    deleteList.Add(i);
                }
                return true;
            }
            return false;
        }

        private void DeleteAll()
        {
            foreach (int i in deleteList)
            {
                if (i > -1)
                {
                    bookmarkManager.DeleteBookmark(i);
                }
            }
            deleteList.Clear();
        }

        private void AddAll()
        {
            foreach (BookmarkItem bookmark in bookmarks)
            {
                if (bookmark.Id == -1)
                {
                    bookmarkManager.AddBookmark(bookmark);
                }
            }
        }





        // Buttons
        private void buttonNew_Click(object sender, RoutedEventArgs e)
        {
            WindowEditBookmarks windowEditBookmarks = new WindowEditBookmarks(this, true);
            windowEditBookmarks.Show();
        }

        private void buttonEdit_Click(object sender, RoutedEventArgs e)
        {
            if (listViewBookmarks.SelectedItem is BookmarkItem)
            {
                BookmarkItem item = (BookmarkItem)listViewBookmarks.SelectedItem;
                WindowEditBookmarks windowEditBookmarks = new WindowEditBookmarks(this, false, item);
                windowEditBookmarks.Show();
            }
        }

        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            if (listViewBookmarks.SelectedItem is BookmarkItem)
            {
                BookmarkItem item = (BookmarkItem)listViewBookmarks.SelectedItem;
                if (!AddToDelete(item))
                {
                    MessageBox.Show("Can't find this bookmark");
                }
            }
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            DeleteAll();
            AddAll();
            mainWindow.PopulateListViewBookmarks();
            this.Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            deleteList.Clear();
            this.Close();
        }

        private void buttonApply_Click(object sender, RoutedEventArgs e)
        {
            DeleteAll();
            AddAll();
            mainWindow.PopulateListViewBookmarks();
        }
    }
}
