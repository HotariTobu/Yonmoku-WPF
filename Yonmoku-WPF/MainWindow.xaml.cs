using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Yonmoku-WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            VM = (MWVM)DataContext;

            RotationStoryboard = (Storyboard)FindResource("RotationStoryboard");
        }

        private MWVM VM;

        private Storyboard RotationStoryboard;

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await VM.Initialize();
        }

        #region == MouseEvents ==

        private bool IsDragging;
        private Point LastMousePosition;
        private Point LastAngle;

        private bool IsDown;

        private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                IsDragging = true;
                LastMousePosition = e.GetPosition(Viewport);
                LastAngle = new Point(VM.HorizontalAngle, VM.VerticalAngle);
            }
            else if (e.ChangedButton == MouseButton.Left)
            {
                IsDown = true;
            }
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsDragging)
            {
                Vector vector = e.GetPosition(Viewport) - LastMousePosition;
                vector /= 100;
                VM.HorizontalAngle = LastAngle.X + vector.X;
                VM.VerticalAngle = LastAngle.Y + vector.Y;
            }
            else
            {
                TestHit(sender, e);
            }
        }

        private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                IsDragging = false;
            }
            else if (e.ChangedButton == MouseButton.Left && IsDown)
            {
                IsDown = false;
                VM.AddStone((Keyboard.Modifiers & ModifierKeys.Shift) != 0);
            }
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            VM.CameraScale -= e.Delta / 3000d;
        }

        public void TestHit(object sender, MouseEventArgs e)
        {
            VisualTreeHelper.HitTest((Visual)sender, null, OnHit, new PointHitTestParameters(e.GetPosition((UIElement)sender)));
        }

        public HitTestResultBehavior OnHit(HitTestResult result)
        {
            if (result is RayMeshGeometry3DHitTestResult meshResult)
            {
                VM.SetAirStonePoint(meshResult.PointHit, (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
            }

            return HitTestResultBehavior.Stop;
        }

        #endregion
    }
}
