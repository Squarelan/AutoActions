using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security.Permissions;
using System.Security.Principal;

using Windows.Management.Deployment;
using Windows.ApplicationModel;

namespace AutoActions.UWP
{
    public static class UWPAppsManager
    {
        static PackageManager manager = new PackageManager();


        private const string xboxPassAppFN = "Microsoft.GamingApp_8wekyb3d8bbwe";


        public static UWPAppEntry GetPackage(string packageNameOrFamilyPackageName, string identity = "")
        {
            var package = manager.FindPackageForUser(WindowsIdentity.GetCurrent().User.Value, packageNameOrFamilyPackageName);
            if (package == null)
                return GetUWPAppCompatible(packageNameOrFamilyPackageName, identity);
            return new UWPAppEntry(package);
        }

        private static UWPAppEntry GetUWPAppCompatible(string familyPackageName, string identity)
        {
            foreach (var package in manager.FindPackagesForUser(WindowsIdentity.GetCurrent().User.Value))
            {
                try
                {
                    UWPAppEntry uwpApp = new UWPAppEntry(package);
                    if (uwpApp.FamilyPackageName.Equals(familyPackageName) && (string.IsNullOrEmpty(identity) || uwpApp.Identity.Equals(identity)))
                        return uwpApp;
                }
                catch {}
            }
            return null;
        }


        public static List<UWPApplicationItem> GetUWPApps()
        {

            Globals.Logs.Add($"Retrieving UWP apps...", false);

            List<UWPApplicationItem> uwpApplications = new List<UWPApplicationItem>();
            IEnumerable<Package> packages = manager.FindPackagesForUser(WindowsIdentity.GetCurrent().User.Value);
            try
            {
                foreach (var package in packages)
                {
                    string s = package.DisplayName;
                    if (package.IsFramework || package.IsResourcePackage || package.SignatureKind != PackageSignatureKind.Store )
                    {
                        continue;
                    }

                    try
                    {
                        if (package.InstalledLocation == null)
                        {
                            continue;
                        }
                    }
                    catch
                    {
                        continue;
                    }

                    try
                    {
                        var uwpEntry =  new UWPAppEntry(package);
                        if (!string.IsNullOrEmpty(uwpEntry.ApplicationID))
                         uwpApplications.Add(new UWPApplicationItem(uwpEntry));
                    }
                    catch
                    {
                        continue;
                    }
                }
                return uwpApplications.OrderBy(u => u.DisplayName).ToList();
            }
            catch (Exception ex)
            {
                Globals.Logs.AddException($"Retrieving UWP apps failed.", ex);
                throw;
            }
        }

        public static void StartUWPApp(string FamilyPackage, string applicationID)
        {
            Process process = new Process();
            process.StartInfo.FileName = "explorer.exe";
            process.StartInfo.Arguments = $"shell:AppsFolder\\{FamilyPackage}!{applicationID}";
            process.Start();

        }

    }
}
