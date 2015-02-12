using System.Text;
using System.Windows;

namespace Rdr
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult mbr = MessageBox.Show("Do you really want to Quit?", "Quit", MessageBoxButton.YesNo, MessageBoxImage.Question);

            switch (mbr)
            {
                case MessageBoxResult.Yes:
                    e.Cancel = false;
                    break;
                case MessageBoxResult.No:
                    e.Cancel = true;
                    break;
                default:
                    e.Cancel = true;
                    break;
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F1)
            {
                StringBuilder sb = new StringBuilder();

                foreach (RdrFeedItem each in feedManager.Items)
                {
                    sb.AppendLine(each.ToString());
                }

                Utils.LogMessage(sb.ToString());
            }
        }
    }
}
