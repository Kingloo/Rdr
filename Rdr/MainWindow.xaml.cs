using System.Windows;

namespace Rdr
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new FeedManager(this);
        }
    }
}
