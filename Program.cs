using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace FileSystemScaner
{
    internal class Program
    {
        private static int counter;
        static void Main(string[] args)
        {
            var time_begin = DateTimeOffset.Now;
            List<DirectoryInfo> list_all_disc = new List<DirectoryInfo>();
            XDocument document = new XDocument();
            XElement root_list = new XElement("items");

            list_all_disc.Add(new DirectoryInfo("C:\\"));

            list_all_disc.ForEach(directory =>
            {
                // if at the directory have sub directories. Then run the following code
                List<DirectoryInfo> get_subdirectories = directory.GetDirectories().ToList();
                if (get_subdirectories.Count != 0)
                {
                    // create new search list
                    List<searcher> search_list = new List<searcher>();
                    // create new thread list
                    List<Thread> thread_list = new List<Thread>();

                    // for each folder in the root directory. Creating a class search and thread
                    get_subdirectories.ForEach(sub_directory =>
                    {
                        searcher s1 = new searcher(sub_directory, "*.*", DateTime.MinValue, DateTime.MaxValue, 0, Int32.MaxValue);
                        search_list.Add(s1);
                        Thread t1 = new Thread(s1.start_search);
                        thread_list.Add(t1);
                    });

                    thread_list.ForEach(thread => thread.Start()); // running all threads

                    // waiting for all threads to finish
                    while (true)
                    {
                        int end_count = 0;
                        thread_list.ForEach(thread =>
                        {
                            if (thread.ThreadState == ThreadState.Running)
                                end_count++;
                        });
                        if (end_count == 0) break;
                    }
                    search_list.ForEach(search => search.find_files.ForEach(file =>
                    {
                        XElement root_class = new XElement("file");
                        XElement filename = new XElement("filename", file.file_name);
                        XElement create_date = new XElement("create-data", file.create_date);
                        XElement directories = new XElement("directory", file.directory);
                        XElement file_size = new XElement("file-size", file.size);
                        root_class.Add(filename);
                        root_class.Add(create_date);
                        root_class.Add(directories);
                        root_class.Add(file_size);
                        root_list.Add(root_class);
                        counter++;
                    }));
                }

                // creating a new searches for find all files in the root directory
                searcher ls = new searcher(directory, "*.*", DateTime.MinValue, DateTime.MaxValue, 0, Int32.MaxValue);
                List<file_information> local_file = ls.search_files(directory); // Search for files only in the root directory
                local_file.ForEach(file =>
                {
                    XElement root_class = new XElement("file");
                    XElement filename = new XElement("filename", file.file_name);
                    XElement create_date = new XElement("create-data", file.create_date);
                    XElement directories = new XElement("directory", file.directory);
                    XElement file_size = new XElement("file-size", file.size);
                    root_class.Add(filename);
                    root_class.Add(create_date);
                    root_class.Add(directories);
                    root_class.Add(file_size);
                    root_list.Add(root_class);
                    counter++;
                });
                Console.WriteLine($"Scan system done. Find {counter} files");

                if (counter == 0)
                    Console.WriteLine($"Scan system Error. File not found");
            });
            document.Add(root_list);
            document.Save("123.xml");
            var time_end = DateTimeOffset.Now;
            Console.WriteLine($"{time_begin} | {time_end}");
            Console.ReadLine();
        }
    }
}
