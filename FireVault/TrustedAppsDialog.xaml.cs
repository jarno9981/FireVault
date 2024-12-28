using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FireVault
{
    public sealed partial class TrustedAppsDialog : UserControl
    {
        public ObservableCollection<string> TrustedApps { get; private set; }

        public TrustedAppsDialog(List<string> trustedApps)
        {
            this.InitializeComponent();
            TrustedApps = new ObservableCollection<string>(trustedApps);
            TrustedAppsListView.ItemsSource = TrustedApps;
        }

        private void AddApp_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NewAppTextBox.Text))
            {
                TrustedApps.Add(NewAppTextBox.Text);
                NewAppTextBox.Text = string.Empty;
            }
        }

        private void RemoveApp_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var appName = button.DataContext as string;
            TrustedApps.Remove(appName);
        }
    }
}

