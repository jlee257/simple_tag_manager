using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Diagnostics;
using Microsoft.VisualBasic.FileIO;
using System.Windows;

namespace SimpleTagManager
{
    public class FileTransferHandler
    {
        readonly bool writeDebug = true;

        private TagManager tagManager;
        private HashSet<FileSystemInfo> clip;
        private CommandType lastCommand;

        enum CommandType { Copy, Cut, Delete, Move, Rename, Null }
        


        public FileTransferHandler(TagManager tagManager)
        {
            this.tagManager = tagManager;
            clip = new HashSet<FileSystemInfo>();
            lastCommand = CommandType.Null;
        }

        public bool IsClipEmpty()
        {
            return (clip.Count == 0);
        }
        

        public bool Cut(HashSet<FileSystemInfo> itemInfos)
        {
            clip = itemInfos;
            Debug.WriteLineIf(writeDebug,
                "Cut={" + string.Join(", ", itemInfos.Select(info => info.Name)) + "}",
                this.GetType().Name);
            lastCommand = CommandType.Cut;
            return true;
        }

        public bool Copy(HashSet<FileSystemInfo> itemInfos)
        {
            clip = itemInfos;
            Debug.WriteLineIf(writeDebug,
                "Copy={" + string.Join(", ", itemInfos.Select(info => info.Name)) + "}",
                this.GetType().Name);
            lastCommand = CommandType.Copy;
            return true;
        }

        public bool Paste(FileSystemInfo targetDirectory)
        {
            Debug.WriteLineIf(writeDebug,
                "Paste called in directory (" + targetDirectory.FullName + ")",
                this.GetType().Name);

            if (targetDirectory == null ||
                !targetDirectory.Exists ||
                !targetDirectory.Attributes.HasFlag(FileAttributes.Directory))
            {
                Debug.WriteLineIf(writeDebug,
                    "Paste: target directory (" + targetDirectory.FullName + ") is invalid",
                    this.GetType().Name);
                MessageBox.Show("Paste: target directory (" + targetDirectory.FullName + ") is invalid");
                return false;
            }


            foreach (FileSystemInfo item in clip)
            {
                if (!item.Exists)
                {
                    Debug.WriteLineIf(writeDebug,
                        "Paste: item (" + item.FullName + ") does not exist",
                        this.GetType().Name);
                    MessageBox.Show("Paste: item (" + item.FullName + ") does not exist");

                    lastCommand = CommandType.Null;
                    clip.Clear();
                    return false;
                }

                if (item.Attributes.HasFlag(FileAttributes.Directory) &&
                    targetDirectory.FullName.Contains(item.FullName))
                {
                    Debug.WriteLineIf(writeDebug,
                        "Paste: cannot paste (" + item.FullName + ") into its own subfolder",
                        this.GetType().Name);
                    MessageBox.Show("Paste: cannot paste (" + item.FullName + ") into its own subfolder");
                    return false;
                }
            }


            if (lastCommand == CommandType.Copy)
            {
                return PasteCopy(targetDirectory);
            }
            else if (lastCommand == CommandType.Cut)
            {
                if (Move(targetDirectory))
                {
                    clip.Clear();
                    lastCommand = CommandType.Null;
                    return true;
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        private bool PasteCopy(FileSystemInfo targetDirectory)
        {
            foreach (FileSystemInfo item in clip)
            {
                string newTargetPath = Path.Combine(targetDirectory.FullName, item.Name);
                if (item.Attributes.HasFlag(FileAttributes.Directory))
                {
                    try
                    {
                        FileSystem.CopyDirectory(item.FullName, newTargetPath, UIOption.AllDialogs, UICancelOption.ThrowException);
                        tagManager.CopyDirectoryTags((DirectoryInfo)item, new DirectoryInfo(newTargetPath), false);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLineIf(writeDebug,
                            "Copy canceled " + item.Name + " " + ex.Message,
                            this.GetType().Name);
                    }
                }
                else
                {
                    try
                    {
                        FileSystem.CopyFile(item.FullName, newTargetPath, UIOption.AllDialogs, UICancelOption.DoNothing);
                        tagManager.CopyTags(item, new FileInfo(newTargetPath));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLineIf(writeDebug,
                            "Copy canceled " + item.Name + " " + ex.Message,
                            this.GetType().Name);
                    }
                }
            }
            return true;
        }

        public bool Move(FileSystemInfo targetDirectory)
        {
            foreach (FileSystemInfo item in clip)
            {
                string newTargetPath = Path.Combine(targetDirectory.FullName, item.Name);
                if (item.Attributes.HasFlag(FileAttributes.Directory))
                {
                    try
                    {
                        FileSystem.MoveDirectory(item.FullName, newTargetPath, UIOption.AllDialogs, UICancelOption.DoNothing);
                        tagManager.CopyDirectoryTags((DirectoryInfo)item, new DirectoryInfo(newTargetPath), true);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLineIf(writeDebug,
                            "Move canceled " + item.Name + " " + ex.Message,
                            this.GetType().Name);
                    }
                }
                else
                {
                    try
                    {
                        FileSystem.MoveFile(item.FullName, newTargetPath, UIOption.AllDialogs, UICancelOption.DoNothing);
                        tagManager.CopyTags(item, new FileInfo(newTargetPath));
                        tagManager.DeleteTags(item);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLineIf(writeDebug,
                            "Move canceled " + item.Name + " " + ex.Message,
                            this.GetType().Name);
                    }
                }
            }
            return true;
        }



        
        public bool Delete(FileSystemInfo itemInfo)
        {
            Debug.WriteLineIf(writeDebug,
                "Delete={" + itemInfo.FullName + "}",
                this.GetType().Name);
            if (itemInfo.Attributes.HasFlag(FileAttributes.Directory))
            {
                try
                {
                    FileSystem.DeleteDirectory(itemInfo.FullName,
                        UIOption.AllDialogs,
                        RecycleOption.SendToRecycleBin,
                        UICancelOption.DoNothing);

                    tagManager.DeleteDirectoryTags((DirectoryInfo)itemInfo);
                }
                catch (Exception ex)
                {
                    Debug.WriteLineIf(writeDebug,
                        "Delete: item (" + itemInfo.FullName + ") is invalid",
                        this.GetType().Name);
                    MessageBox.Show(ex.Message);
                    return false;
                }

            }
            else
            {
                try
                {
                    FileSystem.DeleteFile(itemInfo.FullName,
                        UIOption.AllDialogs,
                        RecycleOption.SendToRecycleBin,
                        UICancelOption.DoNothing);

                    tagManager.DeleteTags(itemInfo);
                }
                catch (IOException ex)
                {
                    Debug.WriteLineIf(writeDebug,
                        "Delete: item (" + itemInfo.FullName + ") is invalid",
                        this.GetType().Name);
                    MessageBox.Show(ex.Message);
                    return false;
                }
            }
            
            return true;
        }

        public bool Rename(FileSystemInfo target, string newName)
        {
            try
            {
                if (target.Attributes.HasFlag(FileAttributes.Directory))
                {
                    int end = target.FullName.LastIndexOf(target.Name);
                    string newDirPath = Path.Combine(target.FullName.Substring(0, end - 1), newName);
                    FileSystem.RenameDirectory(target.FullName, newName);
                    tagManager.CopyDirectoryTags((DirectoryInfo)target, new DirectoryInfo(newDirPath), true);
                    
                }
                else
                {
                    int end = target.FullName.LastIndexOf(target.Name);
                    string newFilePath = Path.Combine(target.FullName.Substring(0, end - 1), newName);
                    FileSystem.RenameFile(target.FullName, newName);

                    tagManager.CopyTags((FileInfo)target, new FileInfo(newFilePath));
                    tagManager.DeleteTags((FileInfo)target);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLineIf(writeDebug,
                            "Rename: item (" + target.FullName + ") is invalid",
                            this.GetType().Name);
                MessageBox.Show(ex.Message);
                return false;
            }
            return true;
        }

    }

}
