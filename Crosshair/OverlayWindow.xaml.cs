using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
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
        TShape,
        Custom
    }

    public partial class OverlayWindow : Window
    {
        // UI elements
        private Line LineTop;
        private Line LineBottom;
        private Line LineLeft;
        private Line LineRight;
        private Ellipse Circle;
        private Canvas canvas;
        
        // Current settings
        private CrosshairType currentType = CrosshairType.Classic;
        private double currentSize = 20;
        private MediaColor currentColor = MediaColors.Red;
        private double currentThickness = 2;

        // Window style constants
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public OverlayWindow()
        {
            InitializeComponent();
            InitializeWindow();
            InitializeCrosshair();
        }

        private void InitializeWindow()
        {
            // Configure window for transparency and overlay behavior
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = MediaBrushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            Width = 200;
            Height = 200;
            Cursor = System.Windows.Input.Cursors.None;
        }

        private void InitializeCrosshair()
        {
            // Create crosshair elements
            LineTop = new Line();
            LineBottom = new Line();
            LineLeft = new Line();
            LineRight = new Line();
            Circle = new Ellipse();

            // Add to canvas
            canvas = new Canvas();
            canvas.Children.Add(LineTop);
            canvas.Children.Add(LineBottom);
            canvas.Children.Add(LineLeft);
            canvas.Children.Add(LineRight);
            canvas.Children.Add(Circle);
            Content = canvas;

            // Initial visibility
            SetVisible(true);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Make window click-through and hidden from alt-tab
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
        }

        public void SetColor(MediaColor color)
        {
            currentColor = color;
            
            // Create brush from color
            SolidColorBrush brush = new SolidColorBrush(color);
            
            // Apply to all elements
            LineTop.Stroke = brush;
            LineBottom.Stroke = brush;
            LineLeft.Stroke = brush;
            LineRight.Stroke = brush;
            Circle.Stroke = brush;
        }

        public void SetThickness(double thickness)
        {
            currentThickness = thickness;
            
            // Apply thickness to elements
            LineTop.StrokeThickness = thickness;
            LineBottom.StrokeThickness = thickness;
            LineLeft.StrokeThickness = thickness;
            LineRight.StrokeThickness = thickness;
            Circle.StrokeThickness = thickness / 2; // Thinner circle
        }

        public void SetSize(double size)
        {
            currentSize = size;
            
            double centerX = Width / 2;
            double centerY = Height / 2;

            // Update line positions based on size
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
                Circle.Width = size;
                Circle.Height = size;
                Canvas.SetLeft(Circle, centerX - size / 2);
                Canvas.SetTop(Circle, centerY - size / 2);
            }
        }

        public void SetCrosshairType(CrosshairType type)
        {
            currentType = type;

            // Reset all elements to visible
            LineTop.Visibility = Visibility.Visible;
            LineBottom.Visibility = Visibility.Visible;
            LineLeft.Visibility = Visibility.Visible;
            LineRight.Visibility = Visibility.Visible;
            Circle.Visibility = Visibility.Visible;

            // Apply type-specific visibility
            switch (type)
            {
                case CrosshairType.Classic:
                    // All elements visible (default)
                    break;
                    
                case CrosshairType.Dot:
                    // Only circle visible
                    LineTop.Visibility = Visibility.Hidden;
                    LineBottom.Visibility = Visibility.Hidden;
                    LineLeft.Visibility = Visibility.Hidden;
                    LineRight.Visibility = Visibility.Hidden;
                    break;
                    
                case CrosshairType.Cross:
                    // Lines only
                    Circle.Visibility = Visibility.Hidden;
                    break;
                    
                case CrosshairType.TShape:
                    // T-shape (no bottom line or circle)
                    LineBottom.Visibility = Visibility.Hidden;
                    Circle.Visibility = Visibility.Hidden;
                    break;
            }

            // Apply current size to update positions
            SetSize(currentSize);
        }

        public void CenterOnScreen()
        {
            // Center on primary screen
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
        }

        public void SetVisible(bool visible)
        {
            Visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }

        public CrosshairType GetCurrentCrosshairType()
        {
            return currentType;
        }

        public double GetCurrentSize()
        {
            return currentSize;
        }

        public double GetCurrentThickness()
        {
            return currentThickness;
        }

        public MediaColor GetCurrentColor()
        {
            return currentColor;
        }

        // Apply all settings in one call
        public void ApplySettings(MediaColor color, double thickness, double size, CrosshairType type)
        {
            SetColor(color);
            SetThickness(thickness);
            SetSize(size);
            SetCrosshairType(type);
        }
        // New method to set a custom crosshair
        public void SetCustomCrosshair(CustomCrosshair customCrosshair)
        {
            // Clear existing elements
            canvas.Children.Clear();
            
            // Add each custom element
            double centerX = Width / 2;
            double centerY = Height / 2;
            
            foreach (var element in customCrosshair.Elements)
            {
                // Convert string color to MediaColor
                var colorConverter = new BrushConverter();
                var brush = (SolidColorBrush)colorConverter.ConvertFromString(element.Color) ?? new SolidColorBrush(MediaColors.Red);
                
                switch (element.ElementType)
                {
                    case "Line":
                        var line = new Line
                        {
                            X1 = centerX + (element.X1 * currentSize / 20),
                            Y1 = centerY + (element.Y1 * currentSize / 20),
                            X2 = centerX + (element.X2 * currentSize / 20),
                            Y2 = centerY + (element.Y2 * currentSize / 20),
                            StrokeThickness = element.Thickness * currentThickness,
                            Stroke = brush
                        };
                        canvas.Children.Add(line);
                        break;
                        
                    case "Circle":
                        var ellipse = new Ellipse
                        {
                            Width = element.Width * currentSize / 20,
                            Height = element.Height * currentSize / 20,
                            StrokeThickness = element.Thickness * currentThickness,
                            Stroke = brush
                        };
                        
                        if (element.IsFilled)
                        {
                            ellipse.Fill = brush;
                        }
                        
                        Canvas.SetLeft(ellipse, centerX + (element.X1 * currentSize / 20) - (ellipse.Width / 2));
                        Canvas.SetTop(ellipse, centerY + (element.Y1 * currentSize / 20) - (ellipse.Height / 2));
                        canvas.Children.Add(ellipse);
                        break;
                        
                    case "Rectangle":
                        var rectangle = new System.Windows.Shapes.Rectangle
                        {
                            Width = element.Width * currentSize / 20,
                            Height = element.Height * currentSize / 20,
                            StrokeThickness = element.Thickness * currentThickness,
                            Stroke = brush
                        };
                        
                        if (element.IsFilled)
                        {
                            rectangle.Fill = brush;
                        }
                        
                        Canvas.SetLeft(rectangle, centerX + (element.X1 * currentSize / 20) - (rectangle.Width / 2));
                        Canvas.SetTop(rectangle, centerY + (element.Y1 * currentSize / 20) - (rectangle.Height / 2));
                        canvas.Children.Add(rectangle);
                        break;
                }
            }
            
            currentType = CrosshairType.Custom;
        }

        // Update the SetCrosshairType method to handle the Custom type
        public void SetCrosshairType(CrosshairType type, CustomCrosshair customData = null)
        {
            if (type == CrosshairType.Custom && customData != null)
            {
                SetCustomCrosshair(customData);
                return;
            }
            
            // Reset all elements to visible
            canvas.Children.Clear();
            
            // Re-add standard elements
            LineTop = new Line();
            LineBottom = new Line();
            LineLeft = new Line();
            LineRight = new Line();
            Circle = new Ellipse();
            
            canvas.Children.Add(LineTop);
            canvas.Children.Add(LineBottom);
            canvas.Children.Add(LineLeft);
            canvas.Children.Add(LineRight);
            canvas.Children.Add(Circle);
            
            currentType = type;

            // Reset all elements to visible
            LineTop.Visibility = Visibility.Visible;
            LineBottom.Visibility = Visibility.Visible;
            LineLeft.Visibility = Visibility.Visible;
            LineRight.Visibility = Visibility.Visible;
            Circle.Visibility = Visibility.Visible;

            // Apply type-specific visibility
            switch (type)
            {
                case CrosshairType.Classic:
                    // All elements visible (default)
                    break;
                    
                case CrosshairType.Dot:
                    // Only circle visible
                    LineTop.Visibility = Visibility.Hidden;
                    LineBottom.Visibility = Visibility.Hidden;
                    LineLeft.Visibility = Visibility.Hidden;
                    LineRight.Visibility = Visibility.Hidden;
                    break;
                    
                case CrosshairType.Cross:
                    // Lines only
                    Circle.Visibility = Visibility.Hidden;
                    break;
                    
                case CrosshairType.TShape:
                    // T-shape (no bottom line or circle)
                    LineBottom.Visibility = Visibility.Hidden;
                    Circle.Visibility = Visibility.Hidden;
                    break;
            }

            // Set color and thickness
            SetColor(currentColor);
            SetThickness(currentThickness);
            
            // Apply current size to update positions
            SetSize(currentSize);
        }

        // Update ApplySettings to handle custom crosshairs
        public void ApplySettings(MediaColor color, double thickness, double size, CrosshairType type, CustomCrosshair customData = null)
        {
            SetColor(color);
            SetThickness(thickness);
            SetSize(size);
            SetCrosshairType(type, customData);
        }
    }
}
