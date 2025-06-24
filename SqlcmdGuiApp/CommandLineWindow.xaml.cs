using System.Windows;

namespace SqlcmdGuiApp
{
    public partial class CommandLineWindow : Window
    {
        public CommandLineWindow(string commandLine)
        {
            InitializeComponent();
            CommandTextBox.Text = commandLine;
            CommandTextBox.Focus();
            CommandTextBox.SelectAll();
        }
    }
}
