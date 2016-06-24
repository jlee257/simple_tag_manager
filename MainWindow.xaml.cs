using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.IO;
using System.Diagnostics;
using System.Configuration;
using System.Windows.Controls.Primitives;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SimpleTagManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly bool writeDebug = true;
        readonly bool writeDebugTreeView = true;
        readonly bool writeDebugListViewMain = true;
        readonly bool writeDebugListViewItemActions = true;

        private LinkedList<DirectoryInfo> directoryHistory = new LinkedList<DirectoryInfo>();
        private TagManager tagManager;
        private BookmarkManager bookmarkManager;
        private FileTransferHandler fileTransferHandler;

        public ObservableCollection<ListViewMainItem> listViewMainItems =
            new ObservableCollection<ListViewMainItem>();

        public ObservableCollection<BookmarkItem> bookmarks =
            new ObservableCollection<BookmarkItem>();

        public Dictionary<string, string> customDefaultPrograms =
            new Dictionary<string, string>();




        public MainWindow()
        {
            InitializeComponent();
            PopulateTreeView();
            
            tagManager = new TagManager();
            bookmarkManager = new BookmarkManager();
            fileTransferHandler = new FileTransferHandler(tagManager);
            PopulateListViewBookmarks();


            listViewMain.ItemsSource = listViewMainItems;
            listViewBookmarks.ItemsSource = bookmarks;
            customDefaultPrograms = bookmarkManager.GetCustomDefaultPrograms();
        }
        
        

        // Pouplates treeViewDirectories.
        private void PopulateTreeView()
        {
            Debug.WriteLineIf(writeDebug && writeDebugTreeView,
                    "PopulateTreeView is called", this.GetType().Name);

            DriveInfo[] driveInfos = DriveInfo.GetDrives();

            TreeViewItem rootNode;
            foreach(DriveInfo driveInfo in driveInfos)
            {
                rootNode = new TreeViewItem()
                {
                    Header = driveInfo.Name,
                    Tag = driveInfo.RootDirectory,
                };
                rootNode.Items.Add("not loaded");
                treeViewDirectories.Items.Add(rootNode);

                Debug.WriteLineIf(writeDebug && writeDebugTreeView,
                    "Adding a root node (" + driveInfo.Name +
                    ") to treeViewDirectories", this.GetType().Name);
            }
        }
        
        private void treeViewDirectories_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem expandedItem = (TreeViewItem)e.Source;
            TreeViewItem subItem;
            bool showHidden = Properties.Settings.Default.ShowHidden;

            if((expandedItem.Items.Count == 1) && expandedItem.Items[0] is string)
            {
                expandedItem.Items.Clear();
                foreach(DirectoryInfo subDirectory in
                    ((DirectoryInfo)expandedItem.Tag).GetDirectories())
                {
                    try
                    {
                        if (subDirectory.Attributes.HasFlag(FileAttributes.System) ||
                        ((!showHidden) && subDirectory.Attributes.HasFlag(FileAttributes.Hidden)))
                        {
                            Debug.WriteLineIf(writeDebug && writeDebugTreeView,
                                "(" + subDirectory.FullName + ") is not added", this.GetType().Name);
                            continue;
                        }


                        subItem = new TreeViewItem()
                        {
                            Header = subDirectory.Name,
                            Tag = subDirectory
                        };

                        foreach (DirectoryInfo subSubDirectory in subDirectory.GetDirectories())
                        {
                            subItem.Items.Add("not loaded");
                            break;
                        }

                        if (subDirectory.Attributes.HasFlag(FileAttributes.Hidden))
                            subItem.Foreground = Brushes.Silver;
                        expandedItem.Items.Add(subItem);

                        Debug.WriteLineIf(writeDebug && writeDebugTreeView,
                            "(" + subDirectory.FullName + ") is added", this.GetType().Name);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Debug.WriteLine(ex.Message, "Error Catched");
                    }
                }
            }
            
        }



        // Populates listViewMain.
        private void treeViewDirectories_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            Debug.WriteLineIf(writeDebug && writeDebugListViewMain,
                    "TreeView SelectedITemChanged is triggered on (" +
                    sender.GetType().Name + ", " +
                    ((DirectoryInfo)((TreeViewItem)((TreeView)sender).SelectedItem).Tag).FullName + ")"
                    , this.GetType().Name);

            DirectoryInfo directoryInfo = (DirectoryInfo)((TreeViewItem)((TreeView)sender).SelectedItem).Tag;
            PopulateListViewMain(directoryInfo);
        }

        private void PopulateListViewMain(DirectoryInfo directoryInfo)
        {

            if (directoryInfo == null) { return; }

            //listViewMain.Items.Clear();
            listViewMainItems.Clear();
            AddHistory(directoryInfo);

            Debug.WriteLineIf(writeDebug && writeDebugListViewMain,
                    "PopulateListViewMain is called with (" +
                    directoryInfo.FullName + ")", this.GetType().Name);
            Debug.Indent();

            textBoxPath.Text = directoryInfo.FullName;
            ribbonButtonBookmark.IsEnabled = false;

            bool showHidden = Properties.Settings.Default.ShowHidden;

            foreach (DirectoryInfo subDirectory in directoryInfo.GetDirectories())
            {
                if (subDirectory.Attributes.HasFlag(FileAttributes.System) ||
                       ((!showHidden) && subDirectory.Attributes.HasFlag(FileAttributes.Hidden)))
                {
                    Debug.WriteLineIf(writeDebug && writeDebugListViewMain,
                                "(" + subDirectory.FullName + ") is not added", this.GetType().Name);
                    continue;
                }

                listViewMainItems.Add(new ListViewMainItem(subDirectory, tagManager.GetTags(subDirectory)));
                //listViewMain.Items.Add());
                Debug.WriteLineIf(writeDebug && writeDebugListViewMain,
                                "(" + subDirectory.FullName + ") has been added", this.GetType().Name);

            }

            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                if (file.Attributes.HasFlag(FileAttributes.System) ||
                       ((!showHidden) && file.Attributes.HasFlag(FileAttributes.Hidden)))
                {
                    Debug.WriteLineIf(writeDebug && writeDebugListViewMain,
                                "(" + file.FullName + ") is not added", this.GetType().Name);
                    continue;
                }
                listViewMainItems.Add(new ListViewMainItem(file, tagManager.GetTags(file)));
                //listViewMain.Items.Add(new ListViewMainItem(file, tagManager.GetTags(file)));
            }
            Debug.Unindent();

        }

        private void PopulateListViewMain(IEnumerable<FileSystemInfo> infos, DirectoryInfo searchDirectory)
        {
            listViewMainItems.Clear();
            AddHistory(searchDirectory);

            Debug.WriteLineIf(writeDebug && writeDebugListViewMain,
                    "PopulateListViewMain is called with (enumerable)", this.GetType().Name);
            Debug.Indent();
            
            textBoxPath.Text = "Search result of: " + searchDirectory.FullName;
            ribbonButtonBookmark.IsEnabled = true;

            bool showHidden = Properties.Settings.Default.ShowHidden;

            foreach (FileSystemInfo info in infos)
            {
                if (info.Attributes.HasFlag(FileAttributes.System) ||
                       ((!showHidden) && info.Attributes.HasFlag(FileAttributes.Hidden)))
                {
                    Debug.WriteLineIf(writeDebug && writeDebugListViewMain,
                                "(" + info.FullName + ") is not added", this.GetType().Name);
                    continue;
                }

                listViewMainItems.Add(new ListViewMainItem(info, tagManager.GetTags(info)));
                //listViewMain.Items.Add());
                Debug.WriteLineIf(writeDebug && writeDebugListViewMain,
                                "(" + info.FullName + ") has been added", this.GetType().Name);
            }
            Debug.Unindent();
        }

        public void FindUpdateRow(string fullName, FileSystemInfo info)
        {
            Debug.WriteLineIf(writeDebug && writeDebugListViewMain,
                "FindUpdateRow called. (" + fullName + ")", this.GetType().Name);
            
            foreach (ListViewMainItem item in listViewMainItems)
            {
                if (item.Info.FullName == fullName)
                {
                    Debug.Indent();
                    Debug.WriteLineIf(writeDebug && writeDebugListViewMain,
                        "FindUpdateRow found info", this.GetType().Name);
                    item.UpdateTags(info, tagManager.GetTags(info));
                    Debug.Unindent();
                    return;
                }
            }
        }

        private void AddHistory(DirectoryInfo info)
        {
            if (directoryHistory.First == null || info.FullName != directoryHistory.First.Value.FullName)
            {
                directoryHistory.AddFirst(info);
            }
            if (directoryHistory.Count > 30)
            {
                directoryHistory.RemoveLast();
            }
        }
        
        private void listViewBookmarks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listViewBookmarks.SelectedItem is BookmarkItem)
            {
                BookmarkItem bookmark = (BookmarkItem)listViewBookmarks.SelectedItem;
                if (bookmark.Type == 0)
                {
                    DirectoryInfo new_info = new DirectoryInfo(bookmark.Directory);
                    if (new_info.Exists)
                    {
                        PopulateListViewMain(new_info);
                    }
                    else
                    {
                        MessageBox.Show(bookmark.Directory + " does not exist");
                        return;
                    }
                }
                else
                {
                    DirectoryInfo currentInfo = new DirectoryInfo(bookmark.Directory);
                    SearchQueryInput searchQueryInput = bookmark.searchQueryInput;
                    SearchHandler searchHandler = new SearchHandler(currentInfo, searchQueryInput, tagManager);
                    SearchOption searchOption = SearchOption.AllDirectories;
                    if (bookmark.OptionBString() == "Search current folder")
                    {
                        searchOption = SearchOption.TopDirectoryOnly;
                    }
                    string searchOption2 = bookmark.OptionBString();
                    PopulateListViewMain(searchHandler.Execute(searchOption, searchOption2), currentInfo);
                }
            }
        }





        // Populates listViewBookmarks
        public void PopulateListViewBookmarks()
        {
            bookmarks = bookmarkManager.GetBookmarks();
            listViewBookmarks.ItemsSource = bookmarks;
        }

        private void listViewBookmarks_LostFocus(object sender, RoutedEventArgs e)
        {
            listViewBookmarks.UnselectAll();
        }







        // Setup RibbonMain when loaded
        private void ribbonMain_Loaded(object sender, RoutedEventArgs e)
        {
            var grid = (ribbonApplicationMenu.Template.FindName("MainPaneBorder", ribbonApplicationMenu) as Border).Parent as Grid;
            grid.ColumnDefinitions[2].Width = new GridLength(0);

            ((System.Windows.UIElement)((System.Windows.FrameworkElement)(this.ribbonMain.Template.FindName("PART_TitleHost", this.ribbonMain) as ContentPresenter).Parent).Parent).Visibility = Visibility.Collapsed;
        }
        






        // Application Menu Buttons
        private void ribbonApplicationMenuItemBookmarks_Click(object sender, RoutedEventArgs e)
        {
            WindowBookmarks windowBookmarks = new WindowBookmarks(this, bookmarkManager);
            windowBookmarks.Show();
        }

        private void ribbonApplicationMenuItemOptions_Click(object sender, RoutedEventArgs e)
        {
            WindowOptions windowOptions = new WindowOptions(customDefaultPrograms);
            windowOptions.Show();
        }

        private void ribbonApplicationMenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            MessageBox.Show("SimpleTagManager\nVersion: " + version + "\nContact: jlee257@berkeley.edu\n");
        }

        private void ribbonApplicationMenuItemClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }






        // ListViewMain Item actions
        private void ListViewMain_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = ((FrameworkElement)e.OriginalSource).DataContext as ListViewMainItem;
            if (selectedItem is ListViewMainItem)
            {
                Debug.WriteLineIf(writeDebug && writeDebugListViewItemActions,
                "ListViewMain DoubleClicked selected=" + selectedItem.Name, this.GetType().Name);
                if (selectedItem.Info.Attributes.HasFlag(FileAttributes.Directory))
                {

                    if (Properties.Settings.Default.UseCustomProgram &&
                        customDefaultPrograms.ContainsKey("folder") &&
                        File.Exists(customDefaultPrograms["folder"]))
                    {
                        Process.Start(customDefaultPrograms["folder"], selectedItem.Info.FullName);
                    }
                    else if (Properties.Settings.Default.UseCustomProgram &&
                        customDefaultPrograms.ContainsKey("folder1st") &&
                        File.Exists(customDefaultPrograms["folder1st"]))
                    {
                        Process.Start(customDefaultPrograms["folder1st"], ((DirectoryInfo)selectedItem.Info).GetFiles()[0].FullName);
                    }
                    else
                    {
                        PopulateListViewMain((DirectoryInfo)selectedItem.Info);
                    }
                }
                else
                {
                    if (Properties.Settings.Default.UseCustomProgram &&
                        customDefaultPrograms.ContainsKey(selectedItem.Info.Extension) &&
                        File.Exists(customDefaultPrograms[selectedItem.Info.Extension]))
                    {
                        Process.Start(customDefaultPrograms[selectedItem.Info.Extension], selectedItem.Info.FullName);
                    }
                    else
                    {
                        Process.Start(selectedItem.Info.FullName);
                    }
                }
            }
            else 
            {
                Debug.WriteLineIf(writeDebug && writeDebugListViewItemActions,
                "ListViewMain DoubleClicked selected=null", this.GetType().Name);
            }
        }

        private void listViewMain_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (!(e.OriginalSource is TextBlock))
            {
                listViewMain.SelectedItems.Clear();
                menuItemOpen.Visibility = Visibility.Collapsed;
                menuItemOpenCustom.Visibility = Visibility.Collapsed;
                menuItemSeparater1.Visibility = Visibility.Collapsed;
                menuItemCut.Visibility = Visibility.Collapsed;
                menuItemCopy.Visibility = Visibility.Collapsed;
                menuItemSeparater2.Visibility = Visibility.Collapsed;
                menuItemDelete.Visibility = Visibility.Collapsed;
                menuItemRename.Visibility = Visibility.Collapsed;
                if (ribbonButtonBookmark.IsEnabled)
                {
                    menuItemPaste.Visibility = Visibility.Collapsed;
                }
                else
                {
                    menuItemPaste.Visibility = Visibility.Visible;
                    menuItemPaste.IsEnabled = !fileTransferHandler.IsClipEmpty();
                }
            }
            else if (listViewMain.SelectedItems.Count == 1)
            {
                if (((ListViewMainItem)listViewMain.SelectedItem).
                    Info.Attributes.HasFlag(FileAttributes.Directory))
                {
                    menuItemOpen.Visibility = Visibility.Visible;
                    menuItemOpenCustom.Visibility = Visibility.Visible;
                    menuItemSeparater1.Visibility = Visibility.Visible;
                    menuItemCut.Visibility = Visibility.Visible;
                    menuItemCopy.Visibility = Visibility.Visible;
                    menuItemPaste.Visibility = Visibility.Visible;
                    menuItemPaste.IsEnabled = !fileTransferHandler.IsClipEmpty();
                    menuItemSeparater2.Visibility = Visibility.Visible;
                    menuItemDelete.Visibility = Visibility.Visible;
                    menuItemRename.Visibility = Visibility.Visible;
                }
                else
                {
                    menuItemOpen.Visibility = Visibility.Visible;
                    menuItemOpenCustom.Visibility = Visibility.Visible;
                    menuItemSeparater1.Visibility = Visibility.Visible;
                    menuItemCut.Visibility = Visibility.Visible;
                    menuItemCopy.Visibility = Visibility.Visible;
                    menuItemPaste.Visibility = Visibility.Collapsed;
                    menuItemSeparater2.Visibility = Visibility.Visible;
                    menuItemDelete.Visibility = Visibility.Visible;
                    menuItemRename.Visibility = Visibility.Visible;
                }
            }
            else
            {
                menuItemOpen.Visibility = Visibility.Collapsed;
                menuItemOpenCustom.Visibility = Visibility.Collapsed;
                menuItemSeparater1.Visibility = Visibility.Collapsed;
                menuItemCut.Visibility = Visibility.Visible;
                menuItemCopy.Visibility = Visibility.Visible;
                menuItemPaste.Visibility = Visibility.Collapsed;
                menuItemSeparater2.Visibility = Visibility.Visible;
                menuItemDelete.Visibility = Visibility.Visible;
                menuItemRename.Visibility = Visibility.Collapsed;
            }
        }

        private void menuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            if (listViewMain.SelectedItems.Count == 1)
            {
                ListViewMainItem selectedItem = listViewMain.SelectedItem as ListViewMainItem;
                Debug.WriteLineIf(writeDebug && writeDebugListViewItemActions,
                "ListViewMain Open selected=" + selectedItem.Name, this.GetType().Name);
                if (selectedItem.Info.Attributes.HasFlag(FileAttributes.Directory))
                {
                    PopulateListViewMain((DirectoryInfo)selectedItem.Info);
                }
                else
                {
                    Process.Start(selectedItem.Info.FullName);
                }
            } 
        }

        private void menuItemOpenCustom_Click(object sender, RoutedEventArgs e)
        {
            if (listViewMain.SelectedItems.Count == 1)
            {
                ListViewMainItem selectedItem = listViewMain.SelectedItem as ListViewMainItem;
                Debug.WriteLineIf(writeDebug && writeDebugListViewItemActions,
                "ListViewMain open(custom) selected=" + selectedItem.Name, this.GetType().Name);
                if (selectedItem.Info.Attributes.HasFlag(FileAttributes.Directory))
                {

                    if (customDefaultPrograms.ContainsKey("folder") &&
                        File.Exists(customDefaultPrograms["folder"]))
                    {
                        Process.Start(customDefaultPrograms["folder"], selectedItem.Info.FullName);
                    }
                    else if (customDefaultPrograms.ContainsKey("folder1st") &&
                        File.Exists(customDefaultPrograms["folder1st"]))
                    {
                        Process.Start(customDefaultPrograms["folder1st"], ((DirectoryInfo)selectedItem.Info).GetFiles()[0].FullName);
                    }
                    else
                    {
                        PopulateListViewMain((DirectoryInfo)selectedItem.Info);
                    }
                }
                else
                {
                    if (customDefaultPrograms.ContainsKey(selectedItem.Info.Extension) &&
                        File.Exists(customDefaultPrograms[selectedItem.Info.Extension]))
                    {
                        Process.Start(customDefaultPrograms[selectedItem.Info.Extension], selectedItem.Info.FullName);
                    }
                    else
                    {
                        Process.Start(selectedItem.Info.FullName);
                    }
                }
            }
        }

        private void menuItemCut_Click(object sender, RoutedEventArgs e)
        {
            HashSet<FileSystemInfo> itemInfos = new HashSet<FileSystemInfo>();
            foreach (ListViewMainItem item in listViewMain.SelectedItems)
            {
                itemInfos.Add(item.Info);
            }

            if (fileTransferHandler.Cut(itemInfos))
            {
                Debug.WriteLineIf(writeDebug && writeDebugListViewItemActions,
                    "Cut Successful", this.GetType().Name);
            }
            else
            {
                Debug.WriteLineIf(writeDebug && writeDebugListViewItemActions,
                    "Cut Unsuccessful", this.GetType().Name);
            }
        }

        private void menuItemCopy_Click(object sender, RoutedEventArgs e)
        {
            HashSet<FileSystemInfo> itemInfos = new HashSet<FileSystemInfo>();
            foreach (ListViewMainItem item in listViewMain.SelectedItems)
            {
                itemInfos.Add(item.Info);
            }

            if (fileTransferHandler.Copy(itemInfos))
            {
                Debug.WriteLineIf(writeDebug && writeDebugListViewItemActions,
                    "Copy Successful", this.GetType().Name);
            }
            else
            {
                Debug.WriteLineIf(writeDebug && writeDebugListViewItemActions,
                    "Copy Unsuccessful", this.GetType().Name);
            }
        }

        private void menuItemPaste_Click(object sender, RoutedEventArgs e)
        {
            bool good = false;
            if (listViewMain.SelectedItems.Count == 0)
            {
                good = fileTransferHandler.Paste(directoryHistory.First.Value);
            }
            else if (((ListViewMainItem)listViewMain.SelectedItem).
                Info.Attributes.HasFlag(FileAttributes.Directory))
            {
                good = fileTransferHandler.Paste(
                    ((ListViewMainItem)listViewMain.SelectedItem).Info);
            }

            if (good)
            {
                Debug.WriteLineIf(writeDebug && writeDebugListViewItemActions,
                    "Paste Successful", this.GetType().Name);
            }
            else
            {
                Debug.WriteLineIf(writeDebug && writeDebugListViewItemActions,
                    "Paste Unsuccessful", this.GetType().Name);
            }

            if (!ribbonButtonBookmark.IsEnabled)
            {
                PopulateListViewMain(directoryHistory.First.Value);
            }
        }

        private void menuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            List<ListViewMainItem> deleteList = listViewMain.SelectedItems.Cast<ListViewMainItem>().ToList();
            foreach (ListViewMainItem item in deleteList)
            {
                if (fileTransferHandler.Delete(item.Info))
                {
                    listViewMainItems.Remove(item);
                    Debug.WriteLineIf(writeDebug && writeDebugListViewItemActions,
                        "Delete Successful", this.GetType().Name);
                }
                else
                {
                    Debug.WriteLineIf(writeDebug && writeDebugListViewItemActions,
                        "Delete Unsuccessful", this.GetType().Name);
                }
            }
        }

        private void menuItemRename_Click(object sender, RoutedEventArgs e)
        {
            ListViewMainItem item = (ListViewMainItem)listViewMain.SelectedItem;
            WindowRename windowRename = new WindowRename(this, fileTransferHandler, item.Info);
            windowRename.Show();
        }





        // MainWindow buttons
        private void buttonPrev_Click(object sender, RoutedEventArgs e)
        {
            if (ribbonButtonBookmark.IsEnabled && directoryHistory.Count > 0)
            {
                PopulateListViewMain(directoryHistory.First.Value);
            }
            else if (!ribbonButtonBookmark.IsEnabled && directoryHistory.Count > 1)
            {
                directoryHistory.RemoveFirst();
                PopulateListViewMain(directoryHistory.First.Value);
            }
        }

        private void buttonGoTo_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo new_info = new DirectoryInfo(textBoxPath.Text);
            if (new_info.Exists)
            {
                PopulateListViewMain(new_info);
            }
        }

        private void buttonEdit_Click(object sender, RoutedEventArgs e)
        {
            if (listViewMain.SelectedItems.Count > 0)
            {
                List<FileSystemInfo> f = new List<FileSystemInfo>();
                foreach (ListViewMainItem item in listViewMain.SelectedItems)
                {
                    f.Add(item.Info);
                }

                WindowEditTags windowEditTags =
                    new WindowEditTags(this, tagManager, f);
                windowEditTags.Show();
            }
        }

        private void ribbonButtonSearch_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo currentInfo = directoryHistory.First.Value;
            SearchQueryInput searchQueryInput = new SearchQueryInput(ribbonTextBoxAndTags.Text, ribbonTextBoxOrTags.Text);
            SearchHandler searchHandler = new SearchHandler(currentInfo, searchQueryInput, tagManager);
            SearchOption searchOption = SearchOption.AllDirectories;
            if (ribbonComboBoxSearchSub.Text == "Search current folder")
            {
                searchOption = SearchOption.TopDirectoryOnly;
            }
            string searchOption2 = ribbonComboBoxSearchBoth.Text;


            PopulateListViewMain(searchHandler.Execute(searchOption, searchOption2), currentInfo);
        }

        private void ribbonButtonBookmark_Click(object sender, RoutedEventArgs e)
        {
            WindowEditBookmarks windowEditBookmarks = new WindowEditBookmarks(
                this,
                bookmarkManager,
                directoryHistory.First.Value.FullName,
                ribbonTextBoxAndTags.Text,
                ribbonTextBoxOrTags.Text,
                ribbonComboBoxSearchSub.Text,
                ribbonComboBoxSearchBoth.Text
                );
            windowEditBookmarks.Show();
        }
    }


    public class ListViewMainItem : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string DateModified { get; set; }
        public string Tags { get; set; }
        public ImageSource Icon { get; set; }
        public SolidColorBrush ForeColor { get; set; }
        public FileSystemInfo Info { get; set; }
        
        private static Dictionary<string, ImageSource> iconDictionary =
            new Dictionary<string, ImageSource>();

        static ListViewMainItem()
        {
            iconDictionary.Add("folder", new BitmapImage(new Uri(@"/images/foldericon.png", UriKind.Relative)));
            iconDictionary.Add("iconless", new BitmapImage(new Uri(@"/images/fileicon.png", UriKind.Relative)));
        }



        public ListViewMainItem(FileSystemInfo info, Tag[] tags)
        {
            this.Name = info.Name;
            if (info.Attributes.HasFlag(FileAttributes.Directory))
            {
                Type = "Folder";
            }
            else
            {
                Type = ((FileInfo)info).Extension;
            }
            DateModified = info.LastAccessTime.ToShortDateString();
            Tags = string.Join(", ", tags.Select(tag => tag.ToString()));

            if (info.Attributes.HasFlag(FileAttributes.Hidden))
            {
                ForeColor = Brushes.Silver;
            }
            else
            {
                ForeColor = Brushes.Black;
            }
            Info = info;
            SetIcon();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        public void UpdateTags(FileSystemInfo info, Tag[] tags)
        {
            this.Name = info.Name;
            if (info.Attributes.HasFlag(FileAttributes.Directory))
            {
                Type = "Folder";
            }
            else
            {
                Type = ((FileInfo)info).Extension;
            }
            DateModified = info.LastAccessTime.ToShortDateString();
            Tags = string.Join(", ", tags.Select(tag => tag.ToString()));

            if (info.Attributes.HasFlag(FileAttributes.Hidden))
            {
                ForeColor = Brushes.Silver;
            }
            else
            {
                ForeColor = Brushes.Black;
            }
            Info = info;
            SetIcon();
            this.NotifyPropertyChanged("Tags");
            this.NotifyPropertyChanged("Name");
            this.NotifyPropertyChanged("Icon");
        }

        private void SetIcon()
        {
            if (this.Type == "Folder")
            {
                this.Icon = iconDictionary["folder"];
                //this.Icon = "images/foldericon.png";
            }
            else
            {
                string extension = this.Info.Extension;
                if (iconDictionary.ContainsKey(extension))
                {
                    this.Icon = iconDictionary[extension];
                }
                else
                {
                    System.Drawing.Icon i =
                        System.Drawing.Icon.ExtractAssociatedIcon(this.Info.FullName);
                    ImageSource source = 
                        System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            i.Handle, new Int32Rect(0, 0, i.Width, i.Height),
                            BitmapSizeOptions.FromEmptyOptions());

                    if (source == null)
                    {
                        this.Icon = iconDictionary["iconless"];
                    }
                    else
                    {
                        iconDictionary.Add(extension, source);
                        this.Icon = source;
                    }
                }
            }
        }
    }

}
