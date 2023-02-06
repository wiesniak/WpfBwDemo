using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GlobalHook.Core.Keyboard;
using GlobalHook.Core.Windows.Keyboard;

namespace WpfBwDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly BackgroundWorker worker;

        private readonly IKeyboardHook keyboardHook;

        public MainWindow()
        {
            InitializeComponent();

            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.ProgressChanged += Worker_ProgressChanged;

            keyboardHook = new KeyboardHook();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            keyboardHook.Install();
            keyboardHook.KeyDown += KeyboardHook_KeyDown;
        }

        private void KeyboardHook_KeyDown(object? sender, IKeyboardEventArgs e)
        {
            Dispatcher.Invoke(() => LogMessage($"Key down {e.Key}. Are we active? {this.IsActive}"));
        }

        private void Worker_DoWork(object? sender, DoWorkEventArgs e)
        {
            // I could use worker from the class but IMO it would be not nice to reach from thread to outside
            var ownerWorker = (BackgroundWorker)sender!;
            int loopsCounter = 0;
            while (!ownerWorker.CancellationPending)
            {
                loopsCounter++;
                Dispatcher.Invoke(UpdateCounter);
                ownerWorker.ReportProgress(0, loopsCounter);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        private void Worker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            Dispatcher.Invoke(() => LogMessage("Finished"));
        }

        private void Worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            var loopsCounter = (int)e.UserState!;
            Dispatcher.Invoke(() => LogMessage($"I did {loopsCounter} already! ＼(^o^)／"));
        }

        public void UpdateCounter()
        {
            if (!int.TryParse(CounterTextBox.Text, out var value))
            {
                value = 0;
            }

            value++;
            CounterTextBox.Text = value.ToString();
        }

        private void MagicButton_Click(object sender, RoutedEventArgs e)
        {
            // If there is cancellation pending and the worker is still running, we don't want to call CancelAsync multiple times
            if (worker.IsBusy && !worker.CancellationPending)
            {
                worker.CancelAsync();
                LogMessage("Cancelling...");
            }
            else if (!worker.IsBusy)
            {
                worker.RunWorkerAsync();
                LogMessage("Running...");
            }
            else
            {
                LogMessage("Skipping...");
            }
        }

        private void LogMessage(string message)
        {
            var newLine = LogTextBox.Text.Length == 0 ? "" : Environment.NewLine;
            LogTextBox.Text += $"{newLine}[{DateTime.Now}] {message}";
            LogTextBox.ScrollToEnd();
        }
    }
}
