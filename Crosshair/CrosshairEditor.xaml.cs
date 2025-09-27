using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.IO;
using System.Text.Json;

// Aliases for WPF types for clarity
using MediaBrushes = System.Windows.Media.Brushes;
using MediaPoint = System.Windows.Point;
using MediaRectangle = System.Windows.Shapes.Rectangle;
using MediaPath = System.IO.Path;
using MediaMessageBox = System.Windows.MessageBox;

namespace Crosshair
{
    /// <summary>
    /// Interaction logic for CrosshairEditor.xaml
    /// </summary>
    public partial class CrosshairEditor : Window
    {
        private UIElement selectedElement;
        private MediaPoint startPoint;
        private bool isDragging;
        private readonly string crosshairSaveFolder;

        public CrosshairEditor()
        {
            InitializeComponent();

            // Setup color combo box
            ColorComboBox.Items.Clear();
            foreach (string colorName in new[] { "Red", "Green", "Blue", "Yellow", "White", "Cyan", "Magenta" })
            {
                ColorComboBox.Items.Add(colorName);
            }
            ColorComboBox.SelectedIndex = 0;

            // Get the save folder path
            crosshairSaveFolder = MediaPath.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DotSight", "Crosshairs");

            // Create directory if it doesn't exist
            if (!Directory.Exists(crosshairSaveFolder))
            {
                Directory.CreateDirectory(crosshairSaveFolder);
            }

            // Setup drag events for canvas
            EditorCanvas.MouseLeftButtonDown += EditorCanvas_MouseLeftButtonDown;
            EditorCanvas.MouseLeftButtonUp += EditorCanvas_MouseLeftButtonUp;
            EditorCanvas.MouseMove += EditorCanvas_MouseMove;
        }

        #region Tool Buttons
        private void AddLine_Click(object sender, RoutedEventArgs e)
        {
            var line = new Line
            {
                X1 = 180,
                Y1 = 200,
                X2 = 220,
                Y2 = 200,
                Stroke = MediaBrushes.Red,
                StrokeThickness = 2
            };

            AddElementToCanvas(line);
        }

        private void AddCircle_Click(object sender, RoutedEventArgs e)
        {
            var circle = new Ellipse
            {
                Width = 40,
                Height = 40,
                Stroke = MediaBrushes.Red,
                StrokeThickness = 2
            };

            Canvas.SetLeft(circle, 180);
            Canvas.SetTop(circle, 180);

            AddElementToCanvas(circle);
        }

        private void AddRectangle_Click(object sender, RoutedEventArgs e)
        {
            var rectangle = new MediaRectangle
            {
                Width = 40,
                Height = 40,
                Stroke = MediaBrushes.Red,
                StrokeThickness = 2
            };

            Canvas.SetLeft(rectangle, 180);
            Canvas.SetTop(rectangle, 180);

            AddElementToCanvas(rectangle);
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (selectedElement != null)
            {
                EditorCanvas.Children.Remove(selectedElement);
                selectedElement = null;
                UpdatePropertiesPanel();
            }
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            // Remove all but the grid lines
            List<UIElement> elementsToRemove = new List<UIElement>();
            foreach (UIElement element in EditorCanvas.Children)
            {
                // Keep only the grid lines
                if (element is Line line &&
                    ((line.X1 == 0 && line.X2 == 400) || (line.Y1 == 0 && line.Y2 == 400)))
                {
                    continue;
                }
                elementsToRemove.Add(element);
            }

            foreach (var element in elementsToRemove)
            {
                EditorCanvas.Children.Remove(element);
            }

            selectedElement = null;
            UpdatePropertiesPanel();
        }
        #endregion

        #region Save and Load
        private void SaveCrosshair_Click(object sender, RoutedEventArgs e)
        {
            string name = CrosshairNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MediaMessageBox.Show("Please enter a name for your crosshair.", "Name Required");
                return;
            }

            // Create crosshair model
            CustomCrosshair crosshair = new CustomCrosshair
            {
                Name = name,
                Elements = new List<CrosshairElement>()
            };

            foreach (UIElement element in EditorCanvas.Children)
            {
                // Skip the grid lines
                if (element is Line line &&
                    ((line.X1 == 0 && line.X2 == 400) || (line.Y1 == 0 && line.Y2 == 400)))
                {
                    continue;
                }

                CrosshairElement crosshairElement = ConvertToCrosshairElement(element);
                if (crosshairElement != null)
                {
                    crosshair.Elements.Add(crosshairElement);
                }
            }

            // Save to file
            try
            {
                string filePath = MediaPath.Combine(crosshairSaveFolder, $"{name}.json");
                string json = JsonSerializer.Serialize(crosshair, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);

                MediaMessageBox.Show($"Crosshair '{name}' saved successfully.", "Saved");
            }
            catch (Exception ex)
            {
                MediaMessageBox.Show($"Error saving crosshair: {ex.Message}", "Error");
            }
        }

        private void LoadExisting_Click(object sender, RoutedEventArgs e)
        {
            // Simple file dialog
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".json",
                Filter = "JSON Files (*.json)|*.json",
                InitialDirectory = crosshairSaveFolder
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(dialog.FileName);
                    CustomCrosshair crosshair = JsonSerializer.Deserialize<CustomCrosshair>(json);

                    if (crosshair != null)
                    {
                        // Clear current crosshair
                        ClearAll_Click(null, null);

                        // Load the crosshair name
                        CrosshairNameTextBox.Text = crosshair.Name;

                        // Create UI elements from the model
                        foreach (var element in crosshair.Elements)
                        {
                            UIElement uiElement = ConvertFromCrosshairElement(element);
                            if (uiElement != null)
                            {
                                AddElementToCanvas(uiElement);
                            }
                        }

                        MediaMessageBox.Show($"Crosshair '{crosshair.Name}' loaded successfully.", "Loaded");
                    }
                }
                catch (Exception ex)
                {
                    MediaMessageBox.Show($"Error loading crosshair: {ex.Message}", "Error");
                }
            }
        }
        #endregion

        #region Properties Panel
        private void Position_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (selectedElement == null || !double.TryParse(XPositionTextBox.Text, out double x) ||
                !double.TryParse(YPositionTextBox.Text, out double y))
                return;

            if (selectedElement is Line line)
            {
                // For lines, we move both points maintaining the same vector
                double deltaX = x - line.X1;
                double deltaY = y - line.Y1;

                line.X1 = x;
                line.Y1 = y;
                line.X2 += deltaX;
                line.Y2 += deltaY;
            }
            else
            {
                Canvas.SetLeft(selectedElement, x);
                Canvas.SetTop(selectedElement, y);
            }
        }

        private void EndPosition_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (selectedElement is Line line &&
                double.TryParse(X2PositionTextBox.Text, out double x2) &&
                double.TryParse(Y2PositionTextBox.Text, out double y2))
            {
                line.X2 = x2;
                line.Y2 = y2;
            }
        }

        private void Size_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (selectedElement == null)
                return;

            if (!double.TryParse(WidthTextBox.Text, out double width) ||
                !double.TryParse(HeightTextBox.Text, out double height))
                return;

            if (selectedElement is MediaRectangle rect)
            {
                rect.Width = width;
                rect.Height = height;
            }
            else if (selectedElement is Ellipse ellipse)
            {
                ellipse.Width = width;
                ellipse.Height = height;
            }
        }

        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (selectedElement == null)
                return;

            if (selectedElement is Shape shape)
            {
                shape.StrokeThickness = e.NewValue;
            }
        }

        private void ColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (selectedElement == null || ColorComboBox.SelectedItem == null)
                return;

            if (selectedElement is Shape shape)
            {
                string colorName = ColorComboBox.SelectedItem.ToString();
                shape.Stroke = GetBrushFromColorName(colorName);

                if (shape.Fill != null && shape.Fill != MediaBrushes.Transparent)
                {
                    shape.Fill = GetBrushFromColorName(colorName);
                }
            }
        }

        private void FilledCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (selectedElement == null)
                return;

            if (selectedElement is Shape shape)
            {
                bool isFilled = FilledCheckBox.IsChecked == true;

                if (isFilled)
                {
                    string colorName = ColorComboBox.SelectedItem.ToString();
                    shape.Fill = GetBrushFromColorName(colorName);
                }
                else
                {
                    shape.Fill = MediaBrushes.Transparent;
                }
            }
        }

        private void ApplyCrosshair_Click(object sender, RoutedEventArgs e)
        {
            SaveCrosshair_Click(sender, e);
            DialogResult = true;
        }
        #endregion

        #region Canvas Interaction
        private void EditorCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MediaPoint clickPoint = e.GetPosition(EditorCanvas);
            UIElement hitElement = GetElementAtPoint(clickPoint);

            if (hitElement != null && hitElement != EditorCanvas)
            {
                selectedElement = hitElement;
                startPoint = clickPoint;
                isDragging = true;

                // Capture mouse
                EditorCanvas.CaptureMouse();

                // Update properties panel
                UpdatePropertiesPanel();

                e.Handled = true;
            }
            else
            {
                selectedElement = null;
                UpdatePropertiesPanel();
            }
        }

        private void EditorCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                EditorCanvas.ReleaseMouseCapture();
            }
        }

        private void EditorCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isDragging && selectedElement != null)
            {
                MediaPoint currentPoint = e.GetPosition(EditorCanvas);

                // Calculate the offset from the start position
                Vector offset = currentPoint - startPoint;

                // Apply the offset to the element's position
                if (selectedElement is Line line)
                {
                    // For lines, move both ends
                    line.X1 += offset.X;
                    line.Y1 += offset.Y;
                    line.X2 += offset.X;
                    line.Y2 += offset.Y;
                }
                else
                {
                    // For shapes, adjust the Canvas.Left and Canvas.Top
                    double left = Canvas.GetLeft(selectedElement) + offset.X;
                    double top = Canvas.GetTop(selectedElement) + offset.Y;

                    Canvas.SetLeft(selectedElement, left);
                    Canvas.SetTop(selectedElement, top);
                }

                // Update start point for the next move
                startPoint = currentPoint;

                // Update the properties panel with new positions
                UpdatePropertiesDisplay();
            }
        }

        private UIElement GetElementAtPoint(MediaPoint point)
        {
            // Go through elements in reverse order (topmost first)
            for (int i = EditorCanvas.Children.Count - 1; i >= 0; i--)
            {
                UIElement element = EditorCanvas.Children[i];

                // Skip the grid lines
                if (element is Line line &&
                    ((line.X1 == 0 && line.X2 == 400) || (line.Y1 == 0 && line.Y2 == 400)))
                {
                    continue;
                }

                if (IsPointOverElement(element, point))
                {
                    return element;
                }
            }

            return null;
        }

        private bool IsPointOverElement(UIElement element, MediaPoint point)
        {
            if (element is Line line)
            {
                // For lines, check if point is close enough to the line
                const double tolerance = 5.0;
                return DistanceToLine(point, new MediaPoint(line.X1, line.Y1), new MediaPoint(line.X2, line.Y2)) <= tolerance;
            }
            else if (element is MediaRectangle rect)
            {
                // For rectangles, check if point is within bounds
                double left = Canvas.GetLeft(element);
                double top = Canvas.GetTop(element);

                if (double.IsNaN(left) || double.IsNaN(top))
                    return false;

                double width = rect.Width;
                double height = rect.Height;

                Rect bounds = new Rect(left, top, width, height);
                return bounds.Contains(point);
            }
            else
            {
                // For other shapes, check if point is within bounds
                double left = Canvas.GetLeft(element);
                double top = Canvas.GetTop(element);

                if (double.IsNaN(left) || double.IsNaN(top))
                    return false;

                double width = 0;
                double height = 0;

                // Use a different variable name here to avoid conflict
                if (element is MediaRectangle rectangle)
                {
                    width = rectangle.Width;
                    height = rectangle.Height;
                }
                else if (element is Ellipse ellipse)
                {
                    width = ellipse.Width;
                    height = ellipse.Height;
                }

                Rect bounds = new Rect(left, top, width, height);
                return bounds.Contains(point);
            }
        }

        private double DistanceToLine(MediaPoint point, MediaPoint lineStart, MediaPoint lineEnd)
        {
            double length = Math.Sqrt(Math.Pow(lineEnd.X - lineStart.X, 2) + Math.Pow(lineEnd.Y - lineStart.Y, 2));

            if (length == 0)
                return Math.Sqrt(Math.Pow(point.X - lineStart.X, 2) + Math.Pow(point.Y - lineStart.Y, 2));

            double t = Math.Max(0, Math.Min(1,
                ((point.X - lineStart.X) * (lineEnd.X - lineStart.X) +
                 (point.Y - lineStart.Y) * (lineEnd.Y - lineStart.Y)) /
                Math.Pow(length, 2)));

            double projectionX = lineStart.X + t * (lineEnd.X - lineStart.X);
            double projectionY = lineStart.Y + t * (lineEnd.Y - lineStart.Y);

            return Math.Sqrt(Math.Pow(point.X - projectionX, 2) + Math.Pow(point.Y - projectionY, 2));
        }
        #endregion

        #region Helper Methods
        private void UpdatePropertiesPanel()
        {
            if (selectedElement == null)
            {
                PropertiesPanel.Visibility = Visibility.Collapsed;
                NoSelectionText.Visibility = Visibility.Visible;
                return;
            }

            PropertiesPanel.Visibility = Visibility.Visible;
            NoSelectionText.Visibility = Visibility.Collapsed;

            // Reset panels visibility
            SizePanel.Visibility = Visibility.Collapsed;
            EndPositionPanel.Visibility = Visibility.Collapsed;
            FilledCheckBox.Visibility = Visibility.Visible;

            if (selectedElement is Line)
            {
                EndPositionPanel.Visibility = Visibility.Visible;
                FilledCheckBox.Visibility = Visibility.Collapsed;
            }
            else if (selectedElement is MediaRectangle || selectedElement is Ellipse)
            {
                SizePanel.Visibility = Visibility.Visible;
            }

            UpdatePropertiesDisplay();
        }

        private void UpdatePropertiesDisplay()
        {
            if (selectedElement == null)
                return;

            // Set values based on the selected element
            if (selectedElement is Line line)
            {
                XPositionTextBox.Text = line.X1.ToString("0");
                YPositionTextBox.Text = line.Y1.ToString("0");
                X2PositionTextBox.Text = line.X2.ToString("0");
                Y2PositionTextBox.Text = line.Y2.ToString("0");
                ThicknessSlider.Value = line.StrokeThickness;
                SelectColorInComboBox(line.Stroke as SolidColorBrush);
            }
            else
            {
                double left = Canvas.GetLeft(selectedElement);
                double top = Canvas.GetTop(selectedElement);

                XPositionTextBox.Text = left.ToString("0");
                YPositionTextBox.Text = top.ToString("0");

                if (selectedElement is Shape shape)
                {
                    ThicknessSlider.Value = shape.StrokeThickness;
                    SelectColorInComboBox(shape.Stroke as SolidColorBrush);
                    FilledCheckBox.IsChecked = shape.Fill != null && shape.Fill != MediaBrushes.Transparent;

                    if (selectedElement is MediaRectangle rect)
                    {
                        WidthTextBox.Text = rect.Width.ToString("0");
                        HeightTextBox.Text = rect.Height.ToString("0");
                    }
                    else if (selectedElement is Ellipse ellipse)
                    {
                        WidthTextBox.Text = ellipse.Width.ToString("0");
                        HeightTextBox.Text = ellipse.Height.ToString("0");
                    }
                }
            }
        }

        private void SelectColorInComboBox(SolidColorBrush brush)
        {
            if (brush == null)
                return;

            string colorName = GetColorName(brush);
            foreach (var item in ColorComboBox.Items)
            {
                if (item.ToString() == colorName)
                {
                    ColorComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private string GetColorName(SolidColorBrush brush)
        {
            if (brush.Color == Colors.Red) return "Red";
            if (brush.Color == Colors.Green) return "Green";
            if (brush.Color == Colors.Blue) return "Blue";
            if (brush.Color == Colors.Yellow) return "Yellow";
            if (brush.Color == Colors.White) return "White";
            if (brush.Color == Colors.Cyan) return "Cyan";
            if (brush.Color == Colors.Magenta) return "Magenta";

            // Default to red
            return "Red";
        }

        private System.Windows.Media.Brush GetBrushFromColorName(string colorName)
        {
            return colorName switch
            {
                "Red" => MediaBrushes.Red,
                "Green" => MediaBrushes.Green,
                "Blue" => MediaBrushes.Blue,
                "Yellow" => MediaBrushes.Yellow,
                "White" => MediaBrushes.White,
                "Cyan" => MediaBrushes.Cyan,
                "Magenta" => MediaBrushes.Magenta,
                _ => MediaBrushes.Red
            };
        }

        private void AddElementToCanvas(UIElement element)
        {
            EditorCanvas.Children.Add(element);

            // Make the new element the selected element
            selectedElement = element;
            UpdatePropertiesPanel();

            // Add mouse interaction for the new element
            element.MouseLeftButtonDown += (s, e) =>
            {
                selectedElement = (UIElement)s;
                startPoint = e.GetPosition(EditorCanvas);
                isDragging = true;
                EditorCanvas.CaptureMouse();
                UpdatePropertiesPanel();
                e.Handled = true;
            };
        }

        private CrosshairElement ConvertToCrosshairElement(UIElement element)
        {
            CrosshairElement result = new CrosshairElement
            {
                Thickness = 2
            };

            if (element is Line line)
            {
                result.ElementType = "Line";
                result.X1 = line.X1;
                result.Y1 = line.Y1;
                result.X2 = line.X2;
                result.Y2 = line.Y2;
                result.Thickness = line.StrokeThickness;
                result.Color = GetColorName(line.Stroke as SolidColorBrush);
            }
            else if (element is MediaRectangle rect)
            {
                result.ElementType = "Rectangle";
                result.X1 = Canvas.GetLeft(rect);
                result.Y1 = Canvas.GetTop(rect);
                result.Width = rect.Width;
                result.Height = rect.Height;
                result.Thickness = rect.StrokeThickness;
                result.Color = GetColorName(rect.Stroke as SolidColorBrush);
                result.IsFilled = rect.Fill != null && rect.Fill != MediaBrushes.Transparent;
            }
            else if (element is Ellipse ellipse)
            {
                result.ElementType = "Circle";
                result.X1 = Canvas.GetLeft(ellipse);
                result.Y1 = Canvas.GetTop(ellipse);
                result.Width = ellipse.Width;
                result.Height = ellipse.Height;
                result.Thickness = ellipse.StrokeThickness;
                result.Color = GetColorName(ellipse.Stroke as SolidColorBrush);
                result.IsFilled = ellipse.Fill != null && ellipse.Fill != MediaBrushes.Transparent;
            }
            else
            {
                return null;
            }

            return result;
        }

            private UIElement ConvertFromCrosshairElement(CrosshairElement element)
            {
                System.Windows.Media.Brush brush = GetBrushFromColorName(element.Color);

                if (element.ElementType == "Line")
                {
                    var line = new Line
                    {
                        X1 = element.X1,
                        Y1 = element.Y1,
                        X2 = element.X2,
                        Y2 = element.Y2,
                        Stroke = brush,
                        StrokeThickness = element.Thickness
                    };
                    return line;
                }
                else if (element.ElementType == "Rectangle")
                {
                    var rect = new MediaRectangle
                    {
                        Width = element.Width,
                        Height = element.Height,
                        Stroke = brush,
                        StrokeThickness = element.Thickness,
                        Fill = element.IsFilled ? brush : MediaBrushes.Transparent
                    };

                    Canvas.SetLeft(rect, element.X1);
                    Canvas.SetTop(rect, element.Y1);

                    return rect;
                }
                else if (element.ElementType == "Circle")
                {
                    var ellipse = new Ellipse
                    {
                        Width = element.Width,
                        Height = element.Height,
                        Stroke = brush,
                        StrokeThickness = element.Thickness,
                        Fill = element.IsFilled ? brush : MediaBrushes.Transparent
                    };

                    Canvas.SetLeft(ellipse, element.X1);
                    Canvas.SetTop(ellipse, element.Y1);

                    return ellipse;
                }

                return null;
            }
            #endregion
    }
}