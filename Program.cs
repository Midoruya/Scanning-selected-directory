using System.Xml.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace FileSystemScaner
{
    public class file_information
    {
        public string file_name { get; set; }
        public string create_date { get; set; }
        public string directory { get; set; }
        public string size { get; set; }
        public file_information(string file_name, string create_date, string directory, string size)
        {
            this.create_date = create_date;
            this.directory = directory;
            this.size = size;
            this.file_name = file_name;
        }
    }
    public class Searcher
    {
        private DirectoryInfo directory_info;
        private string patternt_for_search;
        private DateTime min_create_time;
        private DateTime max_Create_time;
        private double min_file_size;
        private double max_file_size;
        public Searcher(DirectoryInfo directory_info, string patternt_for_search, DateTime min_create_time, DateTime max_Create_time, double min_file_size, double max_file_size)
        {
            this.directory_info = directory_info;
            this.patternt_for_search = patternt_for_search;
            this.min_create_time = min_create_time;
            this.max_Create_time = max_Create_time;
            this.min_file_size = min_file_size;
            this.max_file_size = max_file_size;
            this.find_feles = new List<file_information>();
        }
        public List<file_information> find_feles;
        /// <summary>
        /// use this to find all the files in the directory
        /// </summary>
        /// <param name="dir">durectory for finding</param>
        /// <returns>all file in the directory as list</returns>
        public List<file_information> serach_files(DirectoryInfo dir)
        {
            List<file_information> result = new List<file_information>();
            dir.GetFiles(this.patternt_for_search, SearchOption.TopDirectoryOnly).ToList().ForEach(file => // get all files at directory
            {
                try
                {
                    if (file.CreationTime.Date >= this.min_create_time && file.CreationTime.Date <= this.max_Create_time && ((file.Length) / 1000) <= this.max_file_size && ((file.Length / 1000)) >= this.min_file_size)
                    {
                        try { result.Add(new file_information(file.Name, file.CreationTime.ToString(), file.DirectoryName, (file.Length / 1024).ToString())); } // adding to result array new file
                        catch { Thread.Sleep(1); }; // if this file gives an error
                    }
                }
                catch { Thread.Sleep(1); } // if this file gives an error.
            });
            return result;
        }

        public void start_search()
        {
            try
            {
                List<DirectoryInfo> info_about_current_directory = directory_info.GetDirectories().ToList();
                if (info_about_current_directory.Count != 0)
                {
                    info_about_current_directory.ForEach(directory => // For each directory, we launch a new search
                    {
                        Searcher second = new Searcher(directory, patternt_for_search, min_create_time, max_Create_time, min_file_size, max_file_size);
                        second.start_search(); // Start search in subdirectory
                        find_feles.AddRange(second.find_feles); // Get list of all files which statisfy all conditions
                    });
                }
                find_feles.AddRange(serach_files(this.directory_info)); // Find all files in current directory
            }
            catch { }
        }


    }
    internal class Program
    {
        public static int counter = 0;
        public static List<file_information> file_list = new List<file_information>();
        static void Main(string[] args)
        {
            var time_begin = DateTimeOffset.Now;
            List<DirectoryInfo> list_all_disc = new List<DirectoryInfo>();
            XDocument document = new XDocument();
            XElement root_list = new XElement("items");

            list_all_disc.Add(new DirectoryInfo("C:\\"));

            list_all_disc.ForEach(directory =>
            {
                // if at the directory have sub directorys. Then run the following code
                List<DirectoryInfo> get_subdirectories = directory.GetDirectories().ToList();
                if (get_subdirectories.Count != 0)
                {
                    // create new search list
                    List<Searcher> search_list = new List<Searcher>();
                    // create new thread list
                    List<Thread> thread_list = new List<Thread>();

                    // for each folder in the root directory. Creating a class search and thread
                    get_subdirectories.ForEach(sub_directory =>
                    {
                        Searcher s1 = new Searcher(sub_directory, "*.*", DateTime.MinValue, DateTime.MaxValue, 0, Int32.MaxValue);
                        search_list.Add(s1);
                        Thread t1 = new Thread(new ThreadStart(s1.start_search));
                        thread_list.Add(t1);
                    });

                    thread_list.ForEach(thread => thread.Start()); // runin all threads

                    // waiting for all threads to finish
                    while (true)
                    {
                        int endCount = 0;
                        thread_list.ForEach(thread =>
                        {
                            if (thread.ThreadState == ThreadState.Running)
                                endCount++;
                        });
                        if (endCount == 0) break;
                    }
                    search_list.ForEach(search => search.find_feles.ForEach(file =>
                    {
                        XElement root_class = new XElement("file");
                        XElement filename = new XElement("filename", file.file_name);
                        XElement create_date = new XElement("create-data", file.create_date);
                        XElement directorys = new XElement("directory", file.directory);
                        XElement file_size = new XElement("file-size", file.size);
                        root_class.Add(filename);
                        root_class.Add(create_date);
                        root_class.Add(directorys);
                        root_class.Add(file_size);
                        root_list.Add(root_class);
                        counter++;
                    }));
                }

                // creating a new searches for find all files in the root directory
                Searcher ls = new Searcher(directory, "*.*", DateTime.MinValue, DateTime.MaxValue, 0, Int32.MaxValue);
                List<file_information> local_file = ls.serach_files(directory); // Search for files only in the root directory
                local_file.ForEach(file =>
                {
                    XElement root_class = new XElement("file");
                    XElement filename = new XElement("filename", file.file_name);
                    XElement create_date = new XElement("create-data", file.create_date);
                    XElement directorys = new XElement("directory", file.directory);
                    XElement file_size = new XElement("file-size", file.size);
                    root_class.Add(filename);
                    root_class.Add(create_date);
                    root_class.Add(directorys);
                    root_class.Add(file_size);
                    root_list.Add(root_class);
                    counter++;
                });
                Console.WriteLine($"Scan systme done. Find {counter} files");

                if (counter == 0)
                    Console.WriteLine($"Scan systme Error. File not found");
            });
            document.Add(root_list);
            document.Save("123.xml");
            var time_end = DateTimeOffset.Now;
            Console.WriteLine($"{time_begin} | {time_end}");
            Console.ReadLine();
        }
    }
}
