using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using System.IO;

namespace SimpleTagManager
{
    class SearchHandler
    {
        private readonly bool writeDebug = true;

        private DirectoryInfo SearchDirectory;
        private SearchQueryInput SearchInput;
        private bool stop_working;
        private TagManager tagManager;

        public SearchHandler(DirectoryInfo searchDirectory, SearchQueryInput searchInput, TagManager tagManager)
        {
            this.SearchDirectory = searchDirectory;
            this.SearchInput = searchInput;
            this.tagManager = tagManager;
        }

        public IEnumerable<FileSystemInfo> Execute(SearchOption searchOption = SearchOption.AllDirectories, string searchOption2 = "Search files and folders")
        {
            stop_working = false;
            Debug.WriteLineIf(writeDebug,
                "Run Search on '" + SearchDirectory.FullName + "' with " + SearchInput.ToString() + ". " +
                ((searchOption == SearchOption.AllDirectories)? "Searching allsub." : "Searching just this."),
                this.GetType().Name);
            Debug.Indent();



            IEnumerable<string> enumerable;
            if (searchOption2 == "Search files and folders")
            {
                Debug.WriteLineIf(writeDebug,
                    "Searching both files and folders.",
                    this.GetType().Name);
                enumerable = Directory.EnumerateFileSystemEntries(
                    SearchDirectory.FullName, "*", searchOption);
            }
            else if (searchOption2 == "Search files only")
            {
                Debug.WriteLineIf(writeDebug,
                    "Searching files only.",
                    this.GetType().Name);
                enumerable = Directory.EnumerateFiles(
                    SearchDirectory.FullName, "*", searchOption);
            }
            else
            {
                Debug.WriteLineIf(writeDebug,
                    "Searching folders only.",
                    this.GetType().Name);
                enumerable = Directory.EnumerateDirectories(
                    SearchDirectory.FullName, "*", searchOption);
            }



            foreach (string path in enumerable)
            {
                FileSystemInfo info;
                if (File.Exists(path))
                {
                    info = new FileInfo(path);
                }
                else
                {
                    info = new DirectoryInfo(path);
                }
                
                HashSet<Tag> info_tags = new HashSet<Tag>(tagManager.GetTags(info));

                if (stop_working)
                {
                    Debug.WriteLine("....( )'" + info.FullName + "' is stopped");
                    break;
                }

                if (!SearchInput.IsAndEmpty() && !ContainsAllAnd(info.Name, info_tags))
                {
                    Debug.WriteLineIf(writeDebug,
                        "( )'" + info.FullName
                        + "' has tags:{" + string.Join(",", info_tags.Select(tag => tag.Name)) + "} "
                        + "and is missing some AND tags", this.GetType().Name);
                    continue;
                }

                if (!SearchInput.IsOrEmpty() && !ContainsAtLeastOneOr(info.Name, info_tags))
                {
                    Debug.WriteLineIf(writeDebug,
                        "( )'" + info.FullName
                        + "' has tags:{" + string.Join(",", info_tags.Select(tag => tag.Name)) + "} "
                        + "and is missing all OR tags", this.GetType().Name);
                    continue;
                }

                if (stop_working)
                {
                    Debug.WriteLine("....( )'" + info.FullName + "' is stopped");
                    break;
                }
                else
                {
                    Debug.WriteLineIf(writeDebug,
                        "(*)'" + info.FullName
                        + "' has tags:{" + string.Join(",", info_tags.Select(tag => tag.Name)) + "} "
                        + "and is listed", this.GetType().Name);
                    yield return info;
                }
            }
            Debug.Unindent();
            yield break;
        }



        public void StopWorking()
        {
            stop_working = true;
        }



        private bool ContainsAllAnd(string info_name, HashSet<Tag> info_tags)
        {
            return ContainsAllAndNames(info_name) &&
                ContainsAllAndTags(info_tags) &&
                ContainsAllAndGenerals(info_name, info_tags);
        }

        private bool ContainsAllAndNames(string info_name)
        {
            foreach (string tag in SearchInput.AndNames)
            {
                if (!info_name.Contains(tag))
                {
                    Debug.WriteLineIf(writeDebug,
                        "AndName keyword '" + tag + "' is not found. ", this.GetType().Name);
                    return false;
                }
                Debug.WriteLineIf(writeDebug,
                        "AndName keyword '" + tag + "' is found. ", this.GetType().Name);
            }
            return true;
        }

        private bool ContainsAllAndTags(HashSet<Tag> info_tags)
        {

            foreach (Tag tag in SearchInput.AndTags)
            {
                if (!info_tags.Contains(tag))
                {
                    Debug.WriteLineIf(writeDebug,
                        "AndTag keyword '" + tag + "' is not found. ", this.GetType().Name);
                    return false;
                }
                Debug.WriteLineIf(writeDebug,
                        "AndTag keyword '" + tag + "' is found. ", this.GetType().Name);
            }
            return true;
        }

        private bool ContainsAllAndGenerals(string info_name, HashSet<Tag> info_tags)
        {
            Tag searchTag;
            foreach (string tag in SearchInput.AndGenerals)
            {
                try
                {
                    searchTag = new Tag(tag);
                    if (info_tags.Contains(searchTag))
                    {
                        Debug.WriteLineIf(writeDebug,
                        "AndGeneral keyword '" + tag + "' is found. ", this.GetType().Name);
                        continue;
                    }
                }
                catch (ArgumentException ex)
                {
                    Debug.WriteLineIf(writeDebug,
                        "AndGeneral keyword '" + tag + "' is an invalid tag. " +
                        ex.Message, this.GetType().Name);
                }
                if (!(info_name.Contains(tag)))
                {
                    Debug.WriteLineIf(writeDebug,
                        "AndGeneral keyword '" + tag + "' is not found. ", this.GetType().Name);
                    return false;
                }
                Debug.WriteLineIf(writeDebug,
                        "AndGeneral keyword '" + tag + "' is found. ", this.GetType().Name);
            }
            return true;
        }



        private bool ContainsAtLeastOneOr(string info_name, HashSet<Tag> info_tags)
        {
            return ContainsAtLeastOneOrNames(info_name) ||
                ContainsAtLeastOneOrTags(info_tags) ||
                ContainsAtLeastOneOrGenerals(info_name, info_tags);
        }

        private bool ContainsAtLeastOneOrNames(string info_name)
        {
            foreach (string tag in SearchInput.OrNames)
            {
                if (info_name.Contains(tag))
                {
                    Debug.WriteLineIf(writeDebug,
                        "OrName keyword '" + tag + "' is found. ", this.GetType().Name);
                    return true;
                }
                Debug.WriteLineIf(writeDebug,
                        "OrName keyword '" + tag + "' is not found. ", this.GetType().Name);
            }
            return false;
        }

        private bool ContainsAtLeastOneOrTags(HashSet<Tag> info_tags)
        {
            foreach (Tag tag in SearchInput.OrTags)
            {
                if (info_tags.Contains(tag))
                {
                    Debug.WriteLineIf(writeDebug,
                            "OrTag keyword '" + tag + "' is found. ", this.GetType().Name);
                    return true;
                }
                Debug.WriteLineIf(writeDebug,
                        "OrTag keyword '" + tag + "' is not found. ", this.GetType().Name);
            }
            return false;
        }

        private bool ContainsAtLeastOneOrGenerals(string info_name, HashSet<Tag> info_tags)
        {
            Tag searchTag;
            foreach (string tag in SearchInput.OrGenerals)
            {
                try
                {
                    searchTag = new Tag(tag);
                    if (info_tags.Contains(searchTag))
                    {
                        Debug.WriteLineIf(writeDebug,
                            "OrGeneral keyword '" + tag + "' is found. ", this.GetType().Name);
                        return true;
                    }
                }
                catch (ArgumentException ex)
                {
                    Debug.WriteLineIf(writeDebug,
                        "OrGeneral keyword '" + tag + "' is an invalid tag. " +
                        ex.Message, this.GetType().Name);
                }
                if (info_name.Contains(tag))
                {
                    Debug.WriteLineIf(writeDebug,
                        "OrGeneral keyword '" + tag + "' is found. ", this.GetType().Name);
                    return true;
                }
                Debug.WriteLineIf(writeDebug,
                        "OrGeneral keyword '" + tag + "' is not found. ", this.GetType().Name);
            }
            return false;
        }



    }

    public class SearchQueryInput

    {
        readonly bool writeDebug = true;

        public HashSet<Tag> AndTags = new HashSet<Tag>();
        public HashSet<Tag> AndGeneralTags = new HashSet<Tag>();
        public HashSet<string> AndGenerals = new HashSet<string>();
        public HashSet<string> AndNames = new HashSet<string>();
        public HashSet<Tag> OrTags = new HashSet<Tag>();
        public HashSet<Tag> OrGeneralTags = new HashSet<Tag>();
        public HashSet<string> OrGenerals = new HashSet<string>();
        public HashSet<string> OrNames = new HashSet<string>();
        private bool is_and_empty;
        private bool is_or_empty;

        public SearchQueryInput(string andQueryString, string orQueryString)
        {
            is_and_empty = (andQueryString == null || andQueryString.Trim() == "");
            is_or_empty = (orQueryString == null || orQueryString.Trim() == "");


            string word;
            foreach (string w in andQueryString.Split(','))
            {
                word = w.Trim();
                if (word.StartsWith("name:", true, null))
                {
                    AndNames.Add(word.Substring(5));
                }
                else if (word.StartsWith("tag:", true, null) || word.StartsWith("tags:", true, null))
                {
                    try
                    {
                        AndTags.Add(new Tag(word.Substring(word.IndexOf(':') + 1)));
                    }
                    catch (ArgumentException ex)
                    {
                        Debug.WriteLineIf(writeDebug,
                            "AndTag keyword '" + word + "' is not added to AndTags. " +
                            ex.Message, this.GetType().Name);
                    }
                }
                else if (word != "")
                {
                    AndGenerals.Add(word);
                    try
                    {
                        AndGeneralTags.Add(new Tag(word));
                    }
                    catch (ArgumentException ex)
                    {
                        Debug.WriteLineIf(writeDebug,
                            "AndGeneral keyword '" + word + "' is not added to AndGeneralTags. " +
                            ex.Message, this.GetType().Name);
                    }
                }
            }

            foreach (string w in orQueryString.Split(','))
            {
                word = w.Trim();
                if (word.StartsWith("name:", true, null))
                {
                    OrNames.Add(word.Substring(5));
                }
                else if (word.StartsWith("tag:", true, null) || word.StartsWith("tags:", true, null))
                {
                    try
                    {
                        OrTags.Add(new Tag(word.Substring(word.IndexOf(':') + 1)));
                    }
                    catch (ArgumentException ex)
                    {
                        Debug.WriteLineIf(writeDebug,
                            "OrTag keyword '" + word + "' is not added to OrTags. " +
                            ex.Message, this.GetType().Name);
                    }
                }
                else if (word != "")
                {
                    OrGenerals.Add(word);
                    try
                    {
                        OrGeneralTags.Add(new Tag(word));
                    }
                    catch (ArgumentException ex)
                    {
                        Debug.WriteLineIf(writeDebug,
                            "OrGeneral keyword '" + word + "' is not added to OrGeneralTags. " +
                            ex.Message, this.GetType().Name);
                    }
                }
            }
        }

        public bool IsEmpty()
        {
            return is_and_empty && is_or_empty;
        }

        public bool IsAndEmpty()
        {
            return is_and_empty;
        }

        public bool IsOrEmpty()
        {
            return is_or_empty;
        }

        public override string ToString()
        {
            string txt = "SearchQueryInput:{";

            if (AndGenerals.Count > 0)
            {
                txt += "AndGeneral:[";
                txt += string.Join(",", AndGenerals.ToArray());
                txt += "], ";
            }

            if (AndNames.Count > 0)
            {
                txt += "AndNames:[";
                txt += string.Join(",", AndNames.ToArray());
                txt += "], ";
            }
            if (AndTags.Count > 0)
            {
                txt += "AndTags:[";
                txt += string.Join(",", AndTags.Select(tag => tag.ToString()));
                txt += "], ";
            }
            if (OrGenerals.Count > 0)
            {
                txt += "OrGenerals:[";
                txt += string.Join(",", OrGenerals.ToArray());
                txt += "], ";
            }
            if (OrNames.Count > 0)
            {
                txt += "OrNames:[";
                txt += string.Join(",", OrNames.ToArray());
                txt += "], ";
            }
            if (OrTags.Count > 0)
            {
                txt += "OrTags:[";
                txt += string.Join(",", OrTags.Select(tag => tag.ToString()));
                txt += "], ";
            }

            txt.TrimEnd(',', ' ');
            txt += "}";

            return txt;
        }
    }
}
