using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AutoActions.Updater
{
    class Program
    {
        static string temporaryFolder;

        static void Main(string[] args)
        {
            temporaryFolder = GetTemporaryDirectory();
            Console.WriteLine("AutoActions Updater");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("Updating AutoActions...");
            try
            {
                bool download = bool.Parse(args[0]);
                string zip = GetZip(download, args[1]);
                string targetFolder = args[2];
                string callingProcess = args[3];
                Update(zip, targetFolder, callingProcess);
                Console.WriteLine($"Starting {callingProcess}...");
                System.Threading.Thread.Sleep(2000);
                Process.Start(Path.Combine(targetFolder, $"{callingProcess}.exe"));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Press any key to close this window.");
                Console.ReadKey();
            }

            finally
            {
                Directory.Delete(temporaryFolder, true);
            }
        }


        private static string GetZip(bool download, string path)
        {
            string updateZip = Path.Combine(temporaryFolder, "Update.zip");
            if (download)
            {
                Console.WriteLine($"Downloading from {path}...");
                using (WebClient myWebClient = new WebClient())
                {
                    // Download the Web resource and save it into the current filesystem folder.
                    myWebClient.DownloadFile(path, updateZip);
                }
                Console.WriteLine($"Finished download.");

            }
            else
                File.Copy(path, updateZip, true);
            return updateZip;
        }

        private static void Update(string zip, string targetFolder, string callingProcess)
        {
            System.Threading.Thread.Sleep(1000);
            ZipFile.ExtractToDirectory(zip, temporaryFolder);
            Console.WriteLine($"Extrating zip to {temporaryFolder}...");
            File.Delete(zip);
            UpdateData updateData = UpdateData.LoadFromFile(Path.Combine(temporaryFolder, "UpdateData.json"));
            Process[] processes  = Process.GetProcessesByName(callingProcess);
            foreach (var process in processes)
                if (process.StartInfo.WorkingDirectory.ToUpperInvariant().Equals(targetFolder.ToUpperInvariant()))
                    process.Kill();
            List<string> filesToCopy = GetAllFiles(temporaryFolder);
            // Only the update manifest is skipped. The updater's own exe/pdb ARE copied now,
            // so a fixed updater replaces the old one on the next update. This is safe because
            // the running updater is the copy in the "Update" folder, not the one in
            // targetFolder that we overwrite here.
            filesToCopy.Remove(Path.Combine(temporaryFolder, "UpdateData.json"));
            Console.WriteLine($"Updating files...");

            foreach (string file in filesToCopy)
            {
                // Preserve the sub-directory layout (e.g. zh-Hans\, zh-Hant\, de\ satellite
                // assemblies). The old code used only Path.GetFileName(), which flattened every
                // file into the root; localized resource DLLs share the same name across cultures,
                // so they collided and .NET could no longer locate them -> translations silently
                // reverted to English after every auto-update.
                string relativePath = file.Substring(temporaryFolder.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string targetFileName = Path.Combine(targetFolder, relativePath);
                Console.WriteLine($"Updating {targetFileName}");
                string targetSubFolder = Path.GetDirectoryName(targetFileName);
                if (!Directory.Exists(targetSubFolder))
                    Directory.CreateDirectory(targetSubFolder);
                if (File.Exists(targetFileName))
                    File.Delete(targetFileName);
                File.Move(file, targetFileName);
            }
            if (updateData.FilesToDelete.Count > 0)
            Console.WriteLine($"Removing files files...");

            foreach (string file in updateData.FilesToDelete)
            {
                string targetFileName = Path.Combine(targetFolder, Path.GetFileName(file));
                Console.WriteLine($"Removing {targetFileName}");
                if (File.Exists(targetFileName))
                    File.Delete(targetFileName);
            }
        }

        public static string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        private static List<string> GetAllFiles(string directory)
        {
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(directory));
            foreach (var subDirectory in Directory.GetDirectories(directory))
                files.AddRange(GetAllFiles(subDirectory));
            return files;
        }
    }
}
