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

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Views.AutoActionsMainView mainView = new Views.AutoActionsMainView();
            if (!Globals.Instance.Settings.StartMinimizedToTray)
                mainView.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
                mutex.ReleaseMutex();

        }
    }
}
