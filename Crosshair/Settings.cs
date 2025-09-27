using System;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;

namespace Crosshair
{
    public class CrosshairSettings
    {
        // Default settings path and profiles directory
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DotSight");
        
        private static readonly string DefaultSettingsPath = Path.Combine(SettingsDirectory, "settings.json");
        private static readonly string ProfilesDirectory = Path.Combine(SettingsDirectory, "Profiles");
        private static readonly string ConfigPath = Path.Combine(SettingsDirectory, "config.json");
        // Add this static property to store all custom crosshairs
        private static readonly string CustomCrosshairsPath = Path.Combine(SettingsDirectory, "CustomCrosshairs");

        // Settings properties
        public string Name { get; set; } = "Default";
        public bool CrosshairEnabled { get; set; } = true;
        public string SelectedGameWindow { get; set; } = "Center on screen";
        public string SelectedColor { get; set; } = "Red";
        public double CrosshairThickness { get; set; } = 2;
        public double CrosshairSize { get; set; } = 20;
        public CrosshairType CrosshairType { get; set; } = CrosshairType.Classic;
        // Add this property to CrosshairSettings class
        public CustomCrosshair CustomCrosshairData { get; set; } = new CustomCrosshair();

        // App configuration class for storing last used profile
        public class AppConfig
        {
            public string LastUsedProfile { get; set; } = "Default";

            public static AppConfig Load()
            {
                try
                {
                    if (File.Exists(ConfigPath))
                    {
                        string json = File.ReadAllText(ConfigPath);
                        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading app config: {ex.Message}");
                }
                return new AppConfig();
            }

            public static void Save(AppConfig config)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                    string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(ConfigPath, json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving app config: {ex.Message}");
                }
            }
        }

        // Save settings to JSON file
        public static void SaveSettings(CrosshairSettings settings)
        {
            try
            {
                // Create directory if it doesn't exist
                Directory.CreateDirectory(SettingsDirectory);
                
                // If saving default settings
                if (settings.Name == "Default")
                {
                    SaveSettingsToFile(settings, DefaultSettingsPath);
                }
                else
                {
                    // Create profiles directory if it doesn't exist
                    Directory.CreateDirectory(ProfilesDirectory);
                    
                    // Save to profiles directory with the profile name
                    string profilePath = Path.Combine(ProfilesDirectory, $"{settings.Name}.json");
                    SaveSettingsToFile(settings, profilePath);
                }

                // Update last used profile in config
                var config = AppConfig.Load();
                config.LastUsedProfile = settings.Name;
                AppConfig.Save(config);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private static void SaveSettingsToFile(CrosshairSettings settings, string filePath)
        {
            // Serialize settings to JSON
            string jsonString = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            // Write to file
            File.WriteAllText(filePath, jsonString);
        }

        // Load settings from JSON file
        public static CrosshairSettings LoadSettings(string profileName = null)
        {
            try
            {
                // If no profile specified, load from config
                if (profileName == null)
                {
                    var config = AppConfig.Load();
                    profileName = config.LastUsedProfile;
                    
                    // If config has an invalid profile, default to "Default"
                    if (string.IsNullOrEmpty(profileName) || (!File.Exists(Path.Combine(ProfilesDirectory, $"{profileName}.json")) 
                        && profileName != "Default"))
                    {
                        profileName = "Default";
                    }
                }

                string filePath;
                
                if (profileName == "Default")
                {
                    filePath = DefaultSettingsPath;
                }
                else
                {
                    filePath = Path.Combine(ProfilesDirectory, $"{profileName}.json");
                }
                
                // Check if file exists
                if (File.Exists(filePath))
                {
                    // Read and deserialize JSON
                    string jsonString = File.ReadAllText(filePath);
                    var settings = JsonSerializer.Deserialize<CrosshairSettings>(jsonString);
                    
                    // Update the config with this as last used profile
                    var config = AppConfig.Load();
                    config.LastUsedProfile = profileName;
                    AppConfig.Save(config);
                    
                    return settings;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings for profile '{profileName}': {ex.Message}");
            }

            // Return default settings if file doesn't exist or there was an error
            return new CrosshairSettings { Name = profileName ?? "Default" };
        }
        
        // Get a list of all available profiles
        public static List<string> GetProfileNames()
        {
            var profiles = new List<string> { "Default" };
            
            try
            {
                // Create directory if it doesn't exist
                Directory.CreateDirectory(ProfilesDirectory);
                
                // Get all JSON files in the profiles directory
                string[] profileFiles = Directory.GetFiles(ProfilesDirectory, "*.json");
                
                // Extract profile names from filenames
                foreach (string file in profileFiles)
                {
                    string profileName = Path.GetFileNameWithoutExtension(file);
                    if (!profiles.Contains(profileName))
                    {
                        profiles.Add(profileName);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting profile names: {ex.Message}");
            }
            
            return profiles;
        }
        
        // Delete a profile
        public static bool DeleteProfile(string profileName)
        {
            if (profileName == "Default")
            {
                return false; // Can't delete Default profile
            }
            
            try
            {
                string profilePath = Path.Combine(ProfilesDirectory, $"{profileName}.json");
                if (File.Exists(profilePath))
                {
                    File.Delete(profilePath);
                    
                    // If this was the last used profile, reset to Default
                    var config = AppConfig.Load();
                    if (config.LastUsedProfile == profileName)
                    {
                        config.LastUsedProfile = "Default";
                        AppConfig.Save(config);
                    }
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting profile: {ex.Message}");
            }
            
            return false;
        }

        // Method to save a custom crosshair
        public static void SaveCustomCrosshair(CustomCrosshair crosshair)
        {
            try
            {
                Directory.CreateDirectory(CustomCrosshairsPath);
                string filePath = Path.Combine(CustomCrosshairsPath, $"{crosshair.Name}.json");
                string jsonString = JsonSerializer.Serialize(crosshair, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(filePath, jsonString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving custom crosshair: {ex.Message}");
            }
        }

        // Method to load a custom crosshair
        public static CustomCrosshair LoadCustomCrosshair(string name)
        {
            try
            {
                string filePath = Path.Combine(CustomCrosshairsPath, $"{name}.json");
                if (File.Exists(filePath))
                {
                    string jsonString = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<CustomCrosshair>(jsonString) ?? new CustomCrosshair();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading custom crosshair: {ex.Message}");
            }
            return new CustomCrosshair();
        }

        // Method to get all custom crosshair names
        public static List<string> GetCustomCrosshairNames()
        {
            var names = new List<string>();
            try
            {
                Directory.CreateDirectory(CustomCrosshairsPath);
                string[] files = Directory.GetFiles(CustomCrosshairsPath, "*.json");
                foreach (string file in files)
                {
                    names.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting custom crosshair names: {ex.Message}");
            }
            return names;
        }

        // Method to delete a custom crosshair
        public static bool DeleteCustomCrosshair(string name)
        {
            try
            {
                string filePath = Path.Combine(CustomCrosshairsPath, $"{name}.json");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting custom crosshair: {ex.Message}");
            }
            return false;
        }
    }
}
