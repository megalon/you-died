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
        private double _heartbeatTimer = 0;
        private double _heartbeatMaxTime = 5;

        private Dictionary<int, Process> _processDict;

        public MainWindow()
        {
            InitializeComponent();
            _processDict = new Dictionary<int, Process>();

            _lastFrameTime = DateTime.Now;

            Visibility = Visibility.Hidden;

            CompositionTarget.Rendering += Update;

            CollectProcesses();
        }

        private void CollectProcesses()
        {
            Process[] _allProcesses = Process.GetProcesses();

            foreach (Process process in _allProcesses)
            {
                // Skip system processes
                if (process.SessionId == 0)
                {
                    continue;
                }

                if (_processDict.ContainsKey(process.Id))
                {
                    continue;
                }

                try
                {
                    process.EnableRaisingEvents = true;
                    process.Exited += _processToMonitor_Exited;

                    _processDict.Add(process.Id, process);
                }
                catch { }
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
                    _processDict.Remove(process.Id);
                });
            } else
            {
                _processDict.Remove(process.Id);
            }
        }

        private void Update(object sender, EventArgs e)
        {
            DateTime currentFrameTime = DateTime.Now;

            double deltaTime = (currentFrameTime - _lastFrameTime).TotalSeconds;
            _heartbeatTimer += deltaTime;

            UpdateMainOpacity(deltaTime);

            if (_heartbeatTimer > _heartbeatMaxTime)
            {
                Debug.WriteLine("heartbeat. collecting processes...");
                _heartbeatTimer = 0;
                CollectProcesses();
            }

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