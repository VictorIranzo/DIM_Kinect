﻿namespace Exercise2
{
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;

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

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     Draws indicators to show which edges are clipping skeleton data
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
        ///     Execute startup tasks.
        /// </summary>
        /// <param name="sender"> Object sending the event. </param>
        /// <param name="e"> Event arguments. </param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Create the drawing group we'll use for drawing.
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control.
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control.
            Image.Source = this.imageSource;

            //***************************TO DO**********************************
            // DETECCIÓN SENSORES Y ARRANQUE. REGISTRO MANEJADOR DE EVENTOS 
            // ACTIVACIÓN STREAM ESQUELETOS
        }

        /// <summary>
        ///     Execute shutdown tasks.
        /// </summary>
        /// <param name="sender"> Object sending the event. </param>
        /// <param name="e"> Event arguments. </param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //***************************TO DO**********************************
            //PARADA DE SENSOR
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
                //****************************TO DO**********************************
                //COPIADO DE LOS ESQUELETOS DEL FRAME A ALMACENAMIENTO SKELETONS
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size.
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
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

            //*************************************TO DO************************************
            // DIBUJAR LA PIERNA DERECHA

            // ************************************TO DO************************************
            // DIBUJAR TODOS LOS JOINTS COMO ELIPSES (BUCLE)
            // - Si el estado del Joint es "Tracked" usar this.trackedJointBrush;
            // - Si el estado del Joint es "Inferred" usar this.inferredJointBrush;

            Brush drawBrush = null;

            // UTILIZAR ESTA PRIMITIVA PARA EL DIBUJADO DE LA ELIPSE
            drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
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

        /// <summary>
        ///     Draws a bone line between two joints.
        /// </summary>
        /// <param name="skeleton"> Skeleton to draw bones from. </param>
        /// <param name="drawingContext"> Drawing context to draw to. </param>
        /// <param name="jointType0"> Joint to start drawing from. </param>
        /// <param name="jointType1"> Joint to end drawing at. </param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Pen drawPen;

            //********************************TO DO***************************************
            //OBTENER LOS JOINTS DE LOS TIPOS PASADOS COMO PARÁMETROS
            // joint0 y  joint1

            //EN FUNCION DE SU ESTADO:
            // -SI AMBOS "Tracked" dibujar con this.trackedBonePen
            // -SI uno de los dos "NotTracked" no dibujar nada: return;
            // -SI AMBOS "Inferred" no dibujar nada: return;
            // -SI uno "Inferred" y otro "Tracked" dibujar con this.inferredBonePen

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        ///     Handles the checking or unchecking of the seated mode combo box.
        /// </summary>
        /// <param name="sender"> Object sending the event. </param>
        /// <param name="e"> Event arguments. </param>
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (this.sensor != null)
            {
                if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                {
                    //*****************************TO DO**********************************
                    //ACTIVAR MODO SENTADO
                }
                else
                {
                    //*****************************TO DO**********************************
                    //ACTIVAR MODO DEFAULT
                }
            }
        }
    }
}