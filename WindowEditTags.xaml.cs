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
using System.Diagnostics;

namespace SimpleTagManager
{
    /// <summary>
    /// Interaction logic for WindowEditTags.xaml
    /// </summary>
    public partial class WindowEditTags : Window
    {
        readonly bool writeDebug = true;

        private MainWindow mainWindow;
        private TagManager tagManager;
        private List<FileSystemInfo> currentItemInfos;

        private HashSet<Tag> tagsInitial = new HashSet<Tag>();
        private HashSet<Tag> tagsFinal = new HashSet<Tag>();



        public WindowEditTags(MainWindow mainWindow, TagManager tagManager, List<FileSystemInfo> currentItemInfos)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.currentItemInfos = currentItemInfos;
            this.tagManager = tagManager;

            labelAddress.Content = currentItemInfos[0].FullName;
            if (currentItemInfos.Count > 1)
            {
                labelName.Content = currentItemInfos[0].Name + " (+" + (currentItemInfos.Count - 1) + " other files/folders)";
                tagsInitial.Clear();
                tagsFinal.Clear();
                listViewTags.Items.Clear();
            }
            else
            {
                labelName.Content = currentItemInfos[0].Name;
                PopulateListViewTags(currentItemInfos[0]);
            }
        }

        private void PopulateListViewTags(FileSystemInfo info)
        {
            Debug.WriteLineIf(writeDebug,
                "PopulateListViewTags called with (" + info.FullName + ")",
                this.GetType().Name);
            Debug.Indent();

            tagsInitial.Clear();
            tagsFinal.Clear();
            listViewTags.Items.Clear();


            ListViewItem newItem;
            foreach (Tag tag in tagManager.GetTags(info))
            {
                tagsInitial.Add(tag);

                newItem = new ListViewItem() { Content = tag.ToString(), Tag = tag };
                listViewTags.Items.Add(newItem);

                Debug.WriteLineIf(writeDebug,
                    "Tag '" + tag.Name + "' has been added",
                    this.GetType().Name);
            }
            tagsFinal = new HashSet<Tag>(tagsInitial);
            Debug.Unindent();
        }





        // windowEditTags buttons
        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Tag tag = new Tag(textBoxAdd.Text);
                tagsFinal.Add(tag);
                foreach (ListViewItem i in listViewTags.Items)
                {
                    if (tag.Equals((Tag)i.Tag))
                    {
                        i.Content = tag.ToString() + '*';
                        i.Foreground = Brushes.Black;
                        return;
                    }
                    Debug.WriteLineIf(writeDebug,
                        "Tag " + i.Tag + " is different from Tag " + tag,
                        this.GetType().Name);
                }
                ListViewItem item = new ListViewItem() { Content = tag.ToString() + '*', Tag = tag };
                listViewTags.Items.Add(item);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            foreach (ListViewItem item in listViewTags.SelectedItems)
            {
                Tag tag = (Tag)item.Tag;
                tagsFinal.Remove(tag);
                item.Content = tag.ToString() + '*';
                item.Foreground = Brushes.Silver;
            }
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLineIf(writeDebug,
                "buttonOK is clicked. Initial tags={" +
                string.Join(",", tagsInitial) + "} final tags={" +
                string.Join(",", tagsFinal) + "}", this.GetType().Name);
            HashSet<Tag> tagsToAdd = new HashSet<Tag>(tagsFinal);
            HashSet<Tag> tagsToRemove = tagsInitial;
            tagsToAdd.ExceptWith(tagsInitial);
            tagsToRemove.ExceptWith(tagsFinal);

            Debug.WriteLineIf(writeDebug,
                "buttonOK is adding={" +
                string.Join(",", tagsToAdd) + "} and removing={" +
                string.Join(",", tagsToRemove) + "}", this.GetType().Name);

            foreach (FileSystemInfo currentItemInfo in currentItemInfos)
            {
                int fileinfoId = tagManager.GetFileinfoId(currentItemInfo);
                if (currentItemInfos.Count == 1)
                {
                    tagManager.RemoveTags(fileinfoId, tagsToRemove);
                }
                tagManager.InsertTags(fileinfoId, tagsToAdd);

                tagsInitial.Clear();
                tagsFinal.Clear();
                mainWindow.FindUpdateRow(currentItemInfo.FullName, currentItemInfo);
            }

            this.Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLineIf(writeDebug,
                "buttonCancel is clicked.", this.GetType().Name);
            tagsInitial.Clear();
            tagsFinal.Clear();
            this.Close();
        }
    }

    public class TextInputToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Always test MultiValueConverter inputs for non-null 
            // (to avoid crash bugs for views in the designer) 
            if (values[0] is bool && values[1] is bool)
            {
                bool hasText = !(bool)values[0];
                bool hasFocus = (bool)values[1];
                if (hasFocus || hasText)
                    return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
