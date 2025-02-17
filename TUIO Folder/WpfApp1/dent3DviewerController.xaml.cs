using System;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace WpfApp1
{
    public partial class dent3DviewerController : UserControl
    {
        private PerspectiveCamera _camera;
        private Transform3DGroup _transformGroup = new Transform3DGroup();
        private double _currentRotationAngle = 0;
        private double _currentVerticalRotationAngle = 0;

        public dent3DviewerController(string filePath, string imagePath, int flag)
        {
           InitializeComponent();
           CreateSplitView(filePath, imagePath);
        }

        public void UpdateRowSource(string filePath, string imagePath)
        {
            Dispatcher.Invoke(() =>
            {
                CreateSplitView(filePath, imagePath);
            });
        }

        private void CreateSplitView(string modelFilePath, string imagePath)
        {
            Dispatcher.Invoke(() =>
            {
                // Clear existing content
                if (modelVisual != null && modelVisual.Content != null)
                {
                    modelVisual.Content = null; // Clear the 3D content if applicable
                }

                // Resolve relative paths to absolute paths
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string resolvedModelFilePath = Path.Combine(baseDirectory, modelFilePath);
                string resolvedImagePath = Path.Combine(baseDirectory, imagePath);

                // Load the 3D model
                if (File.Exists(resolvedModelFilePath))
                {
                    LoadSTLModel(resolvedModelFilePath);
                }
                else
                {
                    MessageBox.Show($"3D model file not found: {resolvedModelFilePath}", "File Error");
                    return; // Exit if the file isn't found after showing the message
                }

                // Reset or recreate the camera if needed
                _camera = new PerspectiveCamera
                {
                    Position = new Point3D(0, 0, 300),
                    LookDirection = new Vector3D(0, 0, -1),
                    UpDirection = new Vector3D(0, 1, 0),
                    FieldOfView = 60
                };
                viewport.Camera = _camera;

                // Load the image
                if (File.Exists(resolvedImagePath))
                {
                    imageViewer.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(resolvedImagePath, UriKind.Absolute));
                }
                else
                {
                    MessageBox.Show($"Image file not found: {resolvedImagePath}", "File Error");
                }

                // Explicitly refresh the viewport and image viewer if applicable
                viewport.InvalidateVisual();
                imageViewer.InvalidateVisual();
            });
        }




        private void LoadSTLModel(string filePath)
        {
            try
            {
                var stlReader = new StLReader();
                Model3DGroup model = stlReader.Read(filePath);

                Rect3D bounds = model.Bounds;
                double maxDimension = Math.Max(bounds.SizeX, Math.Max(bounds.SizeY, bounds.SizeZ));

                double scaleFactor = 150 / maxDimension;
                Transform3DGroup transformGroup = new Transform3DGroup();
                transformGroup.Children.Add(new ScaleTransform3D(scaleFactor, scaleFactor, scaleFactor));

                transformGroup.Children.Add(new TranslateTransform3D(
                    -bounds.X - bounds.SizeX / 2,
                    -bounds.Y - bounds.SizeY / 2,
                    -bounds.Z - bounds.SizeZ / 2
                ));

                model.Transform = transformGroup;

                var material = new DiffuseMaterial(new SolidColorBrush(Colors.White));
                foreach (var geometry in model.Children)
                {
                    if (geometry is GeometryModel3D geometryModel)
                    {
                        geometryModel.Material = material;
                        geometryModel.BackMaterial = material;
                    }
                }

                modelVisual.Content = model;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load STL model: {ex.Message}");
            }
        }



        public void ChangeBasedOnCommand(string command, int degree = 40)
        {
            switch (command)
            {
                case "Swipe up":
                    RotateUpDown(degree);
                    break;
                case "Swipe down":
                    RotateUpDown(degree);
                    break;
                case "Swipe right":
                    Rotate(degree);
                    break;
                case "Swipe left":
                    Rotate(-degree);
                    break;
                case "Zoom in":
                    ZoomIn();
                    break;
                case "Zoom out":
                    ZoomOut();
                    break;
            }
        }

        private void Rotate(double degrees)
        {
            Dispatcher.Invoke(() =>
            {
                if (modelVisual.Content != null)
                {
                    _currentRotationAngle += degrees;

                    var rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), degrees);
                    var rotateTransform = new RotateTransform3D(rotation);

                    _transformGroup.Children.Add(rotateTransform);

                    modelVisual.Transform = _transformGroup;
                }

                viewport.InvalidateVisual();
            });
        }

        private void RotateUpDown(double degrees) //To move up and down
        {
            Dispatcher.Invoke(() =>
            {
                if (modelVisual.Content != null)
                {
                    _currentVerticalRotationAngle += degrees;

                    var rotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), degrees);
                    var rotateTransform = new RotateTransform3D(rotation);

                    _transformGroup.Children.Add(rotateTransform);

                    modelVisual.Transform = _transformGroup;
                }

                viewport.InvalidateVisual();
            });
        }


        private void ZoomIn()
        {
            Dispatcher.Invoke(() =>
            {
                if (_camera.Position.Z > 100)
                {
                    _camera.Position = new Point3D(_camera.Position.X, _camera.Position.Y, _camera.Position.Z - 20);
                    viewport.InvalidateVisual();
                }
            });
        }

        private void ZoomOut()
        {
            Dispatcher.Invoke(() =>
            {
                if (_camera.Position.Z < 1000)
                {
                    _camera.Position = new Point3D(_camera.Position.X, _camera.Position.Y, _camera.Position.Z + 20);
                    viewport.InvalidateVisual();
                }
            });
        }
    }
}