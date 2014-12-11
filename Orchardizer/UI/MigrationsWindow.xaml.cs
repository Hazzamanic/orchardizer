using System.Windows;

namespace Orchardizer.UI
{
    /// <summary>
    /// Interaction logic for MigrationsWindow.xaml
    /// </summary>
    public partial class MigrationsWindow : Window
    {
        public MigrationsWindow(MigrationsViewModel viewModel)
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
