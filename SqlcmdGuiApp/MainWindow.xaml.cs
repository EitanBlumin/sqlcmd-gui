using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace SqlcmdGuiApp
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<SqlParameter> Parameters { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            ParametersPanel.ItemsSource = Parameters;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "SQL Files (*.sql)|*.sql|All Files (*.*)|*.*" };
            if (dlg.ShowDialog() == true)
            {
                FilePathTextBox.Text = dlg.FileName;
                LoadParameters(dlg.FileName);
            }
        }

        private void LoadParameters(string path)
        {
            Parameters.Clear();
            if (!File.Exists(path)) return;
            var text = File.ReadAllText(path);
            var variableRegex = new Regex(@"\$\(([^)]+)\)");
            var setvarRegex = new Regex(@"^\s*:setvar\s+(\w+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var variables = variableRegex.Matches(text).Select(m => m.Groups[1].Value).ToHashSet();
            var defined = setvarRegex.Matches(text).Select(m => m.Groups[1].Value).ToHashSet();
            var needed = variables.Except(defined);
            foreach (var v in needed)
            {
                Parameters.Add(new SqlParameter { Name = v, Value = string.Empty });
            }
        }

        private void AuthComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            SqlAuthPanel.Visibility = AuthComboBox.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(FilePathTextBox.Text))
            {
                MessageBox.Show("SQL file not found.");
                return;
            }

            var psi = new ProcessStartInfo("sqlcmd")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("-S");
            psi.ArgumentList.Add(ServerTextBox.Text);
            if (!string.IsNullOrEmpty(DatabaseTextBox.Text))
            {
                psi.ArgumentList.Add("-d");
                psi.ArgumentList.Add(DatabaseTextBox.Text);
            }

            if (AuthComboBox.SelectedIndex == 1)
            {
                psi.ArgumentList.Add("-U");
                psi.ArgumentList.Add(UserTextBox.Text);
                psi.ArgumentList.Add("-P");
                psi.ArgumentList.Add(PasswordBox.Password);
            }
            else
            {
                psi.ArgumentList.Add("-E");
            }

            foreach (var p in Parameters)
            {
                psi.ArgumentList.Add("-v");
                psi.ArgumentList.Add($"{p.Name}={p.Value}");
            }

            psi.ArgumentList.Add("-i");
            psi.ArgumentList.Add(FilePathTextBox.Text);

            var process = Process.Start(psi);
            process.WaitForExit();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            var window = new OutputWindow(output, error);
            window.ShowDialog();
        }
    }

    public class SqlParameter
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
