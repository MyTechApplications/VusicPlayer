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
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Controls;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;
using Button = Microsoft.UI.Xaml.Controls.Button;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;
using SelectionChangedEventArgs = Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs;
using StackPanel = Microsoft.UI.Xaml.Controls.StackPanel;
using TextBlock = Microsoft.UI.Xaml.Controls.TextBlock;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Microsoft.UI.Xaml.Controls.Page
    {
        public SettingsPage()
        {
            InitializeComponent();
            txtVersion.Text = $"Version {Appversionstrings.AppVersion}";
            //txtUpdatetype.Text = "(Patch Fixes)";
            loadstuff();
            txtBuild.Text = $"Build {Appversionstrings.BuildNumber}";
            txtVersion.Text = $"Version {Appversionstrings.AppVersion} {Appversionstrings.VersionType}";
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if(e.Parameter is string str)
            {
                if(str == "WinRelate")
                {
                    ScrollToElement(scrViewerMaster, defpanel);
                }
            }
            base.OnNavigatedTo(e);
        }
        public void ScrollToElement(Microsoft.UI.Xaml.Controls.ScrollViewer scrollViewer, UIElement targetElement)
        {
            // Calculate the position of the target element relative to the ScrollViewer
            var transform = targetElement.TransformToVisual(scrollViewer);
            var position = transform.TransformPoint(new Point(0, 0));

            // Calculate the target offset
            // VerticalOffset + the relative position gives us the new position
            double targetVerticalOffset = scrollViewer.VerticalOffset + position.Y;

            // ChangeView(horizontalOffset, verticalOffset, zoomFactor, disableAnimation)
            // Set 'disableAnimation' to false if you want it to slide smoothly
            scrollViewer.ChangeView(null, targetVerticalOffset, null, false);
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
            if (App.MainWindowInstance?.Content is FrameworkElement rootElement)
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
                if (App.MainWindowInstance?.Content is FrameworkElement rootElement)
                {
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
        private void HandleRemoveClick(object sender)
        {
            if (sender is Button button &&
                button.Parent is StackPanel panel)
            {
                RemoveFolderFromUI(panel);
            }
        }
        private void RemoveDocuments_Click(object sender, RoutedEventArgs e)
        {
            HandleRemoveClick(sender);
          
        }

        private void RemoveMusic_Click(object sender, RoutedEventArgs e)
        {
            HandleRemoveClick(sender);
        }

        private void RemoveDownloads_Click(object sender, RoutedEventArgs e)
        {
            HandleRemoveClick(sender);
        }

        private void RemoveVideos_Click(object sender, RoutedEventArgs e)
        {
            HandleRemoveClick(sender);
        }

        private void RemovePictures_Click(object sender, RoutedEventArgs e)
        {
            HandleRemoveClick(sender);
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
            if (sender is FrameworkElement element)
            {
                // 3. Get the flyout associated with this button's parent context
                FlyoutBase.GetAttachedFlyout(element)?.Hide();
            }
        }
        string Logsource = "Settings Page";
        private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            await dlgUpdateChecker.ShowAsync();
            await Task.Delay(2000);
            Version currentVersion = new Version(VersionStringApp.VersionText );
            try
            {
                using var client = new HttpClient();
                hypNew.Visibility = Visibility.Collapsed;
                // 2. Replace with your actual Pastebin RAW URL
                string pastebinContent = await client.GetStringAsync("https://pastebin.com/raw/YjGbNMpc");
                string pastebinContentNew = await client.GetStringAsync("https://pastebin.com/raw/ebPBtgmr");

                var parts = pastebinContent.Split('|');
                if (parts.Length < 2) return;

                Version latestVersion = Version.Parse(parts[0]);
                Logger.Log("Latest Version Check " + latestVersion.ToString() + ": (user initiated)", Logsource, Logger.LogLevelType.Information);
                // 3. Compare versions
                if (latestVersion > currentVersion)
                {
                    imgUpdater.Source = new BitmapImage(new Uri("ms-appx:///Assets/required.png"));
                    txtUpdater.Text = "A new version of the app is available! Version: " + latestVersion.ToString() + Environment.NewLine + "You can manually update the app in Microsoft Store if it doesn't update automatically";
                    hypNew.Visibility = Visibility.Visible;
                    if (pastebinContentNew != string.Empty)
                        hypNew.NavigateUri = new Uri(pastebinContentNew);

                //    string root = AppContext.BaseDirectory;

              //      string stagingFolder = Path.Combine(root, "UpdateStaging");
            //        string? stagingZip = Directory.GetFiles(stagingFolder, "*.zip").FirstOrDefault();
        //            if(stagingZip!= null)
            //        {
                   //     txtUpdateDownloadReady.Visibility = Visibility.Visible;
               //     }
                //    else
                //    {
                      //  txtUpdateDownloadReady.Visibility = Visibility.Collapsed;
                 //   }

                }
                else if (latestVersion == currentVersion)
                {
                    imgUpdater.Source = new BitmapImage(new Uri("ms-appx:///Assets/success.png"));
                    txtUpdater.Text = "Your app is up to date! Version: " + latestVersion.ToString();
                }
                else
                {
                    imgUpdater.Source = new BitmapImage(new Uri("ms-appx:///Assets/error.png"));
                    txtUpdater.Text = "Error checking for updates. Please try again later";
                }
                
                prgCheckforUpdates.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
               
                imgUpdater.Source = new BitmapImage(new Uri("ms-appx:///Assets/error.png"));
                txtUpdater.Text = "Error checking for updates. Please try again later";
                Logger.Log("Error checking for updates:  (user initiated)"  + ex.Message, "Settings Page", Logger.LogLevelType.Error);
            }
        }
      
        private async void Button_Click_4(object sender, RoutedEventArgs e)
        {
         this.Frame.Navigate(typeof(LogPage));
        }

        private async void dlgLogFileClear_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Logger.ClearLog();
            ttClearedStuff.Title = "Cleared App Log";
            ttClearedStuff.IsOpen = true;
            await Task.Delay(4000);
            ttClearedStuff.IsOpen = false;
        }
        public async void ShowWhatNew(XamlRoot rot)
        {
            if (App.HomeWindowInstance == null) return;
            OceanContentDialog.Show($"What's New in Version {VersionStringApp.VersionText}","", "", "OK", OceanContentDialogDefault.Close, grdNewUpdates, rot, 600, 600, OceanContentDialogType.Elevated, App.HomeWindowInstance);
        }
        private void HyperlinkButton_Click_1(object sender, RoutedEventArgs e)
        {
            ShowWhatNew(this.XamlRoot);
        }

        private async void Button_Click_5(object sender, RoutedEventArgs e)
        {
            string pfn = Package.Current.Id.FamilyName;

            await Launcher.LaunchUriAsync(
                new Uri($"ms-settings:defaultapps?registeredAppUser={pfn}"));

        }
    }
}
