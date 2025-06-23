using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlcmdGuiApp
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<SqlParameter> Parameters { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            // Hook the event after initialization to avoid early invocation while components load
            AuthComboBox.SelectionChanged += AuthComboBox_SelectionChanged;
            
            ParametersPanel.ItemsSource = Parameters;
        }

        private void AuthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SqlAuthPanel == null) return; // may be null during XAML load
            SqlAuthPanel.Visibility =
                AuthComboBox.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
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

            try
            {
                var process = Process.Start(psi);
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start sqlcmd process.");
                }
                process.WaitForExit();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                var window = new OutputWindow(output, error);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                App.LogError(ex.ToString());
                MessageBox.Show("Failed to execute sqlcmd. See error.log for details.");
            }
        }
    }

    public class SqlParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
