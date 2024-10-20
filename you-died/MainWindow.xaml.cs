using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Media;
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
using System.Windows.Forms; // For NotifyIcon
using System.Drawing; // For Icon
using Application = System.Windows.Application;
using Path = System.IO.Path;
using System.Reflection;

namespace you_died
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DateTime _lastFrameTime;

        private double _lerpAmount;
        private double _targetOpacity;
        private const double _maxOpacity = 0.6;

        private double _textLerpAmount = 0;
        private double _targetFontSize = 150;

        private double _updateProcessesTimer = 0;
        private double _delayBetweenProcessUpdates = 3;

        private ConcurrentDictionary<int, ProcessInfo> _processConcurrentDict;

        private SoundPlayer _soundPlayer;
        
        private NotifyIcon notifyIcon;

        // This struct helps keep track of all processes, and lets us ignore invalid ones
        private struct ProcessInfo
        {
            public ProcessInfo (Process process, bool ignore = false)
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
            _processConcurrentDict = new ConcurrentDictionary<int, ProcessInfo>();

            string userDataPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "YouDied"
            );

            if (!Directory.Exists(userDataPath))
            {
                Directory.CreateDirectory(userDataPath);
            }

            _soundPlayer = new SoundPlayer();
            _soundPlayer.SoundLocation = $"{userDataPath}\\you-died.wav";
            _soundPlayer.LoadAsync();

            _lastFrameTime = DateTime.Now;

            Visibility = Visibility.Hidden;

            CompositionTarget.Rendering += Update;

            UpdateProcessesDict();
            
            // Set up the NotifyIcon
            notifyIcon = new NotifyIcon();
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("you_died.you-died.ico"))
            {
                notifyIcon.Icon = new System.Drawing.Icon(stream);
                notifyIcon.Visible = true;
                notifyIcon.BalloonTipText = "YOU DIED";
            }

            notifyIcon.Text = "You Died";

            // Optionally create a context menu for the tray icon
            notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            notifyIcon.ContextMenuStrip.Items.Add("Test", null, Test_Click);
            notifyIcon.ContextMenuStrip.Items.Add("Exit", null, Exit_Click);
        }

        // Handle the "Open" menu click
        private void Test_Click(object sender, EventArgs e)
        {
            TriggerMessage("TEST");
        }

        // Handle the "Exit" menu click
        private void Exit_Click(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void UpdateProcessesDict()
        {
            Process[] _allProcesses = Process.GetProcesses();

            foreach (Process process in _allProcesses)
            {
                // Ignore if we are already tracking this process
                if (_processConcurrentDict.ContainsKey(process.Id))
                {
                    continue;
                }

                // Skip system processes
                if (process.SessionId == 0)
                {
                    continue;
                }

                // Try and monitor this process. If it fails, we hit the catch block
                try
                {
                    // Ignore processes that somehow don't have a main module.
                    // This will also throw an error if we don't have permission to access this
                    if (process.MainModule == null)
                    {
                        continue;
                    }

                    // Ignore windows processes, IE: C:\Windows\...
                    if (process.MainModule.FileName.ToLowerInvariant().Substring(3).StartsWith("windows\\"))
                    {
                        continue;
                    }


                    process.EnableRaisingEvents = true;
                    process.Exited += _processToMonitor_Exited;

                    Debug.WriteLine($"{process.Id} {process.MainModule?.FileName}");

                    _processConcurrentDict.AddOrUpdate(
                        process.Id, 
                        _ => new ProcessInfo(process), 
                        (_, _) => new ProcessInfo(process)
                    );
                }
                catch {
                    // We don't want to enter this catch block every heartbeat for the same invalid processes,
                    // so keep track of the process and mark it as "ignore"
                    _processConcurrentDict.AddOrUpdate(
                        process.Id,
                        _ => new ProcessInfo(process, true),
                        (_, _) => new ProcessInfo(process, true)
                    );
                }
            }
        }

        private void _processToMonitor_Exited(object? sender, EventArgs e)
        {
            Process process = sender as Process;

            if (process == null) return;

            if (!_processConcurrentDict.ContainsKey(process.Id))
            {
                Debug.WriteLine($"_processDict didn't contain process {process.Id} - {process.ProcessName}");
                return;
            }

            ProcessInfo processInfo = _processConcurrentDict[process.Id];

            if (processInfo.Ignore)
            {
                _processConcurrentDict.Remove(process.Id, out processInfo);
                return;
            }

            // If process exited normally
            if (process.ExitCode == 0)
            {
                _processConcurrentDict.Remove(process.Id, out processInfo);
                return;
            }

            // Otherwise app has crashed

            _soundPlayer.Play();

            // We are in a different thread, so
            // Access the Dispatcher of the main window
            Application.Current.Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"Process {process.ProcessName} exited with code {process.ExitCode}");

                TriggerMessage(process.ProcessName);

                _processConcurrentDict.Remove(process.Id, out processInfo);
            });
        }

        private void TriggerMessage(string processName)
        {
            Main.Opacity = 0;

            Visibility = Visibility.Visible;

            _lerpAmount = 0;
            _targetOpacity = _maxOpacity;

            _textLerpAmount = 0;
            Message.FontSize = 100;

            string msg = $"{processName} EXPLODED";

            Message.Text = msg;
            MessageDummy.Text = msg;
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