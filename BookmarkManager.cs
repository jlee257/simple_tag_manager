using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// Manages database. Writes queries, etc. 
    /// </summary>
    


    public class BookmarkManager
    {
        readonly bool writeDebug = true;

        private string connectionString;
        private SqlConnection connection;
        

        public BookmarkManager()
        {
            this.connectionString = ConfigurationManager.ConnectionStrings["SimpleTagManager.Properties.Settings.DatabaseTagConnectionString"].ConnectionString;
        }

        public ObservableCollection<BookmarkItem> GetBookmarks()
        {
            Debug.WriteLineIf(writeDebug,
                "GetBookmarks is called",
                this.GetType().Name);

            string bookmarkString = "";

            ObservableCollection<BookmarkItem> bookmarks = new ObservableCollection<BookmarkItem>();
            string query = "SELECT * FROM Bookmark";

            BookmarkItem bookmarkItem;
            using (connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                DataTable TagTable = new DataTable();
                adapter.Fill(TagTable);

                //Debug.WriteLine("....Populating " + TagTable.Rows.Count + " rows");
                
                foreach (DataRow row in TagTable.Rows)
                {
                    bookmarkString += (row["Title"]).ToString();
                    bookmarkString += ", ";

                    if ((int)(row["Type"]) == 0)
                    {
                        bookmarkItem = new BookmarkItem(
                            (int)(row["Id"]),
                            (row["Title"]).ToString(),
                            (row["Address"]).ToString(),
                            (int)(row["Type"]));
                    }
                    else
                    {
                        bookmarkItem = new BookmarkItem(
                            (int)(row["Id"]),
                            (row["Title"]).ToString(),
                            (row["Address"]).ToString(),
                            (int)(row["Type"]),
                            (row["Alland"]).ToString(),
                            (row["Oneor"]).ToString(),
                            (int)(row["OptionA"]),
                            (int)(row["OptionB"]));
                    }
                    bookmarks.Add(bookmarkItem);
                }
            }
            Debug.WriteLineIf(writeDebug,
                "Bookmarks found={" + bookmarkString + "}",
                this.GetType().Name);
            return bookmarks;
        }
        
        public void AddBookmark(BookmarkItem bookmark)
        {
            Debug.WriteLineIf(writeDebug,
                "AddBookmark is called title={" + bookmark.Title + "}",
                this.GetType().Name);
            try
            {

                if (bookmark.Type == 0)
                {
                    string query = "INSERT INTO Bookmark (Title, Address, Type) " +
                            "VALUES (@title, @address, @type)";

                    using (connection = new SqlConnection(connectionString))
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        command.Parameters.AddWithValue("@title", bookmark.Title);
                        command.Parameters.AddWithValue("@address", bookmark.Directory);
                        command.Parameters.AddWithValue("@type", bookmark.Type);
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    string query = "INSERT INTO Bookmark (Title, Address, Type, Alland, Oneor, OptionA, OptionB) " +
                            "VALUES (@title, @address, @type, @alland, @oneor, @optionA, @optionB)";

                    using (connection = new SqlConnection(connectionString))
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        command.Parameters.AddWithValue("@title", bookmark.Title);
                        command.Parameters.AddWithValue("@address", bookmark.Directory);
                        command.Parameters.AddWithValue("@type", bookmark.Type);
                        command.Parameters.AddWithValue("@alland", bookmark.AndQueryString);
                        command.Parameters.AddWithValue("@oneor", bookmark.OrQueryString);
                        command.Parameters.AddWithValue("@optionA", bookmark.OptionA);
                        command.Parameters.AddWithValue("@optionB", bookmark.OptionB);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void DeleteBookmark(int id)
        {
            Debug.WriteLineIf(writeDebug,
                "DeleteBookmark is called title={" + id + "}",
                this.GetType().Name);

            string query = @"DELETE FROM Bookmark WHERE Id = @id";
            using (connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                connection.Open();
                command.Parameters.AddWithValue("@id", id);
                command.ExecuteNonQuery();
            }
        }


        public Dictionary<string, string> GetCustomDefaultPrograms()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            
            string query = "SELECT * FROM DefaultProgram";
            using (connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                DataTable TagTable = new DataTable();
                adapter.Fill(TagTable);

                //Debug.WriteLine("....Populating " + TagTable.Rows.Count + " rows");
                
                foreach (DataRow row in TagTable.Rows)
                {
                    d.Add((row["Extension"]).ToString().Trim(), (row["Program"]).ToString());
                }
            }
            return d;
        }
    }

    public class BookmarkItem //: INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Directory { get; set; }
        public int Type { get; set; }
        public string AndQueryString { get; set; }
        public string OrQueryString { get; set; }
        public SearchQueryInput searchQueryInput { get; set; }
        public int OptionA { get; set; }
        public int OptionB { get; set; }
        public string TypeToString { get; }

        public BookmarkItem(int id, string title, string address, int type, string andQueryString = "", string orQueryString = "", int optionA = -1, int optionB = -1)
        {
            this.Id = id;
            this.Title = title;
            this.Directory = address;
            this.Type = type;
            this.AndQueryString = andQueryString;
            this.OrQueryString = orQueryString;
            this.searchQueryInput = new SearchQueryInput(andQueryString, orQueryString);
            this.OptionA = optionA;
            this.OptionB = optionB;
            this.TypeToString = (type == 0) ? "Folder" : "Search";
        }

        public string OptionAString()
        {
            return (OptionA == 0) ? "Search all subfolders" : "Search current folder";
        }

        public string OptionBString()
        {
            if (OptionB == 0)
            {
                return "Search files and folders";
            }
            else if (OptionB == 1)
            {
                return "Search files only";
            }
            else
            {
                return "Search folders only";
            }
        }

        //public event PropertyChangedEventHandler PropertyChanged;

        //public void NotifyPropertyChanged(string propName)
        //{
        //    if (this.PropertyChanged != null)
        //        this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
        //}

        //public void UpdateBookmark(int id, string title, string address, int type, string andQueryString = "", string orQueryString = "", int optionA = -1, int optionB = -1)
        //{
        //    this.Id = id;
        //    this.Title = title;
        //    this.Directory = address;
        //    this.Type = type;
        //    this.AndQueryString = andQueryString;
        //    this.OrQueryString = orQueryString;
        //    this.searchQueryInput = new SearchQueryInput(andQueryString, orQueryString);
        //    this.OptionA = optionA;
        //    this.OptionB = optionB;
        //    this.NotifyPropertyChanged("Bookmark");
        //}
    }
}
