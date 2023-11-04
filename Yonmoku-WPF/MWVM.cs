using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;

namespace Yonmoku-WPF
{
    class MWVM : DependencyObject, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        #region == CameraPosition ==

        private Point3D _CameraPosition;
        public Point3D CameraPosition
        {
            get => _CameraPosition;
            set
            {
                if (_CameraPosition != value)
                {
                    _CameraPosition = value;
                    UpdateCameraDirection();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CameraPosition)));
                }
            }
        }

        private void UpdateCameraPosition()
        {
            Point3D point = new();
            double radius = CameraRadius * CameraScale;
            double radiusDash = radius * Math.Cos(VerticalAngle);
            point.X = radiusDash * Math.Cos(HorizontalAngle);
            point.Z = radiusDash * Math.Sin(HorizontalAngle);
            point.Y = radius * Math.Sin(VerticalAngle);
            CameraPosition = point;
        }

        #endregion
        #region == CameraDirection ==

        private Vector3D _CameraDirection;
        public Vector3D CameraDirection
        {
            get => _CameraDirection;
            set
            {
                if (_CameraDirection != value)
                {
                    _CameraDirection = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CameraDirection)));
                }
            }
        }

        private void UpdateCameraDirection()
        {
            CameraDirection = -(Vector3D)CameraPosition;
        }

        #endregion
        #region == CameraScale ==

        private double _CameraScale = 1;
        public double CameraScale
        {
            get => _CameraScale;
            set
            {
                if (value < 0.1)
                {
                    value = 0.1;
                }
                if (_CameraScale != value)
                {
                    _CameraScale = value;
                    UpdateCameraPosition();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CameraScale)));
                }
            }
        }

        #endregion
        #region == CameraRadius ==

        private double _CameraRadius;
        public double CameraRadius
        {
            get => _CameraRadius;
            set
            {
                if (_CameraRadius != value)
                {
                    _CameraRadius = value;
                    UpdateCameraPosition();
                }
            }
        }

        #endregion

        #region == HorizontalAngle ==

        public static readonly DependencyProperty HorizontalAngleProperty = DependencyProperty.Register("HorizontalAngle", typeof(double), typeof(MWVM), new PropertyMetadata(0d,
          (d, e) => ((MWVM)d).OnHorizontalAngleChanged(),
          (d, v) => ((MWVM)d).CoerceHorizontalAngle((double)v)));
        public double HorizontalAngle { get => (double)GetValue(HorizontalAngleProperty); set => SetValue(HorizontalAngleProperty, value); }

        protected virtual void OnHorizontalAngleChanged()
        {
            UpdateCameraPosition();
        }

        private double CoerceHorizontalAngle(double value)
        {
            return value;
        }

        #endregion
        #region == VerticalAngle ==

        public static readonly DependencyProperty VerticalAngleProperty = DependencyProperty.Register("VerticalAngle", typeof(double), typeof(MWVM), new PropertyMetadata(0d,
          (d, e) => ((MWVM)d).OnVerticalAngleChanged(),
          (d, v) => ((MWVM)d).CoerceVerticalAngle((double)v)));
        public double VerticalAngle { get => (double)GetValue(VerticalAngleProperty); set => SetValue(VerticalAngleProperty, value); }

        protected virtual void OnVerticalAngleChanged()
        {
            UpdateCameraPosition();
        }

        private static double HalfPI = Math.PI / 2;
        private static double MHalfPI = -HalfPI;

        private double CoerceVerticalAngle(double value)
        {
            return value < MHalfPI ? MHalfPI : value > HalfPI ? HalfPI : value;
        }

        #endregion

        #region == Game ==

        private IGame _Game;
        public IGame Game
        {
            get => _Game;
            set
            {
                if (_Game != value)
                {
                    _Game?.Dispose();
                    _Game = value;
                    value.Initialize();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Game)));
                }
            }
        }

        #endregion

        #region == Stand ==

        private Model3D _Stand;
        public Model3D Stand
        {
            get => _Stand;
            set
            {
                if (_Stand != value)
                {
                    _Stand = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Stand)));
                }
            }
        }

        #endregion
        #region == Stones ==

        public Geometry3D StoneGeometry { get; private set; }

        public static Material WhiteMaterial = new DiffuseMaterial(Brushes.White);
        public static Material BlackMaterial = new MaterialGroup() { Children = new(new Material[] { new DiffuseMaterial(Brushes.Black), new SpecularMaterial(Brushes.White, 50) }) };

        public Model3DCollection Stones { get; } = new();
        public bool?[,][] StonesMap = new bool?[4, 4][];

        public void AddStone(bool isWhite) => AddStone(Column, Row, isWhite);
        public void AddStone(int column, int row, bool isWhite)
        {
            if (CheckStonePoint(column, row))
            {
                int index = StonesMap[column, row].TakeWhile(x => x != null).Count();
                if (index < 4)
                {
                    GeometryModel3D model = new(StoneGeometry, isWhite ? WhiteMaterial : BlackMaterial);
                    TranslateTransform3D transform = GetStoneTransform(column, row);
                    transform.OffsetY = index * 0.3;
                    model.Transform = transform;


                    FallAnimation.From = 1.2;
                    FallAnimation.To = transform.OffsetY;
                    double x = (FallAnimation.From - FallAnimation.To) ?? 0;
                    double seconds = Math.Sqrt(x) * DurationFactor;
                    FallEase ease = new(seconds);
                    ease.EasingMode = EasingMode.EaseIn;
                    FallAnimation.EasingFunction = ease;
                    FallAnimation.Duration = new Duration(new TimeSpan(0, 0, 0, 0, (int)(seconds * 1000)));

                    FallPackage package = new FallPackage() { Transform = transform };
                    FallPackages.Enqueue(package);
                    Storyboard.SetTarget(FallAnimation, model);
                    FallStoryboard.Begin(package, true);

                    Stones.Add(model);
                    StonesMap[column, row][index] = isWhite;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Stones)));
                }
            }
        }

        #endregion
        #region == AirStone ==

        public static Material AirWhiteMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)));
        public static Material AirBlackMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)));

        private GeometryModel3D _AirStone;
        public GeometryModel3D AirStone
        {
            get => _AirStone;
            set
            {
                if (_AirStone != value)
                {
                    _AirStone = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AirStone)));
                }
            }
        }

        public int Column { get; private set; }
        public int Row { get; private set; }

        private bool CheckStonePoint() => CheckStonePoint(Column, Row);
        private bool CheckStonePoint(int column, int row) => column < 4 && column >= 0 && row < 4 && row >= 0;

        public void SetAirStonePoint(Point3D point, bool isWhite)
        {
            if (point.Y > 0)
            {
                Column = (int)Math.Floor(point.X * 2) + 2;
                Row = (int)Math.Floor(point.Z * 2) + 2;
                if (CheckStonePoint())
                {
                    AirStone.Material = isWhite ? WhiteMaterial : BlackMaterial;
                    AirStone.Transform = GetStoneTransform();
                }
                else
                {
                    AirStone.Material = null;
                }
            }
            else
            {
                AirStone.Material = null;
            }
        }

        private TranslateTransform3D GetStoneTransform(double y = 1.2) => GetStoneTransform(Column, Row, y);
        private TranslateTransform3D GetStoneTransform(int column, int row, double y = 1.2)
        {
            TranslateTransform3D transform = new();
            transform.OffsetX = column * 0.5 - 0.75;
            transform.OffsetZ = row * 0.5 - 0.75;
            transform.OffsetY = y;
            return transform;
        }

        #endregion
        #region == FallAnimation ==

        private Queue<FallPackage> FallPackages = new();

        private void FallAnimation_Completed(object sender, EventArgs e)
        {
            FallStoryboard.Stop(FallPackages.Dequeue());
        }

        private Storyboard FallStoryboard = new();
        private DoubleAnimation FallAnimation = new();
        private static double DurationFactor = 0.45175395145262;

        private class FallPackage : FrameworkElement
        {
            #region == Transform ==

            public static readonly DependencyProperty TransformProperty = DependencyProperty.Register("Transform", typeof(TranslateTransform3D), typeof(FallPackage), new PropertyMetadata());
            public TranslateTransform3D Transform { get => (TranslateTransform3D)GetValue(TransformProperty); set => SetValue(TransformProperty, value); }

            #endregion
        }

        private class FallEase : EasingFunctionBase
        {
            public FallEase(double seconds) : base()
            {
                X = 4.9 * seconds * seconds;
                Seconds = seconds;
            }
            protected override double EaseInCore(double normalizedTime)
            {
                double seconds = normalizedTime * Seconds;
                return (4.9 * seconds * seconds) / X;
            }
            protected override Freezable CreateInstanceCore() => new FallEase(Seconds);

            public readonly double X;
            public readonly double Seconds;
        }

        #endregion

        public async Task Initialize()
        {
            CameraRadius = 10;

            Stand = await ModelLoader.LoadModel("Resources/Stand.obj");

            StoneGeometry = ((GeometryModel3D)((Model3DGroup)((Model3DGroup)await ModelLoader.LoadModel("Resources/Stone.obj")).Children[0]).Children[0]).Geometry;
            bool?[] pillar = new bool?[4];
            Array.Fill(pillar, null);
            for (int column = 0; column < 4; column++)
            {
                for (int row = 0; row < 4; row++)
                {
                    StonesMap[column, row] = (bool?[])pillar.Clone();
                }
            }
            FallStoryboard.Children.Add(FallAnimation);
            FallAnimation.Completed += FallAnimation_Completed;
            Storyboard.SetTargetProperty(FallAnimation, new("Transform.OffsetY"));

            AirStone = new(StoneGeometry, null);
            AirStone.Transform = new TranslateTransform3D();
        }
    }
}
