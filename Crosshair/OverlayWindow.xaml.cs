using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

// Aliases for WPF types for clarity
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;
using MediaBrushes = System.Windows.Media.Brushes;

namespace Crosshair
{
    public enum CrosshairType
    {
        Classic,
        Dot,
        Cross,
        TShape
    }

    public partial class OverlayWindow : Window
    {
        private Line LineTop, LineBottom, LineLeft, LineRight;
        private Ellipse Circle;
        private CrosshairType currentType = CrosshairType.Classic;

        public OverlayWindow()
        {
            InitializeComponent();

            // Configure window for transparency and overlay behavior
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = MediaBrushes.Transparent;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.Width = 200;
            this.Height = 200;

            // Initialize crosshair shapes
            LineTop = new Line();
            LineBottom = new Line();
            LineLeft = new Line();
            LineRight = new Line();
            Circle = new Ellipse();

            var canvas = new Canvas();
            canvas.Children.Add(LineTop);
            canvas.Children.Add(LineBottom);
            canvas.Children.Add(LineLeft);
            canvas.Children.Add(LineRight);
            canvas.Children.Add(Circle);
            this.Content = canvas;

            // Set default crosshair settings
            SetColor(Colors.Red);
            SetThickness(2);
            SetSize(20);
            SetCrosshairType(CrosshairType.Classic);
            SetVisible(true);
            CenterOnScreen();
        }

        public void SetColor(System.Windows.Media.Color color)
        {
            // Set stroke color for all crosshair elements
            SolidColorBrush brush = new SolidColorBrush(color);
            LineTop.Stroke = brush;
            LineBottom.Stroke = brush;
            LineLeft.Stroke = brush;
            LineRight.Stroke = brush;
            Circle.Stroke = brush;
        }

        public void SetThickness(double thickness)
        {
            // Set stroke thickness for all crosshair elements
            LineTop.StrokeThickness = thickness;
            LineBottom.StrokeThickness = thickness;
            LineLeft.StrokeThickness = thickness;
            LineRight.StrokeThickness = thickness;
            Circle.StrokeThickness = thickness / 2; // Optionally use a thinner circle
        }

        public void SetSize(double size)
        {
            double centerX = this.Width / 2;
            double centerY = this.Height / 2;

            // Draw only the visible lines based on the current crosshair type
            if (LineTop.Visibility == Visibility.Visible)
            {
                LineTop.X1 = centerX; LineTop.Y1 = centerY - size;
                LineTop.X2 = centerX; LineTop.Y2 = centerY;
            }

            if (LineBottom.Visibility == Visibility.Visible)
            {
                LineBottom.X1 = centerX; LineBottom.Y1 = centerY;
                LineBottom.X2 = centerX; LineBottom.Y2 = centerY + size;
            }

            if (LineLeft.Visibility == Visibility.Visible)
            {
                LineLeft.X1 = centerX - size; LineLeft.Y1 = centerY;
                LineLeft.X2 = centerX; LineLeft.Y2 = centerY;
            }

            if (LineRight.Visibility == Visibility.Visible)
            {
                LineRight.X1 = centerX; LineRight.Y1 = centerY;
                LineRight.X2 = centerX + size; LineRight.Y2 = centerY;
            }

            if (Circle.Visibility == Visibility.Visible)
            {
                double circleDiameter = size;
                Circle.Width = circleDiameter;
                Circle.Height = circleDiameter;
                Canvas.SetLeft(Circle, centerX - circleDiameter / 2);
                Canvas.SetTop(Circle, centerY - circleDiameter / 2);
            }
        }

        public void SetCrosshairType(CrosshairType type)
        {
            currentType = type;

            // Make all elements visible by default
            LineTop.Visibility = LineBottom.Visibility = LineLeft.Visibility = LineRight.Visibility = Circle.Visibility = Visibility.Visible;

            switch (type)
            {
                case CrosshairType.Classic:
                    // All elements are visible
                    break;
                case CrosshairType.Dot:
                    // Only the dot (circle) is visible
                    LineTop.Visibility = LineBottom.Visibility = LineLeft.Visibility = LineRight.Visibility = Visibility.Hidden;
                    break;
                case CrosshairType.Cross:
                    // Hide the circle, show cross lines
                    Circle.Visibility = Visibility.Hidden;
                    break;
                case CrosshairType.TShape:
                    // Hide the bottom line and the circle
                    LineBottom.Visibility = Visibility.Hidden;
                    Circle.Visibility = Visibility.Hidden;
                    break;
            }

            // Redraw crosshair with the current size
            SetSize(Width / 10); // Default proportional size, can be adjusted
        }

        public void CenterOnScreen()
        {
            // Center the overlay window on the primary screen
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            this.Left = (screenWidth - this.Width) / 2;
            this.Top = (screenHeight - this.Height) / 2;
        }

        public void SetVisible(bool visible)
        {
            // Show or hide the overlay window
            this.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }
    }
}
