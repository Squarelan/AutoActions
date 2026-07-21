using AutoActions.Displays;
using AutoActions.Profiles;
using AutoActions.Profiles.Actions;
using CodectoryCore;
using CodectoryCore.UI.Wpf;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace AutoActions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UserAppSettings : BaseViewModel
    {
        public static readonly object _settingsLock = new object();

        private bool _globalAutoActions = true;
        private bool _createLogFile = false;
        private bool _autoStart = false;
        private bool _autoUpdate = true;
        private bool _startMinimizedToTray;
        private bool _closeToTray;
        private bool _checkForNewVersion = true;
        private bool _hideSplashScreenOnStartup = false;
        private bool _hideSplashScreenOnAutoUpdate = false;
        private string _language = string.Empty;

        readonly object _audioDevicesLock = new object();
        private Guid _defaultProfileGuid = Guid.Empty;
        private Size _windowSize = new Size(1280, 800);


        private SortableObservableCollection<ApplicationProfileAssignment> _applicationProfileAssignments;
        private DispatchingObservableCollection<Profile> _applicationProfiles;
        private DispatchingObservableCollection<Display> _displays;
        private DispatchingObservableCollection<ProfileActionShortcut> _actionShortcuts;


        [JsonProperty]
        public Guid DefaultProfileGuid { get => _defaultProfileGuid; set { _defaultProfileGuid = value; OnPropertyChanged(); OnPropertyChanged(nameof(DefaultProfile)); } }

        public Profile DefaultProfile { get => ApplicationProfiles.FirstOrDefault(p => p.GUID.Equals(DefaultProfileGuid)); set { DefaultProfileGuid = value == null ? Guid.Empty : value.GUID; } }


        [JsonProperty]
        public bool GlobalAutoActions { get => _globalAutoActions; set { _globalAutoActions = value; OnPropertyChanged(); } }

        [JsonProperty]
        public bool AutoStart { get => _autoStart; set { _autoStart = value; OnPropertyChanged(); } }
        [JsonProperty]
        public bool AutoUpdate { get => _autoUpdate; set { _autoUpdate = value; OnPropertyChanged(); } }

        [JsonProperty]
        public bool HideSplashScreenOnStartup { get => _hideSplashScreenOnStartup; set { _hideSplashScreenOnStartup = value; OnPropertyChanged(); } }


        [JsonProperty]
        public bool HideSplashScreenOnAutoUpdate { get => _hideSplashScreenOnAutoUpdate; set { _hideSplashScreenOnAutoUpdate = value; OnPropertyChanged(); } }


        [JsonProperty]
        public bool CreateLogFile { get => _createLogFile; set { _createLogFile = value; OnPropertyChanged(); } }

        [JsonProperty]
        public bool StartMinimizedToTray { get => _startMinimizedToTray; set { _startMinimizedToTray = value; OnPropertyChanged(); } }

        [JsonProperty]
        public bool CloseToTray { get => _closeToTray; set { _closeToTray = value; OnPropertyChanged(); } }

        [JsonProperty]
        public bool CheckForNewVersion { get => _checkForNewVersion; set { _checkForNewVersion = value; OnPropertyChanged(); } }

        /// <summary>
        /// UI language culture name. Empty string = follow Windows system locale (automatic).
        /// Examples: "" (auto), "en", "de", "zh-Hans", "zh-Hant".
        /// </summary>
        [JsonProperty]
        public string Language { get => _language; set { _language = value ?? string.Empty; OnPropertyChanged(); } }

        /// <summary>
        /// Language options shown in the settings UI. Autonyms are used so each entry
        /// is readable regardless of the currently active UI language.
        /// </summary>
        [JsonIgnore]
        public System.Collections.Generic.IEnumerable<LanguageOption> AvailableLanguages => LanguageOption.All;


        [JsonProperty(Order = 2)]
        public SortableObservableCollection<ApplicationProfileAssignment> ApplicationProfileAssignments { get => _applicationProfileAssignments; set { _applicationProfileAssignments = value; OnPropertyChanged(); } }

        [JsonProperty(Order = 1)]
        public DispatchingObservableCollection<Profile> ApplicationProfiles { get => _applicationProfiles; set { _applicationProfiles = value; OnPropertyChanged(); } }


        [JsonProperty]
        public DispatchingObservableCollection<Display> Displays { get => _displays; set { _displays = value; OnPropertyChanged(); } }

        [JsonProperty]
        public DispatchingObservableCollection<ProfileActionShortcut> ActionShortcuts { get => _actionShortcuts; set { _actionShortcuts = value; OnPropertyChanged(); } }

        [JsonProperty]
        public Size WindowSize { get => _windowSize; set { _windowSize = value; OnPropertyChanged(); } }


        public UserAppSettings()
        {
            ApplicationProfileAssignments = new SortableObservableCollection<ApplicationProfileAssignment>(new ObservableCollection<ApplicationProfileAssignment>());
            ApplicationProfiles = new DispatchingObservableCollection<Profile>();
            ActionShortcuts = new DispatchingObservableCollection<ProfileActionShortcut>();
            Displays = new DispatchingObservableCollection<Display>();
        }

        public static UserAppSettings ReadSettings(string path)
        {
            UserAppSettings settings = null;

            lock (_settingsLock)
            {

                try
            {
                    string serializedJson = File.ReadAllText(path);
                    serializedJson = UpgradeJson(serializedJson);
                    settings = (UserAppSettings)JsonConvert.DeserializeObject<UserAppSettings>(serializedJson, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
                    });
                }
                catch (Exception ex)
                {
                    try
                    {
                        settings = TryReadXML(path);
                        Globals.Logs.Add("Loaded deprecated xml settings.", false);
                        return settings;
                    }
                    catch (Exception)
                    {
                    }
                    Globals.Logs.AddException(ex);
                    throw;
                }
            }
            return settings;
        }

        private static string UpgradeJson(string serializedJson)
        {
            serializedJson = serializedJson.Replace("AutoHDR", "AutoActions");
            serializedJson = serializedJson.Replace("\"$type\": \"AutoActions.Displays.Display, AutoActions\"", "\"$type\": \"AutoActions.Displays.Display, AutoActions.Displays\"");
            serializedJson = serializedJson.Replace("\"Monitors\": [", "\"Displays\": [");
            serializedJson = serializedJson.Replace("\"SetHDR\":", "\"ChangeHDR\":");
            serializedJson = serializedJson.Replace("\"SetResolution\":", "\"ChangeResolution\":");
            serializedJson = serializedJson.Replace("\"SetRefreshRate\":", "\"ChangeRefreshRate\":");
            serializedJson = serializedJson.Replace("\"SetColorDepth\":", "\"ChangeColorDepth\":");
            serializedJson = serializedJson.Replace("\"SetOutput\":", "\"ChangePlaybackDevice\":");
            serializedJson = serializedJson.Replace("\"SetInput\":", "\"ChangeRecordDevice\":");
            serializedJson = serializedJson.Replace("\"OutputDeviceID\":", "\"PlaybackDeviceID\":");
            serializedJson = serializedJson.Replace("\"InputDeviceID\":", "\"RecordDeviceID\":");

            return serializedJson;
        }

        private static UserAppSettings TryReadXML(string path)
        {
            UserAppSettings settings = null;
            XmlSerializer serializer = new XmlSerializer(typeof(UserAppSettings));
            using (TextReader reader = new StreamReader(path))
            {
                settings = (UserAppSettings)serializer.Deserialize(reader);
            }
            return settings;
        }

        public static void SaveSettings(UserAppSettings settings, string path)
        {
            lock (_settingsLock)
            {
                try
                {
                    string serializedJson = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
                    });
                    File.WriteAllText(path, serializedJson);
                }
                catch (Exception ex)
                {
                    Globals.Logs.AddException(ex);
                    throw;
                }
            }
        }
    }

    public static class UserAppSettingsExtension
    {
        public static void SaveSettings(this UserAppSettings settings, string path)
        {
            UserAppSettings.SaveSettings(settings, path);
        }
    }

    /// <summary>
    /// One entry in the language selector. DisplayName uses the language's own script (autonym).
    /// CultureName is the value stored in UserAppSettings.Language.
    /// </summary>
    public class LanguageOption
    {
        public string DisplayName { get; }
        public string CultureName { get; }

        public LanguageOption(string displayName, string cultureName)
        {
            DisplayName = displayName;
            CultureName = cultureName;
        }

        public static readonly System.Collections.Generic.IReadOnlyList<LanguageOption> All =
            new System.Collections.Generic.List<LanguageOption>
            {
                new LanguageOption("Automatic (system) / 自动 / 自動", string.Empty),
                new LanguageOption("English", "en"),
                new LanguageOption("Deutsch", "de"),
                new LanguageOption("简体中文", "zh-Hans"),
                new LanguageOption("繁體中文", "zh-Hant"),
            };

        /// <summary>
        /// Applies the culture stored in <paramref name="cultureName"/> to the current thread.
        /// Empty string = leave the runtime-detected system locale in place.
        /// </summary>
        public static void ApplyLanguage(string cultureName)
        {
            if (string.IsNullOrEmpty(cultureName))
                return;
            try
            {
                var ci = System.Globalization.CultureInfo.GetCultureInfo(cultureName);
                System.Globalization.CultureInfo.DefaultThreadCurrentCulture = ci;
                System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = ci;
                System.Threading.Thread.CurrentThread.CurrentCulture =
                    System.Globalization.CultureInfo.CreateSpecificCulture(ci.Name);
                System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
            }
            catch { /* unknown culture name — silently fall back to system default */ }
        }
    }

}
