namespace Exercise1
{
    using Microsoft.Kinect;
    using System;
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
        private DepthImagePixel[] depthPixels;
        public WriteableBitmap ColorBitmap { get; set; }

        public WriteableBitmap DepthBitmap { get; set; }

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
                this.EnableColorStream();
                this.EnableDepthStream();

                this.SetSelectedStream();

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

        private void EnableColorStream()
        {
            this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

            this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

            this.ColorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth,
                this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

            this.sensor.ColorFrameReady += this.SensorColorFrameReady;
        }

        private void EnableDepthStream()
        {
            this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);

            this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];

            this.DepthBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth,
                this.sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

            this.sensor.DepthFrameReady += this.SensorDepthFrameReady;
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

        private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);

                    int minDepth = depthFrame.MinDepth;
                    int maxDepth = depthFrame.MaxDepth;

                    // Convert depth to RGB.
                    int colorPixelIndex = 0;
                    for (int i = 0; i < this.depthPixels.Length; ++i)
                    {
                        short depth = depthPixels[i].Depth;
                        byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ?
                        depth : 0);
                        this.colorPixels[colorPixelIndex++] = intensity;
                        this.colorPixels[colorPixelIndex++] = intensity;
                        this.colorPixels[colorPixelIndex++] = intensity;
                        ++colorPixelIndex; // No alpha channel RGB.
                    }

                    // Copy pixels in RGB in the bitmap.
                    this.DepthBitmap.WritePixels(
                    new Int32Rect(0, 0, this.DepthBitmap.PixelWidth,
                    this.DepthBitmap.PixelHeight),
                    this.colorPixels,
                    this.DepthBitmap.PixelWidth * sizeof(int),
                    0);
                }
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SetSelectedStream();
        }

        private void SetSelectedStream()
        {
            if (this.colorStreamRadioButton.IsChecked == true)
            {
                this.Image.Source = this.ColorBitmap;
            }
            else
            {
                this.Image.Source = this.DepthBitmap;
            }
        }
    }
}