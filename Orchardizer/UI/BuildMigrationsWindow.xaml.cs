using System.Windows;

namespace Orchardizer.UI
{
    /// <summary>
    /// Interaction logic for MigrationsWindow.xaml
    /// </summary>
    public partial class BuildMigrationsWindow : Window
    {
        public BuildMigrationsWindow(BuildMigrationsViewModel viewModel)
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
