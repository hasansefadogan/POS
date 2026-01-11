using System.Windows;
using WinPOS.ViewModels;

namespace WinPOS
{
    public partial class MainWindow : Window
    {
        private enum SaleType { Restaurant, Takeaway }
        private SaleType _currentSaleType = SaleType.Restaurant;

        // Aşağıdaki alanları ekleyin
        private System.Windows.Controls.Button BtnRestaurant;
        private System.Windows.Controls.Button BtnTakeaway;

        public MainWindow()
        {
            InitializeComponent();

            // XAML'den butonları bulup alanlara atayın
            BtnRestaurant = (System.Windows.Controls.Button)FindName("BtnRestaurant");
            BtnTakeaway = (System.Windows.Controls.Button)FindName("BtnTakeaway");

            // Butonlar oluştuğu için burada çağırıyoruz
            UpdateSaleTypeUI();

            // MVVM
            DataContext = new MainViewModel();
        }

        private void Restaurant_Click(object sender, RoutedEventArgs e)
        {
            _currentSaleType = SaleType.Restaurant;
            UpdateSaleTypeUI();

            if (DataContext is MainViewModel vm)
                vm.SetSaleType("RESTAURANT");
        }

        private void Takeaway_Click(object sender, RoutedEventArgs e)
        {
            _currentSaleType = SaleType.Takeaway;
            UpdateSaleTypeUI();

            if (DataContext is MainViewModel vm)
                vm.SetSaleType("TAKEAWAY");
        }


        private void UpdateSaleTypeUI()
        {
            if (BtnRestaurant != null)
                BtnRestaurant.Opacity = (_currentSaleType == SaleType.Restaurant) ? 1.0 : 0.6;

            if (BtnTakeaway != null)
                BtnTakeaway.Opacity = (_currentSaleType == SaleType.Takeaway) ? 1.0 : 0.6;
        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
    }
}
