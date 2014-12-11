using System.Windows;

namespace Orchardizer.UI
{
    /// <summary>
    /// Interaction logic for ThemeWindow.xaml
    /// </summary>
    public partial class ThemeWindow : Window
    {
        public ThemeWindow(ThemeViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {


            DialogResult = true;
        }
    }
}
