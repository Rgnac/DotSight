using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

// Aliases to separate WPF and Forms types
using FormsApp = System.Windows.Forms.Application;
using DrawingColor = System.Drawing.Color;
using MediaBrushes = System.Windows.Media.Brushes;
using MediaColor = System.Windows.Media.Color;
using MediaColors = System.Windows.Media.Colors;

namespace Crosshair
{
    public partial class MainWindow : Window
    {
        private OverlayWindow overlay;
        private DispatcherTimer timer;
        private IntPtr targetHwnd = IntPtr.Zero;
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;

        public MainWindow()
        {
            InitializeComponent();

            overlay = new OverlayWindow();
            overlay.Show();

            overlay.CenterOnScreen();
            // Initialize tray icon and menu
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Exit", null, OnTrayExitClick);

            trayIcon = new NotifyIcon();
            trayIcon.Icon = new Icon("Assets/crosshair.ico");
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.Text = "DotSight";

            // Double-clicking the tray icon restores the main window
            trayIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            };

            // Override default close behavior: hide window instead of exiting
            this.Closing += MainWindow_Closing;

            CrosshairToggle.Checked += (s, e) => overlay.SetVisible(true);
            CrosshairToggle.Unchecked += (s, e) => overlay.SetVisible(false);

            SizeSlider.ValueChanged += SizeSlider_ValueChanged;
            ColorComboBox.SelectionChanged += ColorComboBox_SelectionChanged;
            GameComboBox.SelectionChanged += (s, e) => FindTargetWindow();

            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        private void OnTrayExitClick(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose(); // release tray icon resources
            timer.Stop();       // stop the timer
            overlay.Close();
            System.Windows.Application.Current.Shutdown(); // WPF
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // cancel default close operation
            this.Hide();     // hide the main window
                             // overlay remains visible
        }
        // ---------- WinAPI ----------
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (targetHwnd == IntPtr.Zero) FindTargetWindow();
            if (!GetWindowRect(targetHwnd, out RECT rect)) return;

            int winCenterX = rect.Left + (rect.Right - rect.Left) / 2;
            int winCenterY = rect.Top + (rect.Bottom - rect.Top) / 2;

            overlay.Left = winCenterX - overlay.Width / 2;
            overlay.Top = winCenterY - overlay.Height / 2;
        }

        private void FindTargetWindow()
        {
            if (GameComboBox.SelectedItem is not ComboBoxItem item)
            {
                targetHwnd = IntPtr.Zero;
                return;
            }

            string desiredTitle = item.Content.ToString();
            targetHwnd = IntPtr.Zero;

            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(p.MainWindowTitle)) continue;

                    // Partial title match
                    if (p.MainWindowTitle.IndexOf(desiredTitle, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        targetHwnd = p.MainWindowHandle;
                        break;
                    }
                }
                catch
                {
                    // Ignore processes that throw exceptions
                }
            }
        }

        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            overlay?.SetThickness(ThicknessSlider.Value);
        }

        private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            overlay?.SetSize(SizeSlider.Value);
        }
        private void ColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ColorComboBox.SelectedItem is ComboBoxItem item)
            {
                MediaColor color = item.Content.ToString() switch
                {
                    "Red" => MediaColors.Red,
                    "Green" => MediaColors.Green,
                    "Blue" => MediaColors.Blue,
                    "Yellow" => MediaColors.Yellow,
                    "White" => MediaColors.White,
                    "Cyan" => MediaColors.Cyan,
                    "Magenta" => MediaColors.Magenta,
                    _ => MediaColors.Red
                };

                overlay?.SetColor(color);
            }
        }
        // Different crosshair types
        private void CrosshairsButton_Click(object sender, RoutedEventArgs e)
        {
            CrosshairsPanel.Visibility = CrosshairsPanel.Visibility == Visibility.Visible
                                         ? Visibility.Collapsed
                                         : Visibility.Visible;

            if (CrosshairsPanel.Children.Count == 0)
                LoadCrosshairPreviews();
        }

        private void LoadCrosshairPreviews()
        {
            var types = Enum.GetValues(typeof(CrosshairType)).Cast<CrosshairType>();

            foreach (var type in types)
            {
                var canvas = new Canvas
                {
                    Width = 50,
                    Height = 50,
                    Margin = new Thickness(5),
                    Background = MediaBrushes.Transparent
                };

                // Use MediaColor alias
                MediaColor color = MediaColors.Red;
                SolidColorBrush strokeBrush = new SolidColorBrush(color);

                switch (type)
                {
                    case CrosshairType.Classic:
                        // Vertical lines
                        canvas.Children.Add(new Line { X1 = 25, Y1 = 0, X2 = 25, Y2 = 20, Stroke = strokeBrush, StrokeThickness = 2 });
                        canvas.Children.Add(new Line { X1 = 25, Y1 = 50, X2 = 25, Y2 = 30, Stroke = strokeBrush, StrokeThickness = 2 });
                        // Horizontal lines
                        canvas.Children.Add(new Line { X1 = 0, Y1 = 25, X2 = 20, Y2 = 25, Stroke = strokeBrush, StrokeThickness = 2 });
                        canvas.Children.Add(new Line { X1 = 50, Y1 = 25, X2 = 30, Y2 = 25, Stroke = strokeBrush, StrokeThickness = 2 });
                        // Circle
                        var ellipse = new Ellipse { Width = 16, Height = 16, Stroke = strokeBrush, StrokeThickness = 1 };
                        Canvas.SetLeft(ellipse, 17);
                        Canvas.SetTop(ellipse, 17);
                        canvas.Children.Add(ellipse);
                        break;

                    case CrosshairType.Dot:
                        var dot = new Ellipse { Width = 10, Height = 10, Stroke = strokeBrush, StrokeThickness = 2 };
                        Canvas.SetLeft(dot, 20);
                        Canvas.SetTop(dot, 20);
                        canvas.Children.Add(dot);
                        break;

                    case CrosshairType.Cross:
                        canvas.Children.Add(new Line { X1 = 25, Y1 = 0, X2 = 25, Y2 = 50, Stroke = strokeBrush, StrokeThickness = 2 });
                        canvas.Children.Add(new Line { X1 = 0, Y1 = 25, X2 = 50, Y2 = 25, Stroke = strokeBrush, StrokeThickness = 2 });
                        break;

                    case CrosshairType.TShape:
                        canvas.Children.Add(new Line { X1 = 25, Y1 = 0, X2 = 25, Y2 = 25, Stroke = strokeBrush, StrokeThickness = 2 });
                        canvas.Children.Add(new Line { X1 = 0, Y1 = 25, X2 = 50, Y2 = 25, Stroke = strokeBrush, StrokeThickness = 2 });
                        break;
                }

                canvas.MouseDown += (s, e) =>
                {
                    overlay.SetCrosshairType(type); // apply selected crosshair type
                };

                CrosshairsPanel.Children.Add(canvas);
            }
        }


    }
}
