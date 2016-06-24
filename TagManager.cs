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
using System.Windows.Forms;

namespace SimpleTagManager
{
    /// <summary>
    /// Manages database. Writes queries, etc. 
    /// </summary>
    public class TagManager
    {
        readonly bool writeDebug = true;

        private string connectionString;
        private SqlConnection connection;

        public TagManager()
        {
            this.connectionString = ConfigurationManager.ConnectionStrings["SimpleTagManager.Properties.Settings.DatabaseTagConnectionString"].ConnectionString;
        }



        public Tag[] GetTags(FileSystemInfo info)
        {
            Tag[] tagArray = new Tag[0];

            string query = "SELECT t.Tag FROM Filetag AS t " +
                "INNER JOIN (SELECT Id FROM Fileinfo WHERE Address = @address) AS f " +
                "ON f.Id = t.Fileinfo_id";

            using (connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                command.Parameters.AddWithValue("@address", info.FullName);

                DataTable TagTable = new DataTable();
                adapter.Fill(TagTable);

                //Debug.WriteLine("....Populating " + TagTable.Rows.Count + " rows");
                tagArray = new Tag[TagTable.Rows.Count];
                
                int count = 0;
                foreach (DataRow row in TagTable.Rows)
                {
                    tagArray[count] = new Tag((row["Tag"]).ToString());
                    count++;
                }
            }


            Debug.WriteLineIf(writeDebug,
                "GetTags called (" + info.FullName + ") = {" +
                string.Join(", ", tagArray.Select(tag => tag.ToString())) + "}",
                this.GetType().Name);

            return tagArray;
        }

        public void InsertTags(FileSystemInfo info, HashSet<Tag> tags)
        {
            InsertTags(GetFileinfoId(info), tags);
        }

        public void InsertTags(int fileinfoId, HashSet<Tag> tags)
        {
            Debug.WriteLineIf(writeDebug,
                "InsertTags called fileinfoID = " + fileinfoId + " inserting {" +
                string.Join(", ", tags.Select(tag => tag.ToString())) + "}",
                this.GetType().Name);
            if (fileinfoId < 0)
            {
                Debug.WriteLine("fileinfoId is " + fileinfoId + 
                    ". aborting InsertTag", this.GetType().Name);
                throw new ArgumentException();
            }

            foreach (Tag tag_add in tags)
            {
                string query = "INSERT INTO Filetag (Fileinfo_id,Tag) " +
                        "VALUES (@file_id,@tag)";

                using (connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    command.Parameters.AddWithValue("@file_id", fileinfoId);
                    command.Parameters.AddWithValue("@tag", tag_add.ToString());
                    command.ExecuteNonQuery();
                }
            }
        }

        public void RemoveTags(FileSystemInfo info, HashSet<Tag> tags)
        {
            RemoveTags(GetFileinfoId(info), tags);
        }

        public void RemoveTags(int fileinfoId, HashSet<Tag> tags)
        {
            Debug.WriteLineIf(writeDebug,
                "RemoveTags called fileinfoID = " + fileinfoId + " removing {" +
                string.Join(", ", tags.Select(tag => tag.ToString())) + "}",
                this.GetType().Name);

            if (fileinfoId < 0)
            {
                Debug.WriteLine("fileinfoId is " + fileinfoId +
                    ". aborting InsertTag", this.GetType().Name);
                throw new ArgumentException();
            }


            foreach (Tag tag_remove in tags)
            {
                Debug.WriteLine("....Deleting '" + tag_remove + "'");
                string query = "DELETE FROM Filetag WHERE " +
                    "Fileinfo_id = @file_id AND Tag = @tag";
                using (connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    command.Parameters.AddWithValue("@file_id", fileinfoId);
                    command.Parameters.AddWithValue("@tag", tag_remove.ToString());
                    command.ExecuteNonQuery();
                    Debug.WriteLine("....Delete Complete");
                }
            }
        }

        public int GetFileinfoId (FileSystemInfo info)
        {
            int infoId = -1;
            bool insert_new = true;
            string query = "SELECT id FROM Fileinfo WHERE address = @address";
            using (connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                command.Parameters.AddWithValue("@address", info.FullName);

                DataTable TagTable = new DataTable();
                adapter.Fill(TagTable);

                foreach (DataRow row in TagTable.Rows)
                {
                    insert_new = false;
                    infoId = (int)row["id"];
                }
            }


            if (insert_new)
            {
                try
                {
                    query = "INSERT INTO Fileinfo (Name,Address) " +
                        "OUTPUT INSERTED.ID " +
                        "VALUES (@name,@address)";

                    using (connection = new SqlConnection(connectionString))
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        connection.Open();
                        command.Parameters.AddWithValue("@name", info.Name);
                        command.Parameters.AddWithValue("@address", info.FullName);
                        infoId = (int)command.ExecuteScalar();
                    }


                    Debug.WriteLineIf(writeDebug,
                        "Creating new Fileinfo row. Id= " + infoId,
                        this.GetType().Name);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            Debug.WriteLineIf(writeDebug,
                    "GetFileSystemInfoID (" + info.FullName + ")=" + infoId,
                    this.GetType().Name);

            return infoId;
        }



        public void CopyTags(FileSystemInfo source, FileSystemInfo target)
        {
            string q1 = "INSERT INTO Fileinfo (Name, Address) " +
                "SELECT @name, @address " +
                "WHERE NOT EXISTS (SELECT Address FROM Fileinfo WHERE Address = @address2)";

            using (connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(q1, connection))
            {
                connection.Open();
                command.Parameters.AddWithValue("@name", target.Name);
                command.Parameters.AddWithValue("@address", target.FullName);
                command.Parameters.AddWithValue("@address2", target.FullName);
                command.ExecuteNonQuery();
            }



            string q2 = "INSERT INTO Filetag (Fileinfo_id, Tag) " +
                "SELECT n.Id, n.Tag " +
                "FROM (SELECT f.Id AS Id, t.Tag AS Tag " +
                      "FROM (SELECT Id FROM Fileinfo WHERE Address = @target) AS f " +
                      "CROSS JOIN (SELECT Tag " +
                                  "FROM Filetag " +
                                  "INNER JOIN (SELECT Id FROM Fileinfo WHERE Address = @source) AS m " +
                                  "ON m.Id = Filetag.Fileinfo_id) AS t) AS n";
            using (connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(q2, connection))
            {
                connection.Open();
                command.Parameters.AddWithValue("@target", target.FullName);
                command.Parameters.AddWithValue("@source", source.FullName);
                command.ExecuteNonQuery();
            }
        }

        public void CopyDirectoryTags(DirectoryInfo source, DirectoryInfo target, bool deleteSourceTags = false)
        {
            CopyTags(source, target);
            if (deleteSourceTags) { DeleteTags(source); }

            foreach (FileInfo fi in source.GetFiles())
            {
                FileInfo new_fi = new FileInfo(Path.Combine(target.FullName, fi.Name));
                CopyTags(fi, new_fi);
                if (deleteSourceTags) { DeleteTags(fi); }
            }

            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    new DirectoryInfo(Path.Combine(target.FullName, diSourceSubDir.Name));
                CopyDirectoryTags(diSourceSubDir, nextTargetSubDir);
            }
        }

        public void DeleteTags(FileSystemInfo target)
        {
            Debug.WriteLineIf(writeDebug,
                "DeleteTags is called target={" + target.FullName + "}",
                this.GetType().Name);
            
            string query = @"DELETE FROM Fileinfo WHERE Address = @address";
            using (connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                connection.Open();
                command.Parameters.AddWithValue("@address", target.FullName);
                command.ExecuteNonQuery();
            }
        }
        
        public void DeleteDirectoryTags(DirectoryInfo target)
        {

            string query = "DELETE FROM Fileinfo WHERE Address LIKE '@address'";
            using (connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                connection.Open();
                command.Parameters.AddWithValue("@address", target.FullName + '%');
                command.ExecuteNonQuery();
            }
        }
    }









    public class Tag : IEquatable<Tag>
    {
        private static readonly bool writeDebug = true;


        private readonly string n;
        private static readonly RegexStringValidator Validator =
            new RegexStringValidator(@"^[^\t\r\n\v\f*,;\\/|]+$");

        public string Name { get { return n; } }

        public Tag(string name)
        {
            Debug.Indent();
            this.n = name.Trim();
            Validator.Validate(n);
            Debug.WriteLineIf(writeDebug, "created '" + 
                n + "'", this.GetType().Name);
            Debug.Unindent();
        }

        public override int GetHashCode()
        {
            return n.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is Tag && Equals((Tag)obj);
        }

        public bool Equals(Tag t)
        {
            return (n.Equals(t.n));
        }

        public override string ToString()
        {
            return n;
        }
    }
    
}
