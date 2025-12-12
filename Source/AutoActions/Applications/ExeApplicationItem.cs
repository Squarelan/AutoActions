using AutoActions.UWP;
using CodectoryCore.UI.Wpf;
using CodectoryCore.Windows;
using CodectoryCore.Windows.FileSystem;
using CodectoryCore.Windows.Icons;
using Newtonsoft.Json;
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
using System.Xml.Serialization;

namespace AutoActions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ExeApplicationItem : BaseViewModel, IEquatable<ExeApplicationItem>, IApplicationItem
    {
        [JsonProperty]


        private string displayName;
        private string _applicationFilePath;
        private string _applicationName;
        private System.Drawing.Bitmap icon = null;
        //private bool _restartProcess = false;


        [JsonProperty]
        public string DisplayName { get => displayName; set { displayName = value; OnPropertyChanged(); } }
        [JsonProperty]
        public string ApplicationName { get => _applicationName; set { _applicationName = value; OnPropertyChanged(); } }
        [JsonProperty(Order = 1)]
        public string ApplicationFilePath
        {
            get => _applicationFilePath;
            set { _applicationFilePath = value; try {Icon = IconHelper.GetFileIcon(value); } catch { } OnPropertyChanged(); }
        }
        // public bool RestartProcess { get => _restartProcess; set { _restartProcess = value; OnPropertyChanged(); } }
        public Bitmap Icon { get => icon; set { icon = value; OnPropertyChanged(); } }

        private ExeApplicationItem()
        {

        }
        public ExeApplicationItem(string displayName, string applicationFilePath)
        {
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            ApplicationFilePath = applicationFilePath ?? throw new ArgumentNullException(nameof(applicationFilePath));
            ApplicationName = new FileInfo(ApplicationFilePath).Name.Replace(".exe", "");
        }

         Dictionary<ExeApplicationItem, ApplicationState> _lastAppStates = new Dictionary<ExeApplicationItem, ApplicationState>();

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

                Process process = new Process();
                process.StartInfo = new ProcessStartInfo(ApplicationFilePath);
                process.Start();

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
            return Equals(obj as ExeApplicationItem);
        }

        public bool Equals(ExeApplicationItem other)
        {
            return other != null &&
                   _applicationFilePath == other._applicationFilePath;
        }

        public override int GetHashCode()
        {
            int hashCode = 734317580;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_applicationFilePath);
            return hashCode;
        }

        public static bool operator ==(ExeApplicationItem left, ExeApplicationItem right)
        {
            return EqualityComparer<ExeApplicationItem>.Default.Equals(left, right);
        }

        public static bool operator !=(ExeApplicationItem left, ExeApplicationItem right)
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
