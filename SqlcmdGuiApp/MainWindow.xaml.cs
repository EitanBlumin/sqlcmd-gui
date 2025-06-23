using System.Windows;
using System.Windows.Controls;

namespace SqlcmdGuiApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Hook the event after initialization to avoid early invocation while components load
            AuthComboBox.SelectionChanged += AuthComboBox_SelectionChanged;
        }

        private void AuthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SqlAuthPanel == null) return; // may be null during XAML load
            SqlAuthPanel.Visibility =
                AuthComboBox.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
