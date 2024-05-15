using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using SharpCompress.Archives;
using Newtonsoft.Json.Linq;
using SharpCompress.Common;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Principal;

namespace LazyAimmysetup
{
    internal class Program
    {
        static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
           

            bool updatemode = false;
            Console.Title = "You're so lazy | Themida";


            Console.WriteLine("This Program will install VC redistributables, Dotnet version 8 and 7.0.17 and the latest aimmy version.");
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();

            if (!CheckRights())
            {
                Console.Beep(1000, 500);
                Console.Beep(1000, 500);
                Console.WriteLine("You're running Lazy Aimmy without admin privileges you will get UAC prompts");
                Console.WriteLine("Press enter to continue");
                Console.ReadLine();
            }

            if (Directory.Exists("Aimmy"))
            {
                Console.WriteLine("Aimmy already installed, Do you want to reinstall/ update it?");
                string userresponse = Console.ReadLine().ToUpper();
                if (userresponse == "Y")
                {
                    updatemode = true;
                    Directory.Move("Aimmy//bin", Directory.GetCurrentDirectory() + "\\bin");
                    Directory.Delete("Aimmy", recursive: true);
                }
                else
                {
                    Environment.Exit(0);
                }
            }

            if (!updatemode)
            {
                DownloadEssentials();
            }

            Console.Title = "You're so lazy | Themida | Getting lastest version";

            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 6.1; Trident/7.0; rv:11.0) like Gecko");
            string pastebinres = await httprequest("https://api.github.com/repos/Babyhamsta/Aimmy/releases/latest");
            var jsonObj = JObject.Parse(pastebinres);
            var downloadUrl = (string)jsonObj["assets"][0]["browser_download_url"];

            if (downloadUrl != null)
            {
                Console.WriteLine($"Found download URL: {downloadUrl}");
                Console.WriteLine("----------------------------------");

            }
            else
            {
                Console.WriteLine("Erm something went wrong");
                Console.ReadLine();
                Environment.Exit(0);
            }

            WebClient Filedownloader = new WebClient();
            Console.WriteLine("Downloading latest version of aimmy");
            Filedownloader.DownloadFile(downloadUrl, "Aimmy.zip");

            Console.WriteLine("Creating a Aimmy Folder");

            Directory.CreateDirectory("Aimmy");

            Console.WriteLine("Extracting Aimmy");
            ExtractZipFile("Aimmy.zip", "Aimmy");
            Console.WriteLine("Deleting the Aimmy zip file");
            File.Delete("Aimmy.zip");
            if (updatemode)
            {
                Directory.Delete("Aimmy\\bin", recursive: true);
                Directory.Move(Directory.GetCurrentDirectory() + "\\bin", "Aimmy//bin");

            }
            Process.Start(Directory.GetCurrentDirectory() + "\\Aimmy");
            Console.WriteLine("You're all done you should find aimmy inside your current directory");
            Console.ReadLine();

        }

        //Thanks ChatGPT
        static void ExtractZipFile(string zipFilePath, string extractPath)
        {
            Console.Title = "You're so lazy | Themida | Extracting this will take awhile";

            // Open the archive for extraction
            using (var archive = ArchiveFactory.Open(zipFilePath))
            {
                // Extract all the files in the archive
                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        Console.WriteLine($"Extracting {entry.Key}...");
                        entry.WriteToDirectory(extractPath, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
        }

        public static void DownloadEssentials()
        {
            Console.Title = "You're so lazy | Themida | Downloading Essentials";
            try
            {
                Console.WriteLine("----------------------------------");
                WebClient Filedownloader = new WebClient();
                Process Processhandeler = new Process();
                Processhandeler.StartInfo.Arguments = "/q /norestart";
                Console.WriteLine("Downloading vc_redist");
                Filedownloader.DownloadFile("https://aka.ms/vs/17/release/vc_redist.x64.exe", "vc_redist.x64.exe");
                Processhandeler.StartInfo.FileName = "vc_redist.x64.exe";
                if (CheckRights())
                {
                    Console.WriteLine("starting vc_redist setup");
                }
                else
                {
                    Console.WriteLine("starting vc_redist setup, Since you are not running as admin you need to press Yes on the UAC prompt");
                }
                Processhandeler.Start();
                Processhandeler.WaitForExit();

                Console.WriteLine("vc_redist setup finished");
                Console.WriteLine("Downloading dotnet 7.0.17");
                Filedownloader.DownloadFile("https://download.visualstudio.microsoft.com/download/pr/e35dac95-2855-44f9-b6c9-dda018d922ba/fcc2416e232942d81435a659024bd4e5/dotnet-runtime-7.0.17-win-x64.exe", "dotnet7.exe");
                Processhandeler.StartInfo.FileName = "dotnet7.exe";
                if (CheckRights())
                {
                    Console.WriteLine("Starting dotnet 7 setup");
                }
                else
                {
                    Console.WriteLine("starting dotnet8 setup, Since you are not running as admin you need to press Yes on the UAC prompt");
                }

                Processhandeler.Start();
                Processhandeler.WaitForExit();
                Console.WriteLine("vc_redist setup finished");
                Console.WriteLine("Downloading dotnet8");
                Filedownloader.DownloadFile("https://download.visualstudio.microsoft.com/download/pr/84ba33d4-4407-4572-9bfa-414d26e7c67c/bb81f8c9e6c9ee1ca547396f6e71b65f/windowsdesktop-runtime-8.0.2-win-x64.exe", "dotnet8.exe");
                Processhandeler.StartInfo.FileName = "dotnet8.exe";
                if (CheckRights())
                {
                    Console.WriteLine("Starting dotnet8 setup");
                }
                else
                {
                    Console.WriteLine("starting dotnet8 setup, Since you are not running as admin you need to press Yes on the UAC prompt");
                }

                Processhandeler.Start();
                Processhandeler.WaitForExit();
                Console.WriteLine("dotnet8 setup finished");
                Console.WriteLine("Cleaning up");
                try
                {
                    File.Delete("vc_redist.x64.exe");
                    File.Delete("dotnet8.exe");
                    File.Delete("dotnet7.exe");
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine("Failed to delete a file some setup files may remain");
                }

                Console.WriteLine("Done");
                Console.WriteLine("----------------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Would you like to continue? This may cause an incomplete install Y/N");
                string dec = Console.ReadLine().ToUpper();
                if (dec != "Y")
                {
                    Environment.Exit(0);
                }

            }
            return;
        }
        static async Task<string> httprequest(string uri)
        {
            HttpResponseMessage response = await client.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }
        static bool CheckRights()
        {
            bool isElevated;
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            return isElevated;
        }

    }
}