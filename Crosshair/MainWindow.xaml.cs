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

        private string currentProfileName = "Default";

        public MainWindow()
        {
            InitializeComponent();
            this.Topmost = true;
            System.Threading.Tasks.Task.Delay(1).ContinueWith(t =>
            {
                // Switch to UI thread to modify the property
                this.Dispatcher.Invoke(() =>
                {
                    this.Topmost = false;
                });
            });
            // Create overlay but don't show it yet
            overlay = new OverlayWindow();

            try
            {
                // Load last used profile from config (passing null loads the last used profile)
                var settings = CrosshairSettings.LoadSettings();
                
                // Set current profile name to whatever was loaded
                currentProfileName = settings.Name;
                System.Diagnostics.Debug.WriteLine($"Loading last used profile: {currentProfileName}");
                
                // Apply settings before showing the overlay
                ApplySettings(settings);
                
                // Now show the overlay
                overlay.Show();
                overlay.CenterOnScreen();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading initial settings: {ex.Message}");
                
                // If loading settings fails, apply basic defaults and show
                overlay.ApplySettings(MediaColors.Red, 2, 20, CrosshairType.Classic);
                overlay.Show();
                overlay.CenterOnScreen();
            }

            // Initialize tray icon and menu
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Exit", null, OnTrayExitClick);

            trayIcon = new NotifyIcon();
            trayIcon.Icon = new Icon("Assets/crosshair.ico");
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.Text = "DotSight";
            this.Show();

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

            // Load profiles into combobox
            RefreshProfilesList();
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
            // Get selected window option
            string selectedWindow = "Center on screen";
            if (GameComboBox.SelectedItem is ComboBoxItem item)
            {
                selectedWindow = item.Content.ToString();
            }
            
            // Handle "Center on screen" option specially
            if (selectedWindow == "Center on screen")
            {
                // Center on screen and ensure visible
                overlay.CenterOnScreen();
                overlay.SetVisible(CrosshairToggle.IsChecked ?? false);
                return;
            }
            
            // For other options, try to find the window
            FindTargetWindow();
            
            // If window not found or minimized, hide the crosshair
            if (targetHwnd == IntPtr.Zero || IsWindowMinimized(targetHwnd))
            {
                overlay.SetVisible(false);
                return;
            }
            
            // Window found and not minimized, show crosshair if enabled
            overlay.SetVisible(CrosshairToggle.IsChecked ?? false);
            
            // Position the crosshair on the window
            if (!GetWindowRect(targetHwnd, out RECT rect)) return;

            int winCenterX = rect.Left + (rect.Right - rect.Left) / 2;
            int winCenterY = rect.Top + (rect.Bottom - rect.Top) / 2;

            overlay.Left = winCenterX - overlay.Width / 2;
            overlay.Top = winCenterY - overlay.Height / 2;
        }

        // Add this helper method to check if a window is minimized
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsIconic(IntPtr hWnd);

        private bool IsWindowMinimized(IntPtr hWnd)
        {
            return IsIconic(hWnd);
        }

        private void FindTargetWindow()
        {
            if (GameComboBox.SelectedItem is not ComboBoxItem item)
            {
                targetHwnd = IntPtr.Zero;
                return;
            }

            string desiredTitle = item.Content.ToString();
            
            // If "Center on screen" is selected, don't look for a window
            if (desiredTitle == "Center on screen")
            {
                targetHwnd = IntPtr.Zero;
                return;
            }
            
            targetHwnd = IntPtr.Zero;

            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(p.MainWindowTitle)) continue;
                    
                    // Skip minimized windows
                    if (IsWindowMinimized(p.MainWindowHandle)) continue;

                    // Partial title match (case insensitive)
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
            overlay?.SetThickness(ThicknessSlider.Value); // Fixed from ThicnessSlider
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

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = new CrosshairSettings
            {
                Name = currentProfileName,
                CrosshairEnabled = CrosshairToggle.IsChecked ?? true,
                SelectedGameWindow = (GameComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Center on screen",
                SelectedColor = (ColorComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Red",
                CrosshairThickness = ThicknessSlider.Value, // Fixed from ThicnessSlider
                CrosshairSize = SizeSlider.Value,
                CrosshairType = overlay.GetCurrentCrosshairType()
            };

            CrosshairSettings.SaveSettings(settings);
            System.Windows.MessageBox.Show($"Profile '{currentProfileName}' saved successfully.", "DotSight", 
        MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RefreshProfilesList()
        {
            var profiles = CrosshairSettings.GetProfileNames();
            ProfileComboBox.Items.Clear();

            foreach (var profile in profiles)
            {
                ProfileComboBox.Items.Add(profile);
            }

            // Select the current profile
            int index = profiles.IndexOf(currentProfileName);
            if (index >= 0)
            {
                ProfileComboBox.SelectedIndex = index;
            }
            else
            {
                // Default to the first item if current profile not found
                ProfileComboBox.SelectedIndex = 0;
            }
        }

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProfileComboBox.SelectedItem != null)
            {
                try
                {
                    // Get the selected profile name
                    string profileName = ProfileComboBox.SelectedItem.ToString();
                    
                    // Don't reload if it's the same profile
                    if (profileName == currentProfileName)
                        return;
                    
                    System.Diagnostics.Debug.WriteLine($"Profile selection changed from {currentProfileName} to {profileName}");
                    
                    // Set the current profile name
                    currentProfileName = profileName;
                    
                    // Load the selected profile
                    var settings = CrosshairSettings.LoadSettings(profileName);
                    if (settings != null)
                    {
                        // Loading the profile automatically updates the last used setting in our new code
                        System.Diagnostics.Debug.WriteLine($"Successfully loaded profile: {profileName}, Size: {settings.CrosshairSize}");
                        ApplySettings(settings);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in ProfileComboBox_SelectionChanged: {ex.Message}");
                }
            }
        }

        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileComboBox.SelectedItem != null)
            {
                string profileName = ProfileComboBox.SelectedItem.ToString();
                
                if (profileName == "Default")
                {
                    System.Windows.MessageBox.Show("Cannot delete the Default profile.", "DotSight",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var result = System.Windows.MessageBox.Show($"Are you sure you want to delete the '{profileName}' profile?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                if (result == MessageBoxResult.Yes)
                {
                    if (CrosshairSettings.DeleteProfile(profileName))
                    {
                        // If we deleted the current profile, switch to Default
                        if (currentProfileName == profileName)
                        {
                            currentProfileName = "Default";
                        }
                        
                        RefreshProfilesList();
                        
                        // Ensure Default is selected after deletion
                        foreach (var item in ProfileComboBox.Items)
                        {
                            if (item.ToString() == "Default")
                            {
                                ProfileComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void CreateNewButton_Click(object sender, RoutedEventArgs e)
        {
            string newProfileName = NewProfileNameTextBox.Text?.Trim();
    
            if (string.IsNullOrEmpty(newProfileName))
            {
                System.Windows.MessageBox.Show("Please enter a name for the new profile.", "DotSight",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
    
            // Check if profile with this name already exists
            var existingProfiles = CrosshairSettings.GetProfileNames();
            if (existingProfiles.Contains(newProfileName))
            {
                System.Windows.MessageBox.Show($"A profile named '{newProfileName}' already exists.", "DotSight",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
    
            // Save current settings under the new profile name
            var settings = new CrosshairSettings
            {
                Name = newProfileName,
                CrosshairEnabled = CrosshairToggle.IsChecked ?? true,
                SelectedGameWindow = (GameComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Center on screen",
                SelectedColor = (ColorComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Red",
                CrosshairThickness = ThicknessSlider.Value, // Fixed from ThicnessSlider
                CrosshairSize = SizeSlider.Value,
                CrosshairType = overlay.GetCurrentCrosshairType()
            };
    
            CrosshairSettings.SaveSettings(settings);
            currentProfileName = newProfileName;
    
            // Clear the textbox and refresh the profiles list
            NewProfileNameTextBox.Text = "";
            RefreshProfilesList();
    
            // Select the newly created profile
            foreach (var item in ProfileComboBox.Items)
            {
                if (item.ToString() == newProfileName)
                {
                    ProfileComboBox.SelectedItem = item;
                    break;
                }
            }
    
            System.Windows.MessageBox.Show($"Profile '{newProfileName}' created successfully.", "DotSight", 
        MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ReloadProfileButton_Click(object sender, RoutedEventArgs e)
        {
            // Reload the current profile (discarding any unsaved changes)
            var settings = CrosshairSettings.LoadSettings(currentProfileName);
            ApplySettings(settings);
            System.Windows.MessageBox.Show($"Profile '{currentProfileName}' reloaded.", "DotSight", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ApplySettings(CrosshairSettings settings)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Applying settings for profile: {settings.Name}");
                System.Diagnostics.Debug.WriteLine($"CrosshairSize: {settings.CrosshairSize}, Type: {settings.CrosshairType}");
                
                // Apply each setting to the UI and overlay
                CrosshairToggle.IsChecked = settings.CrosshairEnabled;
                
                // Translate color string to actual MediaColor
                MediaColor color = settings.SelectedColor switch
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

                // Set game window
                foreach (ComboBoxItem item in GameComboBox.Items)
                {
                    if (item.Content.ToString() == settings.SelectedGameWindow)
                    {
                        GameComboBox.SelectedItem = item;
                        break;
                    }
                }

                // Set color in UI
                foreach (ComboBoxItem item in ColorComboBox.Items)
                {
                    if (item.Content.ToString() == settings.SelectedColor)
                    {
                        ColorComboBox.SelectedItem = item;
                        break;
                    }
                }

                // Temporarily remove event handlers
                ThicknessSlider.ValueChanged -= ThicknessSlider_ValueChanged;
                SizeSlider.ValueChanged -= SizeSlider_ValueChanged;

                // Update sliders
                ThicknessSlider.Value = settings.CrosshairThickness;
                SizeSlider.Value = settings.CrosshairSize;
                
                System.Diagnostics.Debug.WriteLine($"Set SizeSlider.Value to {settings.CrosshairSize}");

                // Reattach event handlers
                ThicknessSlider.ValueChanged += ThicknessSlider_ValueChanged;
                SizeSlider.ValueChanged += SizeSlider_ValueChanged;

                // CRITICAL: Apply all settings directly to the overlay in a single call
                overlay.ApplySettings(color, settings.CrosshairThickness, settings.CrosshairSize, settings.CrosshairType);
                
                // Set visibility AFTER applying all other settings
                overlay.SetVisible(settings.CrosshairEnabled);
                
                System.Diagnostics.Debug.WriteLine($"Updated overlay with Color: {settings.SelectedColor}, Thickness: {settings.CrosshairThickness}, Size: {settings.CrosshairSize}, Type: {settings.CrosshairType}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ApplySettings: {ex.Message}");
            }
        }
    }
}
