using System;
using System.Windows;

namespace WinPOS
{
    public partial class ZReportWindow : Window
    {
        public ZReportWindow(Action closeAction)
        {
            InitializeComponent();
            Closing += (_, __) => closeAction?.Invoke();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
