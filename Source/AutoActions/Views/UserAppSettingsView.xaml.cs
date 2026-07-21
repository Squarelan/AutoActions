using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AutoActions.ProjectResources;

namespace AutoActions.Views
{
    /// <summary>
    /// Interaktionslogik für UserAppSettings.xaml
    /// </summary>
    public partial class UserAppSettingsView : UserControl
    {
        public UserAppSettingsView()
        {
            InitializeComponent();
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only react to genuine user changes, not the initial binding load.
            if (!IsLoaded || e.AddedItems.Count == 0)
                return;
            MessageBox.Show(ProjectLocales.RestartRequired, "AutoActions",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
