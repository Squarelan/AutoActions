using Hardcodet.Wpf.TaskbarNotification;
using Hardcodet.Wpf.TaskbarNotification.Interop;
using AutoActions.ProjectResources;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AutoActions
{
    public class TrayMenuHelper
    {
        public bool Initialized { get; private set; }
        TaskbarIcon _trayMenu;

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
                _trayMenu.TrayLeftMouseDown += TrayMenu_TrayLeftMouseDown;
                _trayMenu.PreviewTrayToolTipOpen += TrayMenu_PreviewTrayToolTipOpen;

                // Build the Fluent-styled ContextMenu once and assign it to the
                // TaskbarIcon. The library automatically shows the assigned
                // ContextMenu when the user right-clicks the tray icon — its
                // internal popup host (the message window) correctly manages
                // focus, click-outside dismissal, and submenu placement.
                // Subscribing to TrayRightMouseUp and manually setting
                // IsOpen/PlacementTarget left the menu stuck open and broke
                // submenu positioning, because the host window was minimized.
                var menu = new ContextMenu
                {
                    Style = (System.Windows.Style)Application.Current.FindResource("TrayContextMenuStyle")
                };
                _trayMenu.ContextMenu = menu;
                // Rebuild items on every right click so the menu always
                // reflects the current applications and action shortcuts.
                // TrayRightMouseUp fires before the library opens the menu,
                // so the rebuilt items are shown.
                _trayMenu.TrayRightMouseUp += (s, e) => RebuildMenuItems();

                ApplyNativeTrayToolTip();
                CallNewLog("Tray menu initialized");
                SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            }
            catch
            {
                throw; // rethrow without resetting the stack trace
            }
        }

        private void TrayMenu_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            OpenViewRequested?.Invoke(this, EventArgs.Empty);
        }

        // --- Fluent-styled WPF context menu ---------------------------------
        // The tray context menu is a WPF ContextMenu with a custom template
        // (Controls/4_TrayMenuStyles.xaml): rounded corners, system font,
        // semi-transparent surface, WinUI-style hover highlight, proper
        // submenu arrow. Items are rebuilt on every right click (TrayRightMouseUp)
        // so the menu always reflects the current applications and action
        // shortcuts. Colors are DynamicResource, so the menu follows the
        // light/dark palette merged at startup automatically.

        private void RebuildMenuItems()
        {
            var menu = _trayMenu.ContextMenu;
            if (menu == null)
                return;

            menu.Items.Clear();

            var applications = Globals.Instance.Settings.ApplicationProfileAssignments.ToList();
            var actions = Globals.Instance.Settings.ActionShortcuts.ToList();

            // Open
            var openItem = new MenuItem { Header = ProjectLocales.Open, Tag = "Open" };
            openItem.Click += MenuItem_Click;
            menu.Items.Add(openItem);

            // Applications submenu (with icons)
            var appsMenu = new MenuItem { Header = ProjectLocales.Applications };
            for (int i = 0; i < applications.Count; i++)
            {
                var app = applications[i];
                var item = new MenuItem
                {
                    Header = app.Application.DisplayName,
                    Tag = Tuple.Create("App", i)
                };
                System.Windows.Controls.Image iconImg = ToImage(app.Application.Icon);
                if (iconImg != null)
                    item.Icon = iconImg;
                item.Click += MenuItem_Click;
                appsMenu.Items.Add(item);
            }
            if (appsMenu.Items.Count == 0)
                appsMenu.IsEnabled = false;
            menu.Items.Add(appsMenu);

            // Actions submenu
            var actionsMenu = new MenuItem { Header = ProjectLocales.Actions };
            for (int i = 0; i < actions.Count; i++)
            {
                var item = new MenuItem
                {
                    Header = actions[i].ShortcutName,
                    Tag = Tuple.Create("Action", i)
                };
                item.Click += MenuItem_Click;
                actionsMenu.Items.Add(item);
            }
            if (actionsMenu.Items.Count == 0)
                actionsMenu.IsEnabled = false;
            menu.Items.Add(actionsMenu);

            // Separator + Shutdown
            menu.Items.Add(new Separator());
            var shutdownItem = new MenuItem { Header = ProjectLocales.Shutdown, Tag = "Shutdown" };
            shutdownItem.Click += MenuItem_Click;
            menu.Items.Add(shutdownItem);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuItem item) || !(item.Tag is object tag))
                return;

            var applications = Globals.Instance.Settings.ApplicationProfileAssignments.ToList();
            var actions = Globals.Instance.Settings.ActionShortcuts.ToList();

            if (tag is string s)
            {
                if (s == "Open")
                    OpenViewRequested?.Invoke(this, EventArgs.Empty);
                else if (s == "Shutdown")
                    CloseApplicationRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (tag is Tuple<string, int> t)
            {
                if (t.Item1 == "App" && t.Item2 < applications.Count)
                    applications[t.Item2].Application.StartApplication();
                else if (t.Item1 == "Action" && t.Item2 < actions.Count)
                    actions[t.Item2].RunAction();
            }
        }

        /// <summary>
        /// Converts a System.Drawing.Bitmap (the type ApplicationItem.Icon
        /// holds) to a frozen WPF BitmapSource sized for a 16x16 menu icon.
        /// Returns null on failure so the menu still shows without an icon.
        /// </summary>
        private static System.Windows.Controls.Image ToImage(Bitmap bitmap)
        {
            if (bitmap == null)
                return null;
            try
            {
                BitmapSource bs = Imaging.CreateBitmapSourceFromHBitmap(
                    bitmap.GetHbitmap(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(16, 16));
                bs.Freeze();
                return new System.Windows.Controls.Image
                {
                    Source = bs,
                    Width = 16,
                    Height = 16,
                    Stretch = Stretch.Uniform
                };
            }
            catch
            {
                return null;
            }
        }

        // --- Native tray tooltip ----------------------------------------------

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

        // --- Power mode --------------------------------------------------------

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
                await Task.Delay(5000);
                SwitchTrayIcon(true);
            }
        }

        private void CallNewLog(string message)
        {
            NewLog?.Invoke(this, message);
        }

        public void SwitchTrayIcon(bool showTray)
        {
            _trayMenu.Visibility = showTray ? Visibility.Visible : Visibility.Hidden;
            if (showTray)
                ApplyNativeTrayToolTip();
        }

    }
}
