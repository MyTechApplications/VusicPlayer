using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace VusicPlayer
{
    public static class DataManager
    {
        private static readonly string _filePath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "settings.json");
        private static readonly SemaphoreSlim _fileLock = new(1, 1);

        public static async Task SaveProgressAsync(string filePath, string fileName, double current, double total)
        {
            await _fileLock.WaitAsync();
            try
            {
                var settings = await LoadInternal();
                var existingItem = settings.SavedItems.FirstOrDefault(i => i.FilePath == filePath);

                if (existingItem != null)
                {
                    existingItem.CurrentDuration = current;
                    existingItem.TotalDuration = total;
                }
                else
                {
                    settings.SavedItems.Insert(0, new VideoProgress { FilePath = filePath, FileName = fileName, CurrentDuration = current, TotalDuration = total });
                }

                await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(settings));
            }
            finally { _fileLock.Release(); }
        }

        public static async Task LoadToMemory()
        {
            var settings = await LoadInternal();
            var unfinished = settings.SavedItems
                .Where(i => i.CurrentDuration < (i.TotalDuration - 5) && i.TotalDuration > 0)
                .ToList();

            // Update the collection sitting in App.xaml.cs
            App.m_window.DispatcherQueue.TryEnqueue(() =>
            {
                App.GlobalContinuePlaying.Clear();
                foreach (var item in unfinished)
                {
                    App.GlobalContinuePlaying.Add(item);
                }
            });
        }

        private static async Task<AppSettings> LoadInternal()
        {
            if (!File.Exists(_filePath)) return new AppSettings();
            try
            {
                using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return await JsonSerializer.DeserializeAsync<AppSettings>(stream) ?? new AppSettings();
            }
            catch { return new AppSettings(); }
        }
    }
}
