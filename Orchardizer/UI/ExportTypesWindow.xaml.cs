using System.Windows;

namespace Orchardizer.UI
{
    /// <summary>
    /// Interaction logic for MigrationsWindow.xaml
    /// </summary>
    public partial class ExportTypesWindow : Window
    {
        public ExportTypesWindow(ExportTypesViewModel viewModel)
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
