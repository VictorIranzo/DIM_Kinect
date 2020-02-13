namespace Exercise1
{
    using Microsoft.Kinect;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor sensor;
        private byte[] colorPixels;

        public WriteableBitmap ColorBitmap { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            foreach (KinectSensor potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;

                    break;
                }
            }

            if (this.sensor != null)
            {
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

                this.ColorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth,
                    this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                this.sensor.ColorFrameReady += this.SensorColorFrameReady;

                this.Image.Source = this.ColorBitmap;

                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }            else
            {
                this.statusBarText.Text = "Sensor not detected";
            }
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    colorFrame.CopyPixelDataTo(this.colorPixels);
                    this.ColorBitmap.WritePixels(new Int32Rect(0, 0,
                    this.ColorBitmap.PixelWidth, this.ColorBitmap.PixelHeight),
                    this.colorPixels, this.ColorBitmap.PixelWidth * sizeof(int), 0);
                }
            }
        }
    }
}