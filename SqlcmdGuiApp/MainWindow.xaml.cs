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
using System.Security.Principal;
using System.Text.Json;

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

            // Set initial visibility and account information
            UpdateAuthPanels();
        }

        private void AuthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAuthPanels();
        }

        private void UpdateAuthPanels()
        {
            if (SqlAuthPanel == null || WindowsAuthPanel == null) return; // may be null during XAML load
            var useSqlAuth = AuthComboBox.SelectedIndex == 1;
            SqlAuthPanel.Visibility = useSqlAuth ? Visibility.Visible : Visibility.Collapsed;
            WindowsAuthPanel.Visibility = useSqlAuth ? Visibility.Collapsed : Visibility.Visible;
            if (!useSqlAuth)
            {
                WindowsAccountTextBlock.Text = System.Security.Principal.WindowsIdentity.GetCurrent()?.Name ?? string.Empty;
            }
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
            NoParametersTextBlock.Visibility = Visibility.Collapsed;
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
            if (Parameters.Count == 0)
            {
                NoParametersTextBlock.Visibility = Visibility.Visible;
            }
        }

        private List<string> BuildSqlcmdArguments(bool includeInputFile)
        {
            // Use -b so sqlcmd returns a non-zero exit code on errors
            var args = new List<string> { "-b", "-S", ServerTextBox.Text };

            if (!string.IsNullOrEmpty(DatabaseTextBox.Text))
            {
                args.Add("-d");
                args.Add(DatabaseTextBox.Text);
            }

            if (AuthComboBox.SelectedIndex == 1)
            {
                args.Add("-U");
                args.Add(UserTextBox.Text);
                args.Add("-P");
                args.Add(PasswordBox.Password);
            }
            else
            {
                args.Add("-E");
            }

            if (EncryptCheckBox.IsChecked == true)
            {
                args.Add("-N");
            }

            if (TrustServerCertificateCheckBox.IsChecked == true)
            {
                args.Add("-C");
            }

            if (ReadOnlyIntentCheckBox.IsChecked == true)
            {
                args.Add("-K");
                args.Add("ReadOnly");
            }

            foreach (var p in Parameters)
            {
                args.Add("-v");
                args.Add($"{p.Name}={p.Value}");
            }

            if (includeInputFile)
            {
                args.Add("-i");
                args.Add(FilePathTextBox.Text);
            }

            return args;
        }

        private ProcessStartInfo BuildSqlcmdProcessInfo(bool includeInputFile)
        {
            var psi = new ProcessStartInfo("sqlcmd")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            foreach (var arg in BuildSqlcmdArguments(includeInputFile))
            {
                psi.ArgumentList.Add(arg);
            }
            return psi;
        }

        private static string Quote(string arg) => arg.Contains(' ') ? $"\"{arg}\"" : arg;

        private string BuildCommandLine()
        {
            var args = BuildSqlcmdArguments(true).Select(Quote);
            return "sqlcmd " + string.Join(" ", args);
        }

        private void SaveConfigButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*" };
            if (dlg.ShowDialog() != true) return;

            var config = new Configuration
            {
                FilePath = FilePathTextBox.Text,
                Server = ServerTextBox.Text,
                Database = DatabaseTextBox.Text,
                UseSqlAuth = AuthComboBox.SelectedIndex == 1,
                User = UserTextBox.Text,
                Password = PasswordBox.Password,
                Encrypt = EncryptCheckBox.IsChecked == true,
                TrustServerCertificate = TrustServerCertificateCheckBox.IsChecked == true,
                ReadOnlyIntent = ReadOnlyIntentCheckBox.IsChecked == true,
                Parameters = Parameters.Select(p => new SqlParameter { Name = p.Name, Value = p.Value }).ToList()
            };

            try
            {
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dlg.FileName, json);
            }
            catch (Exception ex)
            {
                App.LogError(ex.ToString());
                MessageBox.Show("Failed to save configuration. See error.log for details.");
            }
        }

        private void LoadConfigButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*" };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var json = File.ReadAllText(dlg.FileName);
                var config = JsonSerializer.Deserialize<Configuration>(json);
                if (config == null) throw new InvalidOperationException("Invalid configuration file.");

                FilePathTextBox.Text = config.FilePath;
                ServerTextBox.Text = config.Server;
                DatabaseTextBox.Text = config.Database;
                AuthComboBox.SelectedIndex = config.UseSqlAuth ? 1 : 0;
                UserTextBox.Text = config.User;
                PasswordBox.Password = config.Password;
                EncryptCheckBox.IsChecked = config.Encrypt;
                TrustServerCertificateCheckBox.IsChecked = config.TrustServerCertificate;
                ReadOnlyIntentCheckBox.IsChecked = config.ReadOnlyIntent;

                Parameters.Clear();
                foreach (var p in config.Parameters)
                {
                    Parameters.Add(new SqlParameter { Name = p.Name, Value = p.Value });
                }
                NoParametersTextBlock.Visibility = Parameters.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                App.LogError(ex.ToString());
                MessageBox.Show("Failed to load configuration. See error.log for details.");
            }
        }

        private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(FilePathTextBox.Text))
            {
                MessageBox.Show("SQL file not found.");
                return;
            }

            var psi = BuildSqlcmdProcessInfo(true);

            try
            {
                var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
                var window = new OutputWindow();
                process.OutputDataReceived += (s, ea) =>
                {
                    if (ea.Data != null)
                    {
                        window.AppendOutput(ea.Data + "\n");
                    }
                };
                process.ErrorDataReceived += (s, ea) =>
                {
                    if (ea.Data != null)
                    {
                        window.AppendOutput(ea.Data + "\n");
                    }
                };

                if (!process.Start())
                {
                    throw new InvalidOperationException("Failed to start sqlcmd process.");
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                window.AttachProcess(process);

                window.Show();

                await process.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                App.LogError(ex.ToString());
                MessageBox.Show("Failed to execute sqlcmd. See error.log for details.");
            }
        }

        private void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            var psi = BuildSqlcmdProcessInfo(false);
            psi.ArgumentList.Add("-Q");
            psi.ArgumentList.Add("SELECT 1");

            try
            {
                var process = Process.Start(psi);
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start sqlcmd process.");
                }
                process.WaitForExit();
                var error = process.StandardError.ReadToEnd();

                if (process.ExitCode == 0)
                {
                    MessageBox.Show("Connection successful.");
                }
                else
                {
                    MessageBox.Show(string.IsNullOrWhiteSpace(error) ? "Connection failed." : error);
                }
            }
            catch (Exception ex)
            {
                App.LogError(ex.ToString());
                MessageBox.Show("Failed to execute sqlcmd. See error.log for details.");
            }
        }

        private void ViewCommandLineButton_Click(object sender, RoutedEventArgs e)
        {
            var cmd = BuildCommandLine();
            var window = new CommandLineWindow(cmd);
            window.ShowDialog();
        }
    }

    public class SqlParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
