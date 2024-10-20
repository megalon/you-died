using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace you_died
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DateTime _lastFrameTime;

        private double _targetOpacity;
        private double _lerpAmount;
        private const double _maxOpacity = 0.6;

        private Process[] _processesToMonitor;

        public MainWindow()
        {
            InitializeComponent();

            _lastFrameTime = DateTime.Now;

            Visibility = Visibility.Hidden;

            CompositionTarget.Rendering += Update;

            _processesToMonitor = Process.GetProcesses();

            foreach (Process process in _processesToMonitor)
            {
                // Skip system processes
                if (process.SessionId == 0)
                {
                    continue;
                }

                try
                {
                    process.EnableRaisingEvents = true;
                    process.Exited += _processToMonitor_Exited;
                } catch { }
            }
        }

        private void _processToMonitor_Exited(object? sender, EventArgs e)
        {
            Process process = sender as Process;

            if (process == null) return;

            int exitCode = process.ExitCode;

            if (exitCode == 0)
            {
                // We are in a different thread, so
                // Access the Dispatcher of the main window
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Main.Opacity = 0;

                    Visibility = Visibility.Visible;

                    _lerpAmount = 0;
                    _targetOpacity = _maxOpacity;
                    
                    Message.Content = $"{process.ProcessName} EXPLODED";
                });
            } else
            {
                MessageBox.Show("App has crashed");
            }
        }

        private void Update(object sender, EventArgs e)
        {
            DateTime currentFrameTime = DateTime.Now;

            double deltaTime = (currentFrameTime - _lastFrameTime).TotalSeconds;

            UpdateMainOpacity(deltaTime);

            _lastFrameTime = currentFrameTime;
        }

        private void UpdateMainOpacity(double deltaTime)
        {
            _lerpAmount += deltaTime / 10;
            Main.Opacity = double.Lerp(Main.Opacity, _targetOpacity, _lerpAmount);

            if (Main.Opacity <= 0)
            {
                // Even though opacity is zero, we do this to hide the app from alt-tab
                Visibility = Visibility.Hidden;
            }

            // Fix if opacity goes over
            if (Main.Opacity > _maxOpacity) { 
                Main.Opacity = _maxOpacity;
            }

            if (Main.Opacity >= _maxOpacity)
            {
                _lerpAmount = 0;
                _targetOpacity = 0;
            }
        }

        private async Task Delay()
        {
            Debug.WriteLine("Waiting....");
            await Task.Delay(1000);
            Debug.WriteLine("Done waiting!");
        }
    }
}