using Hardcodet.Wpf.TaskbarNotification;
using Hardcodet.Wpf.TaskbarNotification.Interop;
using AutoActions.ProjectResources;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

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
                _trayMenu.TrayRightMouseUp += TrayMenu_TrayRightMouseUp;
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

        private void TrayMenu_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            OpenViewRequested?.Invoke(this, EventArgs.Empty);
        }

        // --- System-rendered context menu ------------------------------------
        // The tray context menu is built as a native Win32 menu (CreatePopupMenu +
        // TrackPopupMenuEx) instead of a WPF ContextMenu: on Windows 11 native
        // menus automatically get the system look (rounded corners, acrylic,
        // system font) that a self-drawn WPF popup cannot reproduce. The menu is
        // rebuilt on every right click, so it always reflects the current
        // applications and action shortcuts.

        private const uint CmdOpen = 1;
        private const uint CmdShutdown = 2;
        private const uint CmdApplicationFirst = 1000; // + submenu index
        private const uint CmdActionFirst = 2000;      // + submenu index

        private void TrayMenu_TrayRightMouseUp(object sender, RoutedEventArgs e)
        {
            ShowSystemContextMenu();
        }

        private void ShowSystemContextMenu()
        {
            IntPtr ownerWindow = GetMessageWindowHandle();
            if (ownerWindow == IntPtr.Zero)
            {
                CallNewLog("System context menu: no message window handle");
                return;
            }

            var applications = Globals.Instance.Settings.ApplicationProfileAssignments.ToList();
            var actions = Globals.Instance.Settings.ActionShortcuts.ToList();

            IntPtr menu = CreatePopupMenu();
            if (menu == IntPtr.Zero)
                return;

            var menuBitmaps = new List<IntPtr>();
            try
            {
                uint position = 0;
                AppendMenuItem(menu, position++, CmdOpen, ProjectLocales.Open, IntPtr.Zero);

                IntPtr applicationsMenu = CreatePopupMenu();
                for (int i = 0; i < applications.Count; i++)
                {
                    IntPtr bitmap = CreateMenuBitmap(applications[i].Application.Icon);
                    if (bitmap != IntPtr.Zero)
                        menuBitmaps.Add(bitmap);
                    AppendMenuItem(applicationsMenu, (uint)i, CmdApplicationFirst + (uint)i, applications[i].Application.DisplayName, bitmap);
                }
                AppendSubMenu(menu, position++, applicationsMenu, ProjectLocales.Applications);

                IntPtr actionsMenu = CreatePopupMenu();
                for (int i = 0; i < actions.Count; i++)
                    AppendMenuItem(actionsMenu, (uint)i, CmdActionFirst + (uint)i, actions[i].ShortcutName, IntPtr.Zero);
                AppendSubMenu(menu, position++, actionsMenu, ProjectLocales.Actions);

                AppendSeparator(menu, position++);
                AppendMenuItem(menu, position++, CmdShutdown, ProjectLocales.Shutdown, IntPtr.Zero);

                // Foreground + WM_NULL: without this pair the menu would not close
                // when the user clicks outside of it (standard tray menu pattern).
                SetForegroundWindow(ownerWindow);
                GetCursorPos(out POINT cursor);
                int command = TrackPopupMenuEx(menu,
                    TPM_RETURNCMD | TPM_RIGHTBUTTON | TPM_BOTTOMALIGN | TPM_LEFTALIGN,
                    cursor.X, cursor.Y, ownerWindow, IntPtr.Zero);
                PostMessageW(ownerWindow, WM_NULL, IntPtr.Zero, IntPtr.Zero);

                ExecuteMenuCommand((uint)command, applications, actions);
            }
            catch (Exception ex)
            {
                CallNewLog($"System context menu failed: {ex.Message}");
            }
            finally
            {
                DestroyMenu(menu); // also destroys attached submenus
                foreach (var bitmap in menuBitmaps)
                    DeleteObject(bitmap);
            }
        }

        private void ExecuteMenuCommand(uint command, List<ApplicationProfileAssignment> applications, List<ProfileActionShortcut> actions)
        {
            if (command == 0)
                return; // menu dismissed without a selection
            if (command == CmdOpen)
            {
                OpenViewRequested?.Invoke(this, EventArgs.Empty);
                return;
            }
            if (command == CmdShutdown)
            {
                CloseApplicationRequested?.Invoke(this, EventArgs.Empty);
                return;
            }
            if (command >= CmdApplicationFirst && command < CmdActionFirst)
            {
                int index = (int)(command - CmdApplicationFirst);
                if (index < applications.Count)
                    applications[index].Application.StartApplication();
                return;
            }
            if (command >= CmdActionFirst)
            {
                int index = (int)(command - CmdActionFirst);
                if (index < actions.Count)
                    actions[index].RunAction();
            }
        }

        private static void AppendMenuItem(IntPtr menu, uint position, uint id, string text, IntPtr bitmap)
        {
            var info = CreateMenuItemInfo();
            info.fMask = MIIM_ID | MIIM_STRING;
            if (bitmap != IntPtr.Zero)
                info.fMask |= MIIM_BITMAP;
            info.wID = id;
            info.dwTypeData = text;
            info.hbmpItem = bitmap;
            InsertMenuItemW(menu, position, true, ref info);
        }

        private static void AppendSubMenu(IntPtr menu, uint position, IntPtr submenu, string text)
        {
            var info = CreateMenuItemInfo();
            info.fMask = MIIM_SUBMENU | MIIM_STRING;
            info.hSubMenu = submenu;
            info.dwTypeData = text;
            InsertMenuItemW(menu, position, true, ref info);
        }

        private static void AppendSeparator(IntPtr menu, uint position)
        {
            var info = CreateMenuItemInfo();
            info.fMask = MIIM_FTYPE;
            info.fType = MFT_SEPARATOR;
            InsertMenuItemW(menu, position, true, ref info);
        }

        private static MENUITEMINFO CreateMenuItemInfo()
        {
            var info = new MENUITEMINFO();
            info.cbSize = (uint)Marshal.SizeOf(typeof(MENUITEMINFO));
            return info;
        }

        // 16x16, premultiplied 32-bpp ARGB: menu item bitmaps (hbmpItem) only
        // render with correct transparency when supplied as PARGB DIB sections.
        private static IntPtr CreateMenuBitmap(Bitmap icon)
        {
            try
            {
                using (var bitmap = new Bitmap(16, 16, PixelFormat.Format32bppArgb))
                {
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        if (icon != null)
                            graphics.DrawImage(icon, new Rectangle(0, 0, 16, 16));
                        else
                            graphics.DrawIcon(SystemIcons.Application, new Rectangle(0, 0, 16, 16));
                    }
                    return CreatePremultipliedAlphaBitmap(bitmap);
                }
            }
            catch
            {
                return IntPtr.Zero; // a broken icon must not break the menu
            }
        }

        private static IntPtr CreatePremultipliedAlphaBitmap(Bitmap source)
        {
            var bounds = new Rectangle(0, 0, source.Width, source.Height);
            var data = source.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var info = new BITMAPINFO();
                info.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
                info.bmiHeader.biWidth = source.Width;
                info.bmiHeader.biHeight = -source.Height; // top-down rows
                info.bmiHeader.biPlanes = 1;
                info.bmiHeader.biBitCount = 32;
                info.bmiHeader.biCompression = BI_RGB;

                IntPtr hBitmap = CreateDIBSection(IntPtr.Zero, ref info, DIB_RGB_COLORS, out IntPtr bits, IntPtr.Zero, 0);
                if (hBitmap == IntPtr.Zero)
                    return IntPtr.Zero;

                int byteCount = source.Width * source.Height * 4;
                var pixels = new byte[byteCount];
                Marshal.Copy(data.Scan0, pixels, 0, byteCount);
                for (int i = 0; i < byteCount; i += 4)
                {
                    byte alpha = pixels[i + 3];
                    pixels[i] = (byte)(pixels[i] * alpha / 255);         // B
                    pixels[i + 1] = (byte)(pixels[i + 1] * alpha / 255); // G
                    pixels[i + 2] = (byte)(pixels[i + 2] * alpha / 255); // R
                }
                Marshal.Copy(pixels, 0, bits, byteCount);
                return hBitmap;
            }
            finally
            {
                source.UnlockBits(data);
            }
        }

        private IntPtr GetMessageWindowHandle()
        {
            // The library's message window is internal; reach it via reflection
            // (library version is pinned to 1.1.0).
            var sink = typeof(TaskbarIcon).GetField("messageSink", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(_trayMenu);
            var handle = sink?.GetType().GetProperty("MessageWindowHandle",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)?.GetValue(sink);
            return handle is IntPtr ptr ? ptr : IntPtr.Zero;
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

        // --- Win32 interop -----------------------------------------------------

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MENUITEMINFO
        {
            public uint cbSize;
            public uint fMask;
            public uint fType;
            public uint fState;
            public uint wID;
            public IntPtr hSubMenu;
            public IntPtr hbmpChecked;
            public IntPtr hbmpUnchecked;
            public IntPtr dwItemData;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string dwTypeData;
            public uint cch;
            public IntPtr hbmpItem;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            public uint bmiColors; // placeholder RGBQUAD for structure alignment
        }

        private const uint MIIM_ID = 0x2;
        private const uint MIIM_SUBMENU = 0x4;
        private const uint MIIM_STRING = 0x40;
        private const uint MIIM_BITMAP = 0x80;
        private const uint MIIM_FTYPE = 0x100;
        private const uint MFT_SEPARATOR = 0x1;
        private const uint TPM_LEFTALIGN = 0x0;
        private const uint TPM_RIGHTBUTTON = 0x2;
        private const uint TPM_BOTTOMALIGN = 0x20;
        private const uint TPM_RETURNCMD = 0x100;
        private const uint WM_NULL = 0x0;
        private const uint BI_RGB = 0;
        private const uint DIB_RGB_COLORS = 0;

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool InsertMenuItemW(IntPtr menu, uint position, bool byPosition, ref MENUITEMINFO info);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern int TrackPopupMenuEx(IntPtr menu, uint flags, int x, int y, IntPtr window, IntPtr parameters);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DestroyMenu(IntPtr menu);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern bool SetForegroundWindow(IntPtr window);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool GetCursorPos(out POINT point);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool PostMessageW(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO info, uint usage, out IntPtr bits, IntPtr section, uint offset);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

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
