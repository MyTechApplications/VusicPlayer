using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            loadstuff();
        }
        private async void loadstuff()
        {
            await LoadUserSettingsAsync();
        }
        private async Task LoadUserSettingsAsync()
        {
            var currentSettings = await SettingsHelper.LoadSettingsAsync();

            if (currentSettings.UserSettings.Count == 0)
            {
                ThemeRadioButtons.SelectedIndex = 0;
                return;
            }

            var personalization = currentSettings.UserSettings[0];

            switch (personalization.Theme)
            {
                case "Light":
                    ThemeRadioButtons.SelectedIndex = 1;
                    break;

                case "Dark":
                    ThemeRadioButtons.SelectedIndex = 2;
                    break;

                default:
                    ThemeRadioButtons.SelectedIndex = 0;
                    break;
            }

            ApplyTheme(personalization.Theme);
        }
        public static void ApplyTheme(string theme)
        {
            var rootElement = (FrameworkElement)App.MainWindowInstance.Content;
            {
                switch (theme)
                {
                    case "Light":
                        rootElement.RequestedTheme = ElementTheme.Light;
                        break;

                    case "Dark":
                        rootElement.RequestedTheme = ElementTheme.Dark;
                        break;

                    default:
                        rootElement.RequestedTheme = ElementTheme.Default;
                        break;
                }
            }
        }
        private async void ThemeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeRadioButtons.SelectedItem is string selected)
            {
                var rootElement = (FrameworkElement)App.MainWindowInstance.Content;
                string themee = "default";
                if (selected == "Light")
                {
                    rootElement.RequestedTheme = ElementTheme.Light;
                    themee = "Light";
                }

                else if (selected == "Dark")
                {
                    rootElement.RequestedTheme = ElementTheme.Dark;
                    themee = "Dark";
                }

                else
                {
                    rootElement.RequestedTheme = ElementTheme.Default;
                    themee = "System Default";
                }
            
                var currentSettings = await SettingsHelper.LoadSettingsAsync();

                AppPersonalization personalization;

                if (currentSettings.UserSettings.Count == 0)
                {
                    personalization = new AppPersonalization();
                    currentSettings.UserSettings.Add(personalization);
                }
                else
                {
                    personalization = currentSettings.UserSettings[0];
                }

                // Update theme
                personalization.Theme = themee;

                await SettingsHelper.SaveSettingsAsync(currentSettings);
            }
          
        }
        ObservableCollection<AppPersonalization> theme = new();
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            currentSettings.SavedItems.Clear();
            ttClearedStuff.Title = "Cleared Continue Watching";
            ttClearedStuff.IsOpen = true;
            await Task.Delay(4000);
            ttClearedStuff.IsOpen = false;
            await SettingsHelper.SaveSettingsAsync(currentSettings);
        }

        private void ToggleSubtitles_Toggled(object sender, RoutedEventArgs e)
        {

        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
           
        }
        private void OpenDocuments_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }

        private void OpenMusic_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
        }

        private void OpenDownloads_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"));
        }
        private void OpenFolder(string path)
        {
            if (Directory.Exists(path))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
        }
        private void RemoveFolderFromUI(StackPanel panel)
        {
            if (panel != null)
            {
                FoldersPanel.Children.Remove(panel);
            }
        }
        private void AddFolderToUI(string folderPath)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };

            var text = new TextBlock
            {
                Text = folderPath,
                VerticalAlignment = VerticalAlignment.Center
            };

            var openBtn = new Button
            {
                Content = "Open"
            };

            openBtn.Click += (s, e) => OpenFolder(folderPath);

            var removeBtn = new Button
            {
                Content = "Remove"
            };

            removeBtn.Click += (s, e) => FoldersPanel.Children.Remove(panel);

            panel.Children.Add(text);
            panel.Children.Add(openBtn);
            panel.Children.Add(removeBtn);

            FoldersPanel.Children.Add(panel);
        }
        private async void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(App.MainWindowInstance);
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                AddFolderToUI(folder.Path);
            }
        }
        private void OpenVideos_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
        }
        private void RemoveDocuments_Click(object sender, RoutedEventArgs e)
        {
            RemoveFolderFromUI((sender as Button)?.Parent as StackPanel);
        }

        private void RemoveMusic_Click(object sender, RoutedEventArgs e)
        {
            RemoveFolderFromUI((sender as Button)?.Parent as StackPanel);
        }

        private void RemoveDownloads_Click(object sender, RoutedEventArgs e)
        {
            RemoveFolderFromUI((sender as Button)?.Parent as StackPanel);
        }

        private void RemoveVideos_Click(object sender, RoutedEventArgs e)
        {
            RemoveFolderFromUI((sender as Button)?.Parent as StackPanel);
        }

        private void RemovePictures_Click(object sender, RoutedEventArgs e)
        {
            RemoveFolderFromUI((sender as Button)?.Parent as StackPanel);
        }
        private void OpenPictures_Click(object sender, RoutedEventArgs e)
        {
            OpenFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
        }
        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            currentSettings.RecentMusic.Clear();
            ttClearedStuff.Title = "Cleared Recent Music";
            ttClearedStuff.IsOpen = true;
            await Task.Delay(4000);
            ttClearedStuff.IsOpen = false;
            await SettingsHelper.SaveSettingsAsync(currentSettings);
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            currentSettings.SavedPlaylists.Clear();
            ttClearedStuff.Title = "Deleted Saved Playlists";
            ttClearedStuff.IsOpen = true;
            await Task.Delay(4000);
            ttClearedStuff.IsOpen = false;
            await SettingsHelper.SaveSettingsAsync(currentSettings);
        }
    }
}
