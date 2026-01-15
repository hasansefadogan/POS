using System.Windows;

namespace WinPOS
{
    public partial class SaleTypePickerWindow : Window
    {
        public string? SelectedType { get; private set; } // "RESTAURANT" / "TAKEAWAY"

        public SaleTypePickerWindow()
        {
            InitializeComponent();
        }

        private void Restaurant_Click(object sender, RoutedEventArgs e)
        {
            SelectedType = "RESTAURANT";
            DialogResult = true;
            Close();
        }

        private void Takeaway_Click(object sender, RoutedEventArgs e)
        {
            SelectedType = "TAKEAWAY";
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
