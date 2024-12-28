using Microsoft.UI.Xaml.Controls;

namespace FireVault
{
    public sealed partial class AddItemDialog : UserControl
    {
        public AddItemDialog()
        {
            this.InitializeComponent();
        }

        public string Title => TitleBox.Text;
        public string Data => DataBox.Text;
        public string Type => (TypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
        public string Password => PasswordBox.Password;
    }
}

