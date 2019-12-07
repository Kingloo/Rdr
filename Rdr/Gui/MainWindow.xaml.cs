using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace Rdr.Gui
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel vm;

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();

            Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);

            vm = viewModel;

            DataContext = vm;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await vm.ReloadAsync();

            await vm.RefreshAllAsync();

            vm.StartTimer();
        }
    }
}
