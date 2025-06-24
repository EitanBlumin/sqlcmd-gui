using System.Windows;

namespace SqlcmdGuiApp
{
    public partial class OutputWindow : Window
    {
        public OutputWindow()
        {
            InitializeComponent();
        }

        public OutputWindow(string output, string error) : this()
        {
            OutputTextBox.Text = string.IsNullOrWhiteSpace(error) ? output : output + "\n" + error;
        }

        public void AppendOutput(string text)
        {
            Dispatcher.Invoke(() =>
            {
                OutputTextBox.AppendText(text);
                OutputTextBox.ScrollToEnd();
            });
        }
    }
}
