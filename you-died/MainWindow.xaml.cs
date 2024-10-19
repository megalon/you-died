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

        public MainWindow()
        {
            InitializeComponent();

            Debug.WriteLine("hello testing");

            _lastFrameTime = DateTime.Now;

            Visibility = Visibility.Visible;

            CompositionTarget.Rendering += Update;
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
            _lerpAmount += deltaTime;
            Main.Opacity = double.Lerp(Main.Opacity, _targetOpacity, _lerpAmount);


            if (Main.Opacity <= 0)
            {
                _lerpAmount = 0;
                _targetOpacity = _maxOpacity;
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

            Visibility = Visibility.Visible;
        }
    }
}