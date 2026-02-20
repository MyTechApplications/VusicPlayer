using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public static class GlobalData
    {
        public static ObservableCollection<VideoProgress> ContinuePlayingItems { get; } = new();
        private static readonly SemaphoreSlim _synlock = new(1, 1);

        public static async Task RefreshFromDiskAsync()
        {
            await _synlock.WaitAsync();
            try
            {
                var settings = await SettingsHelper.LoadSettingsAsync();
                if (settings?.SavedItems == null) return;

                var unfinished = settings.SavedItems.Where(i => i.CurrentDuration < i.TotalDuration && i.TotalDuration > 0)
                .ToList();
                Debug.WriteLine(unfinished.Count.ToString() + " is the number");
                Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
                {
                    // Simple sync: Clear and reload (or better, use a smarter sync)
                    ContinuePlayingItems.Clear();
                    foreach (var item in unfinished)
                    {
                        ContinuePlayingItems.Add(item);
                    }
                });
            }
            finally
            {
                _synlock.Release();
            }

        }
    } }

