using AutoActions.UWP;
using CodectoryCore.UI.Wpf;
using CodectoryCore.Windows;
using CodectoryCore.Windows.FileSystem;
using CodectoryCore.Windows.Icons;
using ControlzEx.Standard;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Windows.ApplicationModel;

namespace AutoActions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UWPApplicationItem : BaseViewModel, IEquatable<UWPApplicationItem>, IApplicationItem
    {
        [JsonProperty]
        public bool PackageError { get; private set; } = false;
        private bool _isUWPWebApp = false;

        private string displayName;
        private string _applicationFilePath;
        private string _applicationName;
        private System.Drawing.Bitmap icon = null;
        //private bool _restartProcess = false;
        private string _uwpFullPackageName = string.Empty;
        private string _uwpFamilyPackageName = string.Empty;
        private string _uwpApplicationID = string.Empty;
        private string _uwpIconPath = string.Empty;
        private string _uwpIdentity = string.Empty;
        Dictionary<UWPApplicationItem, ApplicationState> _lastAppStates = new Dictionary<UWPApplicationItem, ApplicationState>();



        [JsonProperty]
        public string DisplayName { get => displayName; set { displayName = value; OnPropertyChanged(); } }
        [JsonProperty]
        public string ApplicationName { get => _applicationName; set { _applicationName = value; OnPropertyChanged(); } }
        [JsonProperty(Order = 1)]
        public string ApplicationFilePath
        {
            get => _applicationFilePath;
            set { _applicationFilePath = value; OnPropertyChanged(); }
        }
        // public bool RestartProcess { get => _restartProcess; set { _restartProcess = value; OnPropertyChanged(); } }
        [JsonProperty(Order = 0)]
        public bool IsWebApp { get => _isUWPWebApp; set { _isUWPWebApp = value; OnPropertyChanged(); } }
        public Bitmap Icon { get => icon; set { icon = value; OnPropertyChanged(); } }
        [JsonProperty(Order = 1)]
        public string FullPackageName
        {
            get => _uwpFullPackageName;
            set { _uwpFullPackageName = value; OnPropertyChanged(); LoadUWPData(); }
        }

        [JsonProperty(Order = 0)]
        public string FamilyPackageName
        {
            get => _uwpFamilyPackageName;
            set { _uwpFamilyPackageName = value; OnPropertyChanged(); }
        }

        [JsonProperty(Order = 2)]
        public string ApplicationID
        {
            get => _uwpApplicationID;
            set { _uwpApplicationID = value; OnPropertyChanged(); if (string.IsNullOrEmpty(FullPackageName)) LoadUWPData(); }
        }
        public string IconPath
        {
            get => _uwpIconPath;
            set { _uwpIconPath = value; OnPropertyChanged(); }
        }

        [JsonProperty(Order = 0)]
        public string Identity { get => _uwpIdentity; set { _uwpIdentity = value; OnPropertyChanged(); } }

        private UWPApplicationItem()
        {

        }

        public UWPApplicationItem(UWPAppEntry appEntry)
        {
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            ApplicationFilePath = appEntry.ExecutablePath ?? throw new ArgumentNullException(nameof(appEntry.ExecutablePath));
            ApplicationName = new FileInfo(ApplicationFilePath).Name.Replace(".exe", "");
            IsWebApp = appEntry.IsWebApp;
            FamilyPackageName = appEntry.FamilyPackageName;
            _uwpFullPackageName = appEntry.FullPackageName;
            IconPath = appEntry.IconPath;
            ApplicationID = appEntry.ApplicationID;
            Identity = appEntry.Identity;

        }


        private void LoadUWPData()
        {
            string packageNotFound = "[PackageNotFound]_";

            UWPAppEntry uwpApp;
            uwpApp = UWPAppsManager.GetPackage(FamilyPackageName, Identity);
            if (uwpApp == null)
            {
                if (PackageError)
                    return;
                DisplayName = $"{packageNotFound}{DisplayName}";
                IconPath = "";
                Identity = "";
                PackageError = true;
                return;
            }
            PackageError = false;
            if (DisplayName.StartsWith(packageNotFound))
                DisplayName = DisplayName.Substring(packageNotFound.Length, DisplayName.Length - packageNotFound.Length);
            FamilyPackageName = uwpApp.FamilyPackageName;
            _uwpFullPackageName = uwpApp.FullPackageName;
            IconPath = uwpApp.IconPath;
            Identity = uwpApp.Identity;
        }


        public void Restart()
        {
            try
            {
                Globals.Logs.Add($"Restarting application {ApplicationName}", false);
                foreach (Process process in Process.GetProcessesByName(ApplicationName).ToList())
                    if (process.StartTime < Process.GetCurrentProcess().StartTime)
                    {
                        Globals.Logs.Add($"Won't restart application {ApplicationName} as it was running before {ProjectResources.ProjectLocales.AutoActions}.", false);

                        return;
                    }
                Process.GetProcessesByName(ApplicationName).ToList().ForEach(p => p.Kill());
                System.Threading.Thread.Sleep(1500);
                StartApplication();
            }
            catch (Exception ex)
            {
                Globals.Logs.AddException($"Failed to restart process {DisplayName} ({ApplicationFilePath}).", ex);
                throw;
            }
        }

        public void StartApplication()
        {
            Globals.Logs.Add($"Start application {ApplicationName}", false);
            try
            {
                UWP.UWPAppsManager.StartUWPApp(FamilyPackageName, ApplicationID);
                System.Threading.Thread.Sleep(2500);
                var processes = Process.GetProcessesByName(ApplicationName).ToList();
                if (processes.Count > 0)
                {
                    Process foundProcess = new Process();
                    Globals.Logs.Add($"Bring application to front: {ApplicationName}", false);
                    foundProcess = processes[0];
                    if (!foundProcess.HasExited && foundProcess.Responding)
                        Window.BringMainWindowToFront(foundProcess.ProcessName);
                }
                else
                    Globals.Logs.Add($"No started application found: {ApplicationName}", false);

            }
            catch (Exception ex)
            {
                Globals.Logs.AddException(ex);
            }
        }



        #region Overrides 

        public override bool Equals(object obj)
        {
            return Equals(obj as UWPApplicationItem);
        }

        public bool Equals(UWPApplicationItem other)
        {
            return other != null &&
                   _applicationFilePath == other._applicationFilePath;
        }

        public override int GetHashCode()
        {
            int hashCode = 73345580;
            hashCode = hashCode * -152135495 + EqualityComparer<string>.Default.GetHashCode(_applicationFilePath);
            return hashCode;
        }

        public static bool operator ==(UWPApplicationItem left, UWPApplicationItem right)
        {
            return EqualityComparer<UWPApplicationItem>.Default.Equals(left, right);
        }

        public static bool operator !=(UWPApplicationItem left, UWPApplicationItem right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return $"{DisplayName} [{ApplicationName} |{ApplicationFilePath}]";
        }

        #endregion Overrides
    }
}
