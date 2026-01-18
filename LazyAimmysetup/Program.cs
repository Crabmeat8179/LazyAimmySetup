using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using SharpCompress.Archives;
using Newtonsoft.Json.Linq;
using SharpCompress.Common;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Text;

namespace LazyAimmysetup
{
    internal class Program
    {
        static readonly HttpClient client = new HttpClient();
        static readonly string version = "2.3";
        static async Task Main(string[] args)
        {
           
            bool binfoldermoved = false;
            bool updatemode = false;
            bool SkipAimmy = false;
            //Console.WriteLine(args[0].ToString());
            //Console.ReadLine();
            if (args.Length > 0)
            {
                if (args[0].ToString().ToUpper() == "SKIPSETUPS")
                {
                    updatemode = true;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("SkipSetups Argument used\n");
                    Console.ResetColor();
                }
                if (args[0].ToString().ToUpper() == "SKIPAIMMY")
                {
                    SkipAimmy = true;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("SkipAimmy Argument used\n");
                    Console.ResetColor();
                }
            }
            string gpunames = getgpunames();
            if (!gpunames.Contains("RTX") && !gpunames.Contains("XT"))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Your GPU is quite weak or no GPU found you might lag during the use of aimmy");
                Console.ResetColor();

            }
            Console.Title = $"You're so lazy | Themida | {version}";

            Console.WriteLine("This Program will install VC redistributables, Dotnet version 8 and 7.0.17 and the latest aimmy version.");
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();

            if (!CheckRights() && updatemode == false)
            {
                Console.Beep(1000, 200);
                Console.Beep(1000, 200);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("You're running Lazy Aimmy without admin privileges it will ask");
                Console.WriteLine("Press enter to continue");

                Console.ReadLine();
                using(Process process  = new Process())
                {
                    process.StartInfo.FileName = Process.GetCurrentProcess().ProcessName;
                    process.StartInfo.Verb = "runas";
                    if (SkipAimmy) {
                        process.StartInfo.Arguments = "SkipAimmy";
                    }
                    process.Start();

                }
                Environment.Exit(0);
            }
            
            Console.Clear();
            if (Directory.Exists("Aimmy"))
            {
                Console.WriteLine("Aimmy already installed, Do you want to reinstall/ update it? [Y/N]");
                string userresponse = Console.ReadLine().ToUpper();
                if (userresponse == "Y" || userresponse == "YES")
                {
                    updatemode = true;
                    try
                    {
                        Directory.Move("Aimmy//bin", Directory.GetCurrentDirectory() + "\\bin");
                        binfoldermoved = true;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;

                        Console.WriteLine("Lazy Aimmy attempted to backup your bin folder but something went wrong error:");
                        Console.WriteLine(ex.Message + "\n");
                        Console.WriteLine("Update has been frozen if you want to back this folder up yourself to continue press enter");
                        Console.ReadLine();
                        Console.ResetColor();

                    }
                    Directory.Delete("Aimmy", recursive: true);
                }
                else
                {
                    Environment.Exit(0);
                }
            }

            if (!updatemode)
            {
                await DownloadEssentials();
            }

            Console.Title = $"You're so lazy | Themida | Getting lastest version | {version}";

            if (!SkipAimmy)
            {
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
                Filedownloader.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
                Filedownloader.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(DownloadFileCompletedCallback);

                Console.WriteLine("Downloading latest version of aimmy");
                await DownloadFileAsync(Filedownloader, downloadUrl, "Aimmy.zip");

                Console.WriteLine("Creating a Aimmy Folder");
                Directory.CreateDirectory("Aimmy");

                Console.WriteLine("Extracting Aimmy");
                ExtractZipFile("Aimmy.zip", "Aimmy");
                Console.WriteLine("Deleting the Aimmy zip file");
                File.Delete("Aimmy.zip");
                if (updatemode && binfoldermoved)
                {
                    Directory.Delete("Aimmy\\bin", recursive: true);
                    Directory.Move(Directory.GetCurrentDirectory() + "\\bin", "Aimmy//bin");
                }
                Process.Start(Directory.GetCurrentDirectory() + "\\Aimmy");
                Console.Title = $"You're so lazy | Themida | Done | {version}";
                Console.WriteLine("You're all done you should find aimmy inside your current directory");
                Console.WriteLine("Would you like to Launch Aimmy? Y/N");
                string Response = Console.ReadLine().ToUpper();
                if (Response == "Y")
                {
                    Console.WriteLine("Launching Aimmy");
                    using (Process LaunchAimmy = new Process())
                    {
                        LaunchAimmy.StartInfo.FileName = Directory.GetCurrentDirectory() + "\\Aimmy\\AimmyLauncher.exe";
                        LaunchAimmy.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory() + "\\Aimmy";
                        LaunchAimmy.Start();
                    }
                    //Process.Start(Directory.GetCurrentDirectory() + "\\Aimmy\\AimmyLauncher.exe");
                }
                Environment.Exit(0);
            }
            else
            {
                Console.Title = $"You're so lazy | Themida | Done | {version}";
                Console.WriteLine("You're all done | Press Enter to exit");
                Console.ReadLine();
                Environment.Exit(0);
            }
            
            
            
            
            
        }

        static async Task DownloadFileAsync(WebClient client, string url, string destination)
        {
            var tcs = new TaskCompletionSource<bool>();
            client.DownloadFileCompleted += (s, e) =>
            {
                if (e.Error != null)
                {
                    tcs.TrySetException(e.Error);
                }
                else
                {
                    tcs.TrySetResult(true);
                }
            };

            client.DownloadFileAsync(new Uri(url), destination);

            await tcs.Task;
        }

        static void ExtractZipFile(string zipFilePath, string extractPath)
        {
            Console.Title = $"You're so lazy | Themida | Extracting this will take awhile | {version}";

            using (var archive = ArchiveFactory.Open(zipFilePath))
            {
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

        public static async Task DownloadEssentials()
        {
            Console.Title = $"You're so lazy | Themida | Downloading Essentials | {version}";
            try
            {
                Console.WriteLine("----------------------------------");
                WebClient Filedownloader = new WebClient();
                Process Processhandeler = new Process();
                Filedownloader.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
                Filedownloader.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(DownloadFileCompletedCallback);

                Processhandeler.StartInfo.Arguments = "/q /norestart";

                Console.WriteLine("Downloading vc_redist");
                await DownloadFileAsync(Filedownloader, "https://aka.ms/vs/17/release/vc_redist.x64.exe", "vc_redist.x64.exe");

                Processhandeler.StartInfo.FileName = "vc_redist.x64.exe";
                StartProcessWithExitCodeCheck(Processhandeler, "vc_redist setup");

                Console.WriteLine("Downloading dotnet 7.0.17");
                await DownloadFileAsync(Filedownloader, "https://download.visualstudio.microsoft.com/download/pr/e35dac95-2855-44f9-b6c9-dda018d922ba/fcc2416e232942d81435a659024bd4e5/dotnet-runtime-7.0.17-win-x64.exe", "dotnet7.exe");

                Processhandeler.StartInfo.FileName = "dotnet7.exe";
                StartProcessWithExitCodeCheck(Processhandeler, "dotnet 7 setup");

                Console.WriteLine("Downloading dotnet8");
                await DownloadFileAsync(Filedownloader, "https://download.visualstudio.microsoft.com/download/pr/84ba33d4-4407-4572-9bfa-414d26e7c67c/bb81f8c9e6c9ee1ca547396f6e71b65f/windowsdesktop-runtime-8.0.2-win-x64.exe", "dotnet8.exe");

                Processhandeler.StartInfo.FileName = "dotnet8.exe";
                StartProcessWithExitCodeCheck(Processhandeler, "dotnet 8 setup");

                Console.WriteLine("Cleaning up");
                try
                {
                    File.Delete("vc_redist.x64.exe");
                    File.Delete("dotnet8.exe");
                    File.Delete("dotnet7.exe");
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine("Failed to delete a file, some setup files may remain");
                }

                Console.WriteLine("Done");
                Console.WriteLine("----------------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred");
                Console.WriteLine(ex.ToString() + "\n");
                Console.WriteLine("Would you like to continue? This may cause an incomplete install Y/N");
                string dec = Console.ReadLine().ToUpper();
                if (dec != "Y")
                {
                    Environment.Exit(0);
                }
            }
        }

        static void StartProcessWithExitCodeCheck(Process process, string setupName)
        {
            
            process.Start();
            process.WaitForExit();
            if (process.ExitCode == 0 || process.ExitCode == 3010) 
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(" | Installer returned Success\n");
            }
            else if (process.ExitCode == 1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(" | Installer returned Failure\n");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($" | installer returned Code {process.ExitCode}\n");
            }
            Console.ResetColor();
            Console.WriteLine($"{setupName} finished");
            Console.WriteLine();
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

        private static void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            Console.Write($"\rDownload progress: {e.ProgressPercentage}%");
        }

        private static void DownloadFileCompletedCallback(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                Console.WriteLine("\nDownload failed: " + e.Error.Message);
            }
            else
            {
                Console.WriteLine("\nDownload completed successfully!");
            }
        }
        public static string getgpunames()
        {
            try
            {
                using (ManagementObjectSearcher MOS = new ManagementObjectSearcher("SELECT * FROM win32_VideoController"))
                using (ManagementObjectCollection objects = MOS.Get())
                {
                    StringBuilder allgpus = new StringBuilder();
                    foreach (ManagementObject obj in objects)
                    {
                        string gpuname = obj["Name"].ToString();
                        allgpus.Append(gpuname + " ");
                    }
                    Console.WriteLine(allgpus.ToString());
                    return allgpus.ToString();
                }
            }
            catch
            {
                return "An Error Occured trying to fetch GPU info";
            }
                                   
        }
    }
}
