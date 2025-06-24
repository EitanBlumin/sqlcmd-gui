using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace SqlcmdGuiApp
{
    public partial class OutputWindow : Window
    {
        private DispatcherTimer? _timer;
        private readonly Stopwatch _stopwatch = new();
        private Process? _process;
        private bool _cancelled;

        public OutputWindow()
        {
            InitializeComponent();
        }

        public OutputWindow(string output, string error) : this()
        {
            OutputTextBox.Text = string.IsNullOrWhiteSpace(error) ? output : output + "\n" + error;
        }

        public void AttachProcess(Process process)
        {
            _process = process;
            _process.Exited += Process_Exited;

            _stopwatch.Start();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) =>
            {
                DurationTextBlock.Text = $"Duration: {_stopwatch.Elapsed:hh\\:mm\\:ss}";
            };
            _timer.Start();

            StatusTextBlock.Text = "Status: Running";
            DurationTextBlock.Text = "Duration: 00:00:00";
        }

        private void Process_Exited(object? sender, EventArgs e)
        {
            _timer?.Stop();
            _stopwatch.Stop();
            Dispatcher.Invoke(() =>
            {
                string status = _cancelled ? "Cancelled" : (_process?.ExitCode == 0 ? "Completed" : "Failed");
                StatusTextBlock.Text = $"Status: {status}";
                DurationTextBlock.Text = $"Duration: {_stopwatch.Elapsed:hh\\:mm\\:ss}";
                StopButton.IsEnabled = false;
            });
        }

        public void AppendOutput(string text)
        {
            Dispatcher.Invoke(() =>
            {
                OutputTextBox.AppendText(text);
                OutputTextBox.ScrollToEnd();
            });
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_process != null && !_process.HasExited)
            {
                _cancelled = true;
                try
                {
                    _process.Kill();
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}
