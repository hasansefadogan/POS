using System.Windows;

namespace WinPOS
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = Application.Current.MainWindow.DataContext; // MainViewModel
        }
    }
}
