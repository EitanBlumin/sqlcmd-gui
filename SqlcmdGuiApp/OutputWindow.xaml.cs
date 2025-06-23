using System.Windows;

namespace SqlcmdGuiApp
{
    public partial class OutputWindow : Window
    {
        public OutputWindow(string output, string error)
        {
            InitializeComponent();
            OutputTextBox.Text = string.IsNullOrWhiteSpace(error) ? output : output + "\n" + error;
        }
    }
}
