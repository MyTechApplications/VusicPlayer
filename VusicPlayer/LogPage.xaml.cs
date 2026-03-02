using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;
using static VusicPlayer.Logger;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LogPage : Page
{
    public LogPage()
    {
        InitializeComponent(); Logger.Log("Log Entries requested by user", "LogPage", LogLevelType.Information);

    }
    private void LoadPlayerLogs()
    {
        logEntries.Clear();
        Logger.GetLogDetailsList().ForEach(log =>
        {
            logEntries.Insert(0, new LogEntry
            {
                Timestamp = log.Timestamp,
                Source = log.Source,
                Message = log.Message,
                Level = log.Level,
                Icon = log.Level switch
                {
                    Logger.LogLevelType.Information => "ms-appx:///Assets/infoicon.png",
                    Logger.LogLevelType.Warning => "ms-appx:///Assets/warning.png",
                    Logger.LogLevelType.Error => "ms-appx:///Assets/error.png",
                    Logger.LogLevelType.Success => "ms-appx:///Assets/success.png",
                    _ => null
                }
            });

        });
        lstViewPlayerLogs.ItemsSource = logEntries;
        if(logEntries.Count== 0)
        {
            txtNoLogs.Visibility = Visibility.Visible;
            lstViewPlayerLogs.Visibility = Visibility.Collapsed;
            stackHeader.Visibility = Visibility.Collapsed;
        }
        else
        {
            txtNoLogs.Visibility = Visibility.Collapsed;
            stackHeader.Visibility = Visibility.Visible;
            lstViewPlayerLogs.Visibility = Visibility.Visible;
        }
    }
    private void LoadUpdaterLogs()
    {
  /*      Updater.Logger.GetLogDetailsList().ForEach(log =>
        {
            UpdaterlogEntries.Add(new Updater.LogEntry
            {
                Timestamp = log.Timestamp,
                Source = log.Source,
                Message = log.Message,
                Level = log.Level,
                Icon = log.Level switch
                {
                    Updater.Logger.LogLevelType.Information => "ms-appx:///Assets/infoicon.png",
                    Updater.Logger.LogLevelType.Warning => "ms-appx:///Assets/warning.png",
                    Updater.Logger.LogLevelType.Error => "ms-appx:///Assets/error.png",
                    Updater.Logger.LogLevelType.Success => "ms-appx:///Assets/success.png",
                    _ => null
                }
            });
        });
      */
    }
    ObservableCollection<LogEntry> logEntries = new ObservableCollection<LogEntry>();
//    ObservableCollection<Updater.LogEntry> UpdaterlogEntries = new ObservableCollection<Updater.LogEntry>();
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        LoadPlayerLogs();
        LoadUpdaterLogs();
 //       Logger.LogAdded += Logger_LogAdded; ;
       // Updater.Logger.LogAdded += Logger_LogAdded1; ;
    }
    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        base.OnNavigatedFrom(e);

        Logger.LogAdded -= Logger_LogAdded;
 //       Updater.Logger.LogAdded -= Logger_LogAdded1;    

    }

    private async void Logger_LogAdded1()//Updater.LogEntry obj)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            LoadUpdaterLogs();
        });
    }

    private void Logger_LogAdded(LogEntry obj)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            LoadPlayerLogs();
        });
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        //Copy log entry
        if (sender is not Button button)
            return;

        if (button.DataContext is not LogEntry logEntry)
            return;

        var logText =
            $"[{logEntry.Timestamp:O}] " +
            $"[{logEntry.Level}] " +
            $"{logEntry.Source}: {logEntry.Message}";

        var dataPackage = new DataPackage();
        dataPackage.RequestedOperation = DataPackageOperation.Copy;
        dataPackage.SetText(logText);

        Clipboard.SetContent(dataPackage);
        Clipboard.Flush(); // ensures persistence after app closes
    }

    private async void Button_Click_1(object sender, RoutedEventArgs e)
    {
        try
        {
            if (logTabView.SelectedItem is not TabViewItem selectedTab)
                return;

            string? selectedType = selectedTab.Tag?.ToString();

            var picker = new FileSavePicker();
            var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.SuggestedFileName = "VusicPlayer" + "_Log";

            picker.FileTypeChoices.Add("Log File", new List<string> { ".log" });
            picker.FileTypeChoices.Add("Text File", new List<string> { ".txt" });

            StorageFile file = await picker.PickSaveFileAsync();
            if (file == null)
                return;

            var sb = new StringBuilder();
            if (logEntries.Count == 0)
                return;

            foreach (var log in logEntries)
            {
                sb.AppendLine(
                    $"{log.Timestamp:O} | {log.Level} | {log.Source} | {log.Message}");
            }
            if (selectedType == "Player")
            {
               
            }
            else if (selectedType == "Updater")
            {
         //       if (UpdaterlogEntries.Count == 0)
             //       return;

                /*foreach (var log in UpdaterlogEntries)
                {
                    sb.AppendLine(
                        $"{log.Timestamp:O} | {log.Level} | {log.Source} | {log.Message}");
                }*/
            }

            await FileIO.WriteTextAsync(file, sb.ToString());

            Logger.Log($"VusicPlayer log exported successfully.",
                "LogPage",
                LogLevelType.Information);
        }
        catch (Exception ex)
        {
            Logger.Log("Failed to export log: " + ex.Message,
                "LogPage",
                LogLevelType.Error);
        }
    }

    private void Button_Click_2(object sender, RoutedEventArgs e)
    {
       
        LoadPlayerLogs();
    }
}
