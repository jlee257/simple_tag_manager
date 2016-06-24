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
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace SimpleTagManager
{
    /// <summary>
    /// Interaction logic for WindowCustomizeProgram.xaml
    /// </summary>
    public partial class WindowCustomizeProgram : Window
    {
        readonly bool writeDebug = true;

        private string connectionString;
        private SqlConnection connection;
        private ObservableCollection<DefaultProgramItem> programs =
            new ObservableCollection<DefaultProgramItem>();

        private Dictionary<string, string> CustomDefaultPrograms =
            new Dictionary<string, string>();


        public WindowCustomizeProgram(Dictionary<string, string> customDefaultPrograms)
        {
            InitializeComponent();
            this.connectionString = ConfigurationManager.ConnectionStrings["SimpleTagManager.Properties.Settings.DatabaseTagConnectionString"].ConnectionString;
            this.CustomDefaultPrograms = customDefaultPrograms;
            PopulatePrograms();
        }

        private void PopulatePrograms()
        {
            programs.Clear();
            string query = "SELECT * FROM DefaultProgram";
            using (connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                DataTable TagTable = new DataTable();
                adapter.Fill(TagTable);

                //Debug.WriteLine("....Populating " + TagTable.Rows.Count + " rows");

                DefaultProgramItem item;
                foreach (DataRow row in TagTable.Rows)
                {
                    item = new DefaultProgramItem(
                        (row["Extension"]).ToString().Trim(),
                        (row["Program"]).ToString());
                    programs.Add(item);
                }
            }
            listViewPrograms.ItemsSource = programs;
        }

        private void buttonUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (textBoxExtension.Text == null || textBoxExtension.Text.Trim() == "")
            {
                return;
            }
            string query1 = @"DELETE FROM DefaultProgram WHERE Extension = @ext";
            using (connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query1, connection))
            {
                connection.Open();
                command.Parameters.AddWithValue("@ext", textBoxExtension.Text.Trim());
                command.ExecuteNonQuery();
            }
            List<DefaultProgramItem> temp = new List<DefaultProgramItem>(programs);


            foreach (DefaultProgramItem item in temp)
            {
                if (item.Extension == textBoxExtension.Text.Trim())
                {
                    programs.Remove(item);
                }
            }
            CustomDefaultPrograms.Remove(textBoxExtension.Text.Trim());




            if (textBoxPath.Text.Trim() != "")
            {
                try
                {
                    DefaultProgramItem item = new DefaultProgramItem(
                        textBoxExtension.Text.Trim(), textBoxPath.Text.Trim());

                    string query2 = "INSERT INTO DefaultProgram (Extension, Program) " +
                                "VALUES (@ext, @program)";

                    using (connection = new SqlConnection(connectionString))
                    using (SqlCommand command = new SqlCommand(query2, connection))
                    {
                        connection.Open();
                        command.Parameters.AddWithValue("@ext", item.Extension);
                        command.Parameters.AddWithValue("@program", item.Path);
                        command.ExecuteNonQuery();
                    }


                    programs.Add(item);
                    CustomDefaultPrograms.Add(item.Extension, item.Path);
                }

                catch (Exception ex)
                {
                    Debug.WriteLineIf(writeDebug,
                        "Error: " + ex.Message, this.GetType().Name);
                }
            }
            PopulatePrograms();

        }
    }

    public class DefaultProgramItem
    {
        public string Extension { get; set; }
        public string Program { get; set; }
        public string Path { get; set; }


        public DefaultProgramItem(string extension, string path)
        {
            if (path != null && File.Exists(path))
            {
                if (extension.Equals("folder") || extension.Equals("folder1st") || extension.StartsWith("."))
                {
                    FileInfo fileInfo = new FileInfo(path);
                    this.Extension = extension;
                    this.Program = fileInfo.Name;
                    this.Path = fileInfo.FullName;
                }
                else
                {
                    Debug.WriteLine(
                        "Extension: '" + extension + "'");
                    throw new ArgumentException("invalid extension");
                }
            }
            else
            {
                throw new ArgumentException("invalid file path");
            }
        }
    }
}
