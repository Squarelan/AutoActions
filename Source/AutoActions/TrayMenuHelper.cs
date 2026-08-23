using Hardcodet.Wpf.TaskbarNotification;
using Hardcodet.Wpf.TaskbarNotification.Interop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AutoActions.ProjectResources;
using AutoActions.Displays;
using Microsoft.Win32;
using CodectoryCore.UI.Wpf;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AutoActions
{
    public class TrayMenuHelper
    {
        public bool Initialized { get; private set; }
        TaskbarIcon _trayMenu;

        private MenuItem _openButton;
        private MenuItem _closeButton;
        private MenuItem _appplications;
        private MenuItem _actions;


        public event EventHandler OpenViewRequested;
        public event EventHandler CloseApplicationRequested;


        public event EventHandler<string> NewLog;


        public void Initialize()
        {
            if (Initialized)
                return;
            CallNewLog("Initializing tray menu");
            try
            {
                _trayMenu = new TaskbarIcon();
                _trayMenu.Visibility = Visibility.Visible;
                _trayMenu.ToolTipText = ProjectLocales.AutoActions;
                _trayMenu.Icon = ProjectLocales.MainIcon;
                ContextMenu contextMenu = new ContextMenu();
                _openButton = new MenuItem()
                {
                    Header = ProjectLocales.Open
                };

                _closeButton = new MenuItem()
                {
                    Header = ProjectLocales.Shutdown
                };
                _openButton.Click += (o, e) => OpenViewRequested?.Invoke(this, EventArgs.Empty);
                _closeButton.Click += (o, e) => CloseApplicationRequested?.Invoke(this, EventArgs.Empty);
                contextMenu.Items.Add(_openButton);
                InitializeApplicationsMenuItem(contextMenu);
                InitializeActionsMenuItem(contextMenu);
                contextMenu.Items.Add(_closeButton);
                _trayMenu.ContextMenu = contextMenu;
                _trayMenu.TrayLeftMouseDown += TrayMenu_TrayLeftMouseDown;
                _trayMenu.PreviewTrayToolTipOpen += TrayMenu_PreviewTrayToolTipOpen;
                ApplyNativeTrayToolTip();
                CallNewLog("Tray menu initialized");
                SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;


            }
            catch
            {
                throw; // rethrow without resetting the stack trace
            }

        }

        readonly object _lockActions = new object();

        private void InitializeActionsMenuItem(ContextMenu contextMenu)
        {
            _actions = new MenuItem()
            {
                Header = ProjectLocales.Actions
            };
            contextMenu.Items.Add(_actions);
            Globals.Instance.Settings.ActionShortcuts.CollectionChanged += (o, e) =>
                {
                    switch (e.Action)
                    {
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                            foreach (var item in e.NewItems)
                                ((BaseViewModel)item).PropertyChanged += Actions_PropertyChanged;
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                            foreach (var item in e.OldItems)
                                ((BaseViewModel)item).PropertyChanged -= Actions_PropertyChanged;
                            break;
                    }
                    UpdateActionItems();
                };
            UpdateActionItems();
        }

        private void Actions_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateActionItems();
        }

        private void UpdateActionItems()
        {
            lock (_lockActions)
            {
                _actions.Items.Clear();
                foreach (var action in Globals.Instance.Settings.ActionShortcuts)
                {
                    MenuItem item = new MenuItem();
                    item.Header = action.ShortcutName;
                    item.Click += (o, e) => action.RunAction();
                    _actions.Items.Add(item);
                }
            }
        }

        private void InitializeApplicationsMenuItem(ContextMenu contextMenu)
        {
            _appplications= new MenuItem()
            {
                Header = ProjectLocales.Applications
            };
            contextMenu.Items.Add(_appplications);
            Globals.Instance.Settings.ApplicationProfileAssignments.CollectionChanged += (o, e) =>
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        foreach (var item in e.NewItems)
                            ((BaseViewModel)item).PropertyChanged += TrayMenuHelper_PropertyChanged;
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        foreach (var item in e.OldItems)
                            ((BaseViewModel)item).PropertyChanged -= TrayMenuHelper_PropertyChanged;
                        break;
                }
                UpdatApplicationItems();
            };
            UpdatApplicationItems();
        }

        private void TrayMenuHelper_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdatApplicationItems();
        }

        private void UpdatApplicationItems()
        {
            lock (_lockActions)
            {
                var converter = new CodectoryCore.UI.Wpf.BitmapToBitmapImageConverter();
                _appplications.Items.Clear();
                foreach (var assignment in Globals.Instance.Settings.ApplicationProfileAssignments)
                {
                    MenuItem item = new MenuItem();
                    ImageSource imageSource = (ImageSource)converter.Convert(assignment.Application.Icon, typeof(ImageSource), null, System.Globalization.CultureInfo.CurrentUICulture);
                    item.Icon = new System.Windows.Controls.Image
                    {
                        Source = imageSource,
                        Width = 16,
                        Height = 16
                    };
                    item.Header = assignment.Application.DisplayName;
                    item.Click += (o, e) => assignment.Application.StartApplication();
                    _appplications.Items.Add(item);
                }
            }
        }


        private async void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                SwitchTrayIcon(false);
            }
            else if (e.Mode == PowerModes.Resume)
            {
                // Give the shell time to settle before re-adding the icon, but
                // asynchronously — this event arrives on the UI thread, and
                // blocking here froze the whole interface for the wait duration.
                await System.Threading.Tasks.Task.Delay(5000);
                SwitchTrayIcon(true);
            }
        }

        private void TrayMenu_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            //SwitchTrayIcon(false);
            OpenViewRequested?.Invoke(this, EventArgs.Empty);

        }

        private void TrayMenu_PreviewTrayToolTipOpen(object sender, RoutedEventArgs e)
        {
            // The library's WPF tooltip has no placement anchor and only closes when
            // Explorer sends NIN_POPUPCLOSE. If that message is lost (mouse leaves the
            // icon quickly, Explorer hiccup), the tooltip stays on screen forever,
            // typically pinned to the top-left corner. Never open it — the hover name
            // is shown by the shell-rendered native tooltip instead (see below).
            e.Handled = true;
            ApplyNativeTrayToolTip();
        }

        // Shell_NotifyIcon is not exposed by the tray library (its Util wrapper is
        // internal), so declare it here. NotifyIconData is the library's public,
        // already-marshaled struct.
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NotifyIconData lpData);

        private const uint NIM_MODIFY = 0x1;

        /// <summary>
        /// Registers the icon's hover text as a shell-rendered native tooltip
        /// (NIM_MODIFY with NIF_TIP + NIF_SHOWTIP). NIF_SHOWTIP is cleared by every
        /// subsequent Shell_NotifyIcon call, so it must be re-applied after the icon
        /// is (re-)added: startup, resume from standby, taskbar restart.
        /// </summary>
        private void ApplyNativeTrayToolTip()
        {
            try
            {
                var iconDataField = typeof(TaskbarIcon).GetField("iconData", BindingFlags.NonPublic | BindingFlags.Instance);
                if (iconDataField == null)
                {
                    CallNewLog("Native tray tooltip: iconData field not found (library version changed?)");
                    return;
                }
                // NotifyIconData is a struct — this is a copy, so the library's own
                // state is left untouched.
                var data = (NotifyIconData)iconDataField.GetValue(_trayMenu);
                data.ToolTipText = ProjectLocales.AutoActions;
                data.ValidMembers = IconDataMembers.Tip | IconDataMembers.UseLegacyToolTips;
                Shell_NotifyIcon(NIM_MODIFY, ref data);
            }
            catch (Exception ex)
            {
                CallNewLog($"Applying native tray tooltip failed: {ex.Message}");
            }
        }

        private void CallNewLog(string message)
        {
            NewLog?.Invoke(this, message);
        }

        public void SwitchTrayIcon(bool showTray)
        {
            _trayMenu.Visibility = showTray ? System.Windows.Visibility.Visible : Visibility.Hidden;
            if (showTray)
                ApplyNativeTrayToolTip();
        }

    }
}
