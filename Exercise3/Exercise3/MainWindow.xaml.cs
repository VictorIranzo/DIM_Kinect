namespace Exercise3
{
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;

    //*******************************************
    using Kinect.Toolbox;
    using System;
    using System.Windows.Media.Imaging;

    public partial class MainWindow : Window
    {
        private const float RenderWidth = 640.0f;
        private const float RenderHeight = 480.0f;
        private const double JointThickness = 3;
        private const double BodyCenterThickness = 10;
        private const double ClipBoundsThickness = 10;

        private readonly Brush centerPointBrush = Brushes.Blue;
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        private readonly Brush inferredJointBrush = Brushes.Yellow;
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        private KinectSensor sensor;
        private DrawingGroup drawingGroup;
        private DrawingImage imageSource;
        private WriteableBitmap colorBitmap;
        private WriteableBitmap depthBitmap;

        private byte[] colorPixels;
        private DepthImagePixel[] depthPixels;

        private bool enabledColorStream;
        private bool enabledSkeleton = true;

        //***********************TO DO*******************************
        // Definir reconocedores como miembros privados
        private SwipeGestureDetector swipeGestureRecognizer;
        private TemplatedGestureDetector circleGestureRecognizer;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     Execute startup tasks.
        /// </summary>
        /// <param name="sender"> Object sending the event. </param>
        /// <param name="e"> Event arguments. </param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            //********************TO DO*************************************
            // Instanciar reconocedores
            // Si es reconocedor Template_Based abrir fichero con templates.
            // Path.Combine(Environment.CurrentDirectory, @"Datos\circleKB.save")
            swipeGestureRecognizer = new SwipeGestureDetector();
            string circleKBPath = Path.Combine(Environment.CurrentDirectory, @"Datos\circleKB.save");

            using (Stream recordStream = File.Open(circleKBPath, FileMode.Open))
            {
                circleGestureRecognizer = new TemplatedGestureDetector("Circle", recordStream);
            }

            //********************TO DO*************************************
            // Añadir los manejadores como listeners de OnGestureDetected
            circleGestureRecognizer.OnGestureDetected += OnGestureDetectedCircle;
            swipeGestureRecognizer.OnGestureDetected += OnGestureDetectedSwipe;
            // Create the drawing group we'll use for drawing.
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control.
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control.
            Image.Source = this.imageSource;

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug,
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit.
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                this.SetupColorStream();
                this.SetupDepthStream();

                // Turn on the skeleton stream to receive skeleton frames.
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data.
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            this.ChangeBackground();

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }
        }

        /// <summary>
        ///     Execute shutdown tasks.
        /// </summary>
        /// <param name="sender"> Object sending the event. </param>
        /// <param name="e"> Event arguments. </param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        //**************** TO DO ********************************************
        // Definir métodos manejadores que se ejecuten cuando se detecte gesto

        private void OnGestureDetectedSwipe(string gesture)
        {
            if (gesture == "SwipeToRight")
            {
                this.ChangeBackground();
            }
        }

        private void ChangeBackground()
        {
            enabledColorStream = !enabledColorStream;

            if (enabledColorStream)
            {
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.sensor.DepthStream.Disable();
            }
            else
            {
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                this.sensor.ColorStream.Disable();
            }
        }

        private void OnGestureDetectedCircle(string gesture)
        {
            if (gesture == "Circle")
            {
                enabledSkeleton = !enabledSkeleton;
            }
        }

        /// <summary>
        ///     Draws indicators to show which edges are clipping skeleton data.
        /// </summary>
        /// <param name="skeleton"> Skeleton to draw clipping information for. </param>
        /// <param name="drawingContext"> Drawing context to draw to. </param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        ///     Event handler for Kinect sensor's SkeletonFrameReady event.
        /// </summary>
        /// <param name="sender"> Object sending the event. </param>
        /// <param name="e"> Event arguments. </param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            this.RefreshView(skeletons);
        }

        private void RefreshView(Skeleton[] skeletons)
        {
            using (DrawingContext dc = this.drawingGroup.Open())
            {
                if (enabledColorStream)
                {
                    dc.DrawImage(this.colorBitmap, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                }
                else
                {
                    dc.DrawImage(this.depthBitmap, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                }

                // Draw a transparent background to set the render size.
                ////dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly && enabledSkeleton)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // Prevent drawing outside of our render area.
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        /// <summary>
        ///     Draws a skeleton's bones and joints.
        /// </summary>
        /// <param name="skeleton"> Skeleton to draw. </param>
        /// <param name="drawingContext"> Drawing context to draw to. </param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (this.enabledSkeleton)
            {
                DrawBones(skeleton, drawingContext);
            }

            // Render Joints.
            foreach (Joint joint in skeleton.Joints)
            {
                if (enabledSkeleton)
                {
                    this.DrawJoint(drawingContext, joint);
                }

                //***************************TO DO *********************************
                // Si el joint está en estado JointTrackingState.Tracked y su tipo 
                // es el que queremos seguir joint.JointType == JointType.HandRight
                // añadir la posición del Joint al reconocedor.

                if (joint.TrackingState == JointTrackingState.Tracked && joint.JointType == JointType.HandLeft)
                {
                    swipeGestureRecognizer.Add(joint.Position, sensor);
                }

                if (joint.TrackingState == JointTrackingState.Tracked && joint.JointType == JointType.HandRight)
                {
                    circleGestureRecognizer.Add(joint.Position, sensor);
                }
            }
        }

        private void DrawJoint(DrawingContext drawingContext, Joint joint)
        {
            Brush drawBrush = null;

            if (joint.TrackingState == JointTrackingState.Tracked)
            {
                drawBrush = this.trackedJointBrush;
            }
            else if (joint.TrackingState == JointTrackingState.Inferred)
            {
                drawBrush = this.inferredJointBrush;
            }

            if (drawBrush != null)
            {
                drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
            }
        }

        private void DrawBones(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso.
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm.
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm.
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg.
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg.
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);
        }

        /// <summary>
        ///     Maps a SkeletonPoint to lie within our render space and converts to Point.
        /// </summary>
        /// <param name="skelpoint"> Point to map. </param>
        /// <returns> Mapped point. </returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        private void SetupColorStream()
        {
            this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

            this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth,
                this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

            this.sensor.ColorFrameReady += this.SensorColorFrameReady;
        }

        private void SetupDepthStream()
        {
            this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];

            this.depthBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth,
                this.sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

            this.sensor.DepthFrameReady += this.SensorDepthFrameReady;
        }

        /// <summary>
        ///     Draws a bone line between two joints.
        /// </summary>
        /// <param name="skeleton"> Skeleton to draw bones from. </param>
        /// <param name="drawingContext"> Drawing context to draw to. </param>
        /// <param name="jointType0"> Joint to start drawing from. </param>
        /// <param name="jointType1"> Joint to end drawing at. </param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit.
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred.
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked.
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    colorFrame.CopyPixelDataTo(this.colorPixels);
                    this.colorBitmap.WritePixels(new Int32Rect(0, 0,
                    this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                    this.colorPixels, this.colorBitmap.PixelWidth * sizeof(int), 0);
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
                    this.depthBitmap.WritePixels(
                    new Int32Rect(0, 0, this.depthBitmap.PixelWidth,
                    this.depthBitmap.PixelHeight),
                    this.colorPixels,
                    this.depthBitmap.PixelWidth * sizeof(int),
                    0);
                }
            }
        }

        /// <summary>
        ///     Handles the checking or unchecking of the seated mode combo box.
        /// </summary>
        /// <param name="sender"> Object sending the event. </param>
        /// <param name="e"> Event arguments. </param>
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
            {
                if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
                else
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }
    }
}