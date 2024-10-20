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
        private double _updateProcessesTimer = 0;
        private double _delayBetweenProcessUpdates = 3;

        private Dictionary<int, ProcessInfo> _processDict;

        // This struct helps keep track of all processes, and lets us ignore invalid ones
        private struct ProcessInfo
        {
            public ProcessInfo (Process process, bool ignore)
            {
                this.Process = process;
                this.Ignore = ignore;
            }

            public readonly bool Ignore;
            public readonly Process Process;
        }

        public MainWindow()
        {
            InitializeComponent();
            _processDict = new Dictionary<int, ProcessInfo>();

            _lastFrameTime = DateTime.Now;

            Visibility = Visibility.Hidden;

            CompositionTarget.Rendering += Update;

            UpdateProcessesDict();
        }

        private void UpdateProcessesDict()
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

                    _processDict.Add(process.Id, new ProcessInfo(process, false));
                }
                catch {
                    // We don't want to enter this catch block every heartbeat for the same invalid processes,
                    // so keep track of the process and mark it as "ignore"
                    _processDict.Add(process.Id, new ProcessInfo(process, true));
                }
            }
        }

        private void _processToMonitor_Exited(object? sender, EventArgs e)
        {
            Process process = sender as Process;

            if (process == null) return;

            if (!_processDict.ContainsKey(process.Id))
            {
                Debug.WriteLine($"_processDict didn't contain process {process.Id} - {process.ProcessName}");
                return;
            }

            ProcessInfo processInfo = _processDict[process.Id];

            if (processInfo.Ignore)
            {
                _processDict.Remove(process.Id);
                return;
            }

            // If process exited normally
            if (process.ExitCode == 0)
            {
                _processDict.Remove(process.Id);
                return;
            }

            // Otherwise app has crashed

            // We are in a different thread, so
            // Access the Dispatcher of the main window
            Application.Current.Dispatcher.Invoke(() =>
            {
                Main.Opacity = 0;

                Visibility = Visibility.Visible;

                _lerpAmount = 0;
                _targetOpacity = _maxOpacity;

                _textLerpAmount = 0;
                Message.FontSize = 100;

                string msg = $"{process.ProcessName} EXPLODED";

                Message.Text = msg;
                MessageDummy.Text = msg;

                _processDict.Remove(process.Id);
            });
        }

        private void Update(object sender, EventArgs e)
        {
            DateTime currentFrameTime = DateTime.Now;

            double deltaTime = (currentFrameTime - _lastFrameTime).TotalSeconds;
            _updateProcessesTimer += deltaTime;

            UpdateMainOpacity(deltaTime);
            UpdateTextSize(deltaTime);

            if (_updateProcessesTimer > _delayBetweenProcessUpdates)
            {
                Debug.WriteLine("collecting processes...");
                _updateProcessesTimer = 0;
                UpdateProcessesDict();
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

        private double _textLerpAmount = 0;
        private double _targetFontSize = 150;

        private void UpdateTextSize(double deltaTime)
        {
            _textLerpAmount += deltaTime / 40;
            Message.FontSize = double.Lerp(Message.FontSize, _targetFontSize, _textLerpAmount);

            if (_textLerpAmount > 1)
            {
                _textLerpAmount = 1;
            }
        }
    }
}