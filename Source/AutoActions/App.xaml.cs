using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AutoActions.ProjectResources;
using AutoActions.Theming;

namespace AutoActions
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    ///
    public partial class App : Application
    {
        public static Theme Theme { get; set; } = Theme.Light;

        static Mutex mutex;

        [STAThread]
        public static void Main()
         {
         bool createNew = false;
            mutex = new Mutex(true, "{2846416C-610B-4A6B-A31C-A4AA6826E9BE}", out createNew);
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                var application = new App();
                application.InitializeComponent();
                // Pick the UI theme from the current Windows setting before any window
                // is shown. Dark overrides are layered on top of the (light) defaults.
                ApplyWindowsTheme(application);
                // WPF UI (Fluent) appearance: follow Windows light/dark + force red accent.
                AutoActions.Theming.FluentThemeSetup.Initialize();
                // Remove the leftover "Update" folder from a previous auto-update. AutoUpdate()
                // copies the whole app there to run the updater from, but never cleans it up
                // afterwards, so it lingered in the install directory. Safe to delete on startup.
                CleanupUpdateFolder();
                Globals.Instance.LoadSettings();
                // Apply the saved UI language before any window is created.
                // Empty setting = follow the Windows system locale (automatic detection).
                LanguageOption.ApplyLanguage(Globals.Instance.Settings.Language);
                application.Run();
            }
            else
            {
                MessageBox.Show(ProjectLocales.AlreadyRunning);
            }
        }

        private static void CleanupUpdateFolder()
        {
            try
            {
                string updatePath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Update");
                if (System.IO.Directory.Exists(updatePath))
                    System.IO.Directory.Delete(updatePath, true);
            }
            catch
            {
                // A locked leftover file must never block startup; the folder is harmless.
            }
        }

        /// <summary>
        /// Selects the UI theme based on the current Windows app theme. Light is the
        /// baked-in default (Controls/AppResources.xaml); when Windows is in dark mode
        /// the dark palette (Theming/DarkColors.xaml) is merged on top so that every
        /// DynamicResource brush reference resolves to its dark value. Must run before
        /// any window is created. The chosen theme is fixed for the session - changing
        /// the Windows theme takes effect after the app is restarted.
        /// </summary>
        private static void ApplyWindowsTheme(App application)
        {
            try
            {
                Theme = AutoActions.Windows.UI.GetWindowsTheme() == AutoActions.Windows.WindowsTheme.Dark
                    ? Theme.Dark
                    : Theme.Light;

                if (Theme == Theme.Dark)
                {
                    var darkColors = new ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/Theming/DarkColors.xaml", UriKind.Absolute)
                    };
                    application.Resources.MergedDictionaries.Add(darkColors);
                }
            }
            catch
            {
                // Any failure (registry read, dictionary load) is non-fatal: fall back
                // to the light theme that is already in effect.
                Theme = Theme.Light;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Apply the dark title bar to EVERY window (main window + all DialogService
            // dialogs), not just the main one. A class-level Loaded handler runs for each
            // Window once its HWND exists. Only needed in dark mode (light keeps the default
            // light caption bar). The theme is fixed for the session (set at startup).
            if (Theme == Theme.Dark)
            {
                EventManager.RegisterClassHandler(typeof(System.Windows.Window),
                    System.Windows.Window.LoadedEvent,
                    new RoutedEventHandler(OnAnyWindowLoaded));
            }
            Views.AutoActionsMainView mainView = new Views.AutoActionsMainView();
            if (!Globals.Instance.Settings.StartMinimizedToTray)
                mainView.Show();
        }

        // Sets the immersive dark title bar on any window as it loads (dark theme only).
        private static void OnAnyWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Window window)
                {
                    IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
                    WinAPIFunctions.UseImmersiveDarkMode(hwnd, true);
                }
            }
            catch { }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
                mutex.ReleaseMutex();

        }
    }
}
