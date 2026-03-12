using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;          
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public static class SettingsHelper
    {
        // This lock ensures only one part of the app writes/reads the file at a time
        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        private static readonly string _folderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VusicPlayer"); // Use your actual App Name here

        private static readonly string _filePath = Path.Combine(_folderPath, "settings.json");

        public static async Task SaveSettingsAsync(AppSettings settings)
        {
            // Safety: Never save if the object itself is null
            if (settings == null) return;

            await _fileLock.WaitAsync();
            try
            {
                if (!Directory.Exists(_folderPath)) Directory.CreateDirectory(_folderPath);

                string json = JsonSerializer.Serialize(settings);

                // Use a temporary file first. 
                // This prevents the "0-byte file" bug if the app crashes during writing.
                string tempPath = _filePath + ".tmp";
                await File.WriteAllTextAsync(tempPath, json);

                // Move the temp file to the real path (overwriting the old one)
                File.Move(tempPath, _filePath, true);
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public static async Task<AppSettings> LoadSettingsAsync()
        {
            await _fileLock.WaitAsync();
            try
            {
                if (!File.Exists(_filePath))
                {
                    return new AppSettings();
                }

                string json = await File.ReadAllTextAsync(_filePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                // If deserialization fails, return a fresh object but don't overwrite yet
                return settings ?? new AppSettings();
            }
            catch (Exception ex)
            {
                Logger.Log($"Error loading settings: {ex.Message}" , "SettingsHelper", Logger.LogLevelType.Error);
                return new AppSettings();
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }
}
