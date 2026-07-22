using System.Windows.Media;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace AutoActions.Theming
{
    /// <summary>
    /// One-time WPF UI (Fluent) appearance setup. Drives WPF UI from the same theme we
    /// already detected from the Windows registry (<see cref="App.Theme"/>), so the Fluent
    /// controls match the rest of the app, and forces the project's red accent (#FF6666)
    /// instead of the Windows accent colour.
    ///
    /// Isolated on purpose: if it ever fails to compile against the installed WPF-UI
    /// version, removing this file (and the call in App.Main) lets the app still build.
    /// </summary>
    public static class FluentThemeSetup
    {
        private static readonly Color AccentRed = Color.FromRgb(0xFF, 0x66, 0x66);

        public static void Initialize()
        {
            try
            {
                ApplicationTheme theme = AutoActions.App.Theme == Theme.Dark
                    ? ApplicationTheme.Dark
                    : ApplicationTheme.Light;

                // Explicitly apply the detected theme (do NOT rely on WPF UI's own system
                // detection, which did not match). No window backdrop (Mica) - the app's
                // window derives from a closed base class that does not cooperate with a
                // transparent/Mica window, so we keep a solid themed background instead.
                ApplicationThemeManager.Apply(theme, WindowBackdropType.None, false);
                ApplicationAccentColorManager.Apply(AccentRed, theme, false);
            }
            catch
            {
                // Non-fatal: fall back to WPF UI defaults.
            }
        }
    }
}
