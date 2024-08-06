using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MultithreadedDownloader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Parse command line arguments
            string urls = "";
            string directory = "";
            int threads = 4; // Default value

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-urls":
                        urls = args[i + 1];
                        break;
                    case "-dir":
                        directory = args[i + 1];
                        break;
                    case "-threads":
                        if (!int.TryParse(args[i + 1], out threads))
                        {
                            Console.WriteLine("Invalid value for -threads. Using default value of 4.");
                            threads = 4;
                        }
                        break;
                }
            }

            // Validate input
            if (string.IsNullOrEmpty(urls) || string.IsNullOrEmpty(directory))
            {
                Console.WriteLine("Usage: downloader.exe -urls \"url1,url2,...\" -dir \"directory\" [-threads count]");
                return;
            }

            // Split URLs
            string[] urlList = urls.Split(',');

            // Use SemaphoreSlim to limit the number of concurrent downloads
            using (SemaphoreSlim semaphore = new SemaphoreSlim(threads))
            {
                // Create a list of tasks
                List<Task> tasks = new List<Task>();

                // Start a new task for each URL
                foreach (string url in urlList)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        // Wait for a semaphore slot to become available
                        await semaphore.WaitAsync();
                        try
                        {
                            await DownloadFileAsync(url, directory);
                        }
                        finally
                        {
                            // Release the semaphore slot
                            semaphore.Release();
                        }
                    }));
                }

                // Wait for all downloads to complete
                await Task.WhenAll(tasks);
            }

            Console.WriteLine("All files downloaded successfully!");
        }

        static async Task DownloadFileAsync(string url, string directory)
        {
            try
            {
                // Get file name from URL
                string fileName = Path.GetFileName(url);

                // Create full file path
                string filePath = Path.Combine(directory, fileName);

                // Download file
                using (WebClient client = new WebClient())
                {
                    long totalBytes = 0;
                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        totalBytes = e.TotalBytesToReceive;
                        int progress = (int)((double)e.BytesReceived / totalBytes * 100);
                        Console.WriteLine($"\rDownloading {fileName}: [{new string('#', progress / 5)}] {progress}%");
                    };
                    await client.DownloadFileTaskAsync(new Uri(url), filePath);
                }

                Console.WriteLine($"\nDownloaded {fileName} successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading {url}: {ex.Message}");
            }
        }
    }
}