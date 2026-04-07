using CommunityToolkit.WinUI;
using FlyleafLib.MediaPlayer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.System;
using Package = Windows.ApplicationModel.Package;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    public sealed partial class MasterPage : Page
    {
        public MediaPlaybackController mediacontroller => MediaPlaybackController.instance;
        bool _isDragging = false;

        Player? player;
        string currentVideoPath = "";
        int currentVideoIndex = 0;

        public MasterPage()
        {
            InitializeComponent();
            MainPageInstance = this;
            sldMain.IsEnabled = true;
            txtPreviewBuild.Text = $"Vusic Player {Strings.VersionText} {Appversionstrings.AppVersion + Environment.NewLine} {Appversionstrings.VersionType} {Strings.BuildText} {Appversionstrings.BuildNumber}";
            loadingRing.IsActive = true;

            loadingRing.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            frmMain.Navigated += FrmMain_Navigated;

            if (nvgMain.MenuItems.Count > 0)
            {
                nvgMain.SelectedItem = nvgMain.MenuItems[0];
            }
            QueueService.frmMain = frmMain;

            CheckForDefaultNess();
        }
        public async Task<bool> IsAppDefault()
        {

            var result = await Launcher.FindFileHandlersAsync(".mp3");

            foreach (var handler in result)
            {

                if (handler.PackageFamilyName == Windows.ApplicationModel.Package.Current.Id.FamilyName)
                {

                    return true;
                }
            }
            return false;
        }
        public static bool IsDefaultForMp4()
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.mp4\UserChoice");

            if (key == null)
                return false;

            var progId = key.GetValue("ProgId")?.ToString();

            if (progId == null)
                return false;

            string pfn = Package.Current.Id.FamilyName;

            return progId.Contains(pfn);
        }
        private async void CheckForDefaultNess()
        {
            bool isRegistered = await IsAppDefault();
            if (!isRegistered)
            {
                ttDefaultAppSet.IsOpen = true;
            }
            if (!IsDefaultForMp4())
            {
                ttDefaultAppSet.IsOpen = true;
            }
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            if (currentSettings.ShowDefaultMessage == false)
            {
                ttDefaultAppSet.IsOpen = false;
            }
        }
        private async void FrmMain_Navigated(object sender, NavigationEventArgs e)
        {
            MusicPlayerMaster.Visibility = Visibility.Visible;

            if (e.SourcePageType == typeof(HomePage))
                nvgMain.Header = "Home";

            else if (e.SourcePageType == typeof(MusicLibrary))
                nvgMain.Header = "Music Library";

            else if (e.SourcePageType == typeof(VideoLibrary))
                nvgMain.Header = "Video Library";

            else if (e.SourcePageType == typeof(QueuePage))
            {
                MusicPlayerMaster.Visibility = Visibility.Collapsed;

                nvgMain.Header = "Play Queue";
            }

            else if (e.SourcePageType == typeof(SettingsPage))
                nvgMain.Header = "App Settings";
            else if (e.SourcePageType == typeof(LogPage))
                nvgMain.Header = "App Logs";

            else if (e.SourcePageType == typeof(SearchResults))
                nvgMain.Header = Headersearch;


            else
                nvgMain.Header = "";
        }
        private void nvgMain_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            MusicPlayerMaster.Visibility = Visibility.Visible;
            if (args.IsSettingsSelected)
            {
                frmMain.Navigate(typeof(SettingsPage));
                return;
            }

            if (args.SelectedItemContainer == null)
                return;

            Type? pageType = null;

            if (args.SelectedItemContainer == nvgitHome)
                pageType = typeof(HomePage);

            else if (args.SelectedItemContainer == nvgitMusic)
                pageType = typeof(MusicLibrary);

            else if (args.SelectedItemContainer == nvgitVideo)
                pageType = typeof(VideoLibrary);

            else if (args.SelectedItemContainer == nvgitQueue)
            {
                frmMain.Navigate(typeof(QueuePage), null, new DrillInNavigationTransitionInfo());
                MusicPlayerMaster.Visibility = Visibility.Collapsed;
            }

            if (pageType != null && frmMain.CurrentSourcePageType != pageType)
            {
                frmMain.Navigate(pageType, null, new DrillInNavigationTransitionInfo());
            }
        }
        string stateofplay = "paused";
        string Headersearch = "Search results";

        private async void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (PlayerService.MasterPlayer == null) return;
            if (PlayerService.MasterPlayer.IsPlaying)
            {
                PlayerService.Pause();
                ToolTipService.SetToolTip(btnPlayPause, "Play");
            }
            else
            {
                PlayerService.Play();
                ToolTipService.SetToolTip(btnPlayPause, "Pause");
            }

        }
        private void UpdatePlayUI(bool isPlaying)
        {
            string iconName = isPlaying ? "pause" : "play";
            string toolTipText = isPlaying ? "Pause" : "Play";

            imgPlayPause.Source = new BitmapImage(new Uri($"ms-appx:///Assets/{iconName}.png"));
            ToolTipService.SetToolTip(btnPlayPause, toolTipText);
        }
        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            QueueHandler.PlayPrevious();
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            QueueHandler.PlayNext();
        }



        private void nvgMain_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (frmMain.CanGoBack)
            {
                frmMain.GoBack();
            }
        }

        private void sldVolume_ValueChanged(double obj)
        {

            PlayerService.VolumeChange(obj);
            txtVolume.Text = PlayerService.currentvol + "%";
            VolumeIcon.Foreground = PlayerService.volForeground;
            VolumeIcon.Glyph = PlayerService.volumeglyph;
        }

        private void btnSkipForward_Click(object sender, RoutedEventArgs e)
        {

        }
        public void RestoreBackOriginalWindow(long time)
        {

        }
        private void btnSkipBack_Click(object sender, RoutedEventArgs e)
        {
        }
        string videospeed = "1";
        private void btnSpeedfly_Click(object sender, RoutedEventArgs e)
        {
            ttSpeedCustom.IsOpen = false;
            var menuflyoutitem = (RadioMenuFlyoutItem)sender;

            string speed = menuflyoutitem.Text;
            videospeed = speed;
            if (PlayerService.MasterPlayer != null)
            {
                if (menuflyoutitem != null)
                {

                    if (double.TryParse(speed, System.Globalization.CultureInfo.InvariantCulture, out double speedfloat))
                    {
                        PlayerService.MasterPlayer.Speed = speedfloat;
                    }
                }
            }
            //ttSpeedCustom.IsOpen = false;
            //var menuflyoutitem = (RadioMenuFlyoutItem)sender;

            //string speed = menuflyoutitem.Text;
            //videospeed = speed;
            //if (player != null && maintimer != null)
            //{


            //    }

            //}
        }

        private void customSpeed_Click(object sender, RoutedEventArgs e)
        {
            nmbSpeedCustom.Value = Convert.ToDouble(videospeed);
            ttSpeedCustom.IsOpen = true;
        }

        private async void btnEffects_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(PlaybackState.CurrentlyPlayingPath))
            {
                ShowSongDetails(PlaybackState.CurrentlyPlayingPath);
            }
        }

        private void btnMiniPlayer_Click(object sender, RoutedEventArgs e)
        {
        }
        private ObservableCollection<SongModel> AllAvailableSongs = new ObservableCollection<SongModel>();

        private async Task ScanAllFoldersAsync()
        {
            AllAvailableSongs.Clear();

            var userPaths = UserDataPaths.GetDefault();

            string[] searchPaths =
            {
        userPaths.Music,
        userPaths.Downloads,
        userPaths.Pictures,
        userPaths.Videos
    };

            var extensions = new HashSet<string>
    {
        ".mp3", ".wav", ".aac", ".flac", ".m4a", ".ogg", ".wma",
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm"
    };

            foreach (var path in searchPaths)
            {
                if (!Directory.Exists(path))
                    continue;

                IEnumerable<string> files;

                try
                {
                    files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);
                }
                catch
                {
                    continue;
                }

                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file).ToLower();

                    if (!extensions.Contains(ext))
                        continue;

                    try
                    {
                        StorageFile storageFile =
                            await StorageFile.GetFileFromPathAsync(file);

                        MusicProperties props =
                            await storageFile.Properties.GetMusicPropertiesAsync();

                        var song = new SongModel
                        {
                            Title = string.IsNullOrEmpty(props.Title)
                                ? Path.GetFileNameWithoutExtension(file)
                                : props.Title,

                            Artist = string.IsNullOrEmpty(props.Artist)
                                ? "Unknown"
                                : props.Artist,

                            AlbumName = props.Album ?? "",

                            SongDuration = props.Duration,

                            FilePath = file
                        };

                        await DispatcherQueue.EnqueueAsync(() =>
                        {
                            AllAvailableSongs.Add(song);
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex.Message, "HomeWindow", Logger.LogLevelType.Error);
                    }
                }
            }
        }
        private void btnSetCustomSpeed_Click(object sender, RoutedEventArgs e)
        {
            if (PlayerService.MasterPlayer != null)
            {


                if (!double.IsNaN(nmbSpeedCustom.Value))
                {
                    string speed = nmbSpeedCustom.Value.ToString();
                    videospeed = speed;

                    if (double.TryParse(speed, System.Globalization.CultureInfo.InvariantCulture, out double speedfloat))
                    {
                        PlayerService.MasterPlayer.Speed = speedfloat;
                        switch (speedfloat)
                        {
                            case 0.25:
                                spquarter.IsChecked = true;
                                break;
                            case 0.5:
                                sphalf.IsChecked = true;
                                break;
                            case 0.75:
                                spthreefourth.IsChecked = true;
                                break;
                            case 1.0:
                                spone.IsChecked = true;
                                break;
                            case 1.25:
                                sponequarter.IsChecked = true;
                                break;
                            case 1.5:
                                sponehalf.IsChecked = true;
                                break;
                            case 1.75:
                                sponethreefourth.IsChecked = true;
                                break;
                            case 2.0:
                                sptwo.IsChecked = true;
                                break;
                            default:
                                // Optional: Log if an unsupported speed is passed
                                break;
                        }
                    }
                }
            }
        }



        private void hypGoToLocation_Click(object sender, RoutedEventArgs e)
        {
            NavigateToExplorer.GoToLocation(txtInfoFilePath.Text);
        }
        private async void txtInfoRating_ValueChanged(RatingControl sender, object args)
        {
            if (PlaybackState.CurrentlyPlayingPath == null) return;

            // Convert 1-5 back to Windows 0-99
            uint shellRating = sender.Value switch
            {
                5 => 99,
                4 => 75,
                3 => 50,
                2 => 25,
                1 => 1,
                _ => 0
            };

            StorageFile file = await StorageFile.GetFileFromPathAsync(PlaybackState.CurrentlyPlayingPath);
            var propertiesToSave = new Dictionary<string, object>
    {
        { "System.Rating", shellRating }
    };

            try
            {
                await file.Properties.SavePropertiesAsync(propertiesToSave);
            }
            catch (Exception ex)
            {
                // If the file is still 'Blocked' by Windows Security, this will fail
                Logger.Log($"Failed to save rating: {ex.Message}", "HomeWindow", Logger.LogLevelType.Error);
            }
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            CopyToClipboard.CopyStringToClipboard(txtInfoFilePath.Text);
        }
        public async void ShowSongDetails(string FilePath)
        {
            if (FilePath == null) return;

            StorageFile file = await StorageFile.GetFileFromPathAsync(FilePath);
            var musicProps = await file.Properties.GetMusicPropertiesAsync();
            var basicProps = await file.GetBasicPropertiesAsync();
            var propertyKeys = new List<string>
{
    "System.Rating",
    "System.Audio.SampleRate",
    "System.Audio.ChannelCount",
    "System.Music.Composer",
    "System.Music.Conductor",
    "System.Comment",
    "System.Music.Artist" // Contributing Artists
};
            IDictionary<string, object> extraProps = await file.Properties.RetrievePropertiesAsync(propertyKeys);

            // 2. DIALOG & FILE HEADER INFO
            dlgFileInfo.Title = $"Information on '{Path.GetFileName(FilePath)}'";
            txtInfoFileName.Text = !string.IsNullOrWhiteSpace(musicProps.Title) ? musicProps.Title : file.DisplayName;
            txtInfoFilePath.Text = FilePath;
            txtInfoFileType.Text = file.FileType;

            // 3. MUSIC METADATA (Artist, Album, etc.)
            txtInfoAlbum.Text = musicProps.Album;
            txtInfoArtist.Text = musicProps.Artist;
            txtInfoTrack.Text = musicProps.TrackNumber.ToString();
            txtInfoYear.Text = musicProps.Year.ToString();
            txtInfoGenre.Text = musicProps.Genre.Any() ? string.Join("; ", musicProps.Genre) : "Unknown Genre";

            // Contributing Artists (Fallback to main artist if null)
            if (extraProps.TryGetValue("System.Music.Artist", out object? artistObj) && artistObj is string[] artists)
                txtInfoContributingArtists.Text = string.Join("; ", artists);
            else
                txtInfoContributingArtists.Text = musicProps.Artist;

            // 4. TECHNICAL SPECS (Bitrate, Sample Rate, Duration)
            txtInfoBitrate.Text = musicProps.Bitrate > 0 ? $"{musicProps.Bitrate / 1000} kbps" : "Unknown Bitrate";
            txtInfoAudioSampleRate.Text = extraProps.TryGetValue("System.Audio.SampleRate", out object? sr) ? $"{sr} Hz" : "Unknown";
            txtInfoChannels.Text = extraProps.TryGetValue("System.Audio.ChannelCount", out object? ch) ? ch.ToString() : "Unknown";
            txtInfoSpeed.Text = videospeed;

            // Duration Formatting
            txtInfoDuration.Text = musicProps.Duration.TotalHours >= 1
                ? musicProps.Duration.ToString(@"h\:mm\:ss")
                : musicProps.Duration.ToString(@"m\:ss");

            // 5. ADDITIONAL DETAILS (Rating, Comments, Dates)
            // Rating Conversion
            if (extraProps.TryGetValue("System.Rating", out object? rateObj) && rateObj is uint rawRating)
            {
                txtInfoRating.Value = rawRating switch
                {
                    >= 99 => 5,
                    >= 75 => 4,
                    >= 50 => 3,
                    >= 25 => 2,
                    >= 1 => 1,
                    _ => 0
                };
            }
            else { txtInfoRating.Value = 0; }

            // Comments
            txtInfoComments.Text = extraProps.TryGetValue("System.Comment", out object? commObj) ? commObj.ToString() : "";

            // Composers / Conductors
            txtInfoComposers.Text = extraProps.TryGetValue("System.Music.Composer", out object? compObj) && compObj is string[] comps ? string.Join("; ", comps) : "-";
            txtInfoConductors.Text = extraProps.TryGetValue("System.Music.Conductor", out object? condObj) && condObj is string[] conds ? string.Join("; ", conds) : "-";

            // Timestamps
            txtInfoCreatedOn.Text = file.DateCreated.ToString("G");
            txtInfoLastModified.Text = basicProps.DateModified.ToString("G");
            if (App.HomeWindowInstance == null) return;
            Debug.WriteLine("sd");
                OceanContentDialog.Show("Information", "Save", "", "OK", OceanContentDialogDefault.Primary, grdFileInfo, this.XamlRoot, 700, 700, OceanContentDialogType.Elevated, App.HomeWindowInstance, "saveicon", "", "");
            
            // 6. SHOW DIALOG
        }
        public static MasterPage? MainPageInstance { get; private set; }
        private void btnVolume_Click(object sender, RoutedEventArgs e)
        {
            if (PlayerService.currentvol == "0")
            {
                double orig = Convert.ToDouble(PlayerService.originalvolume);
                sldVolume.Value = orig;
                PlayerService.VolumeChange(orig);

            }
            else
            {
                sldVolume.Value = 0;
                PlayerService.VolumeChange(0);
            }
            txtVolume.Text = PlayerService.currentvol + "%";
            VolumeIcon.Foreground = PlayerService.volForeground;
            VolumeIcon.Glyph = PlayerService.volumeglyph;
        }


        private async void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var searchTerm = sender.Text.ToLower().Trim();

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    sender.ItemsSource = null;
                }
                else
                {
                    var suggestions = AllAvailableSongs
                 .Where(s => s.Title != null &&
            s.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                   .OrderByDescending(s =>
    s.Title?.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
                        .ThenBy(s => s.Title)
                        .ToList();

                    sender.ItemsSource = suggestions;
                }
            }

        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string query = args.QueryText.Trim();

            if (string.IsNullOrEmpty(query)) return;

            var results = AllAvailableSongs
            .Where(s => s.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            // If the user picked a specific suggestion from the list, 
            // it might be better to just send that ONE song instead of a filtered list.
            if (args.ChosenSuggestion != null)
            {
                if (args.ChosenSuggestion is SongModel selectedSong)
                {
                    frmMain.Navigate(typeof(SearchResults),
                        new List<SongModel> { selectedSong });

                    nvgMain.Header = $"Results for '{selectedSong.Title}'";
                    Headersearch = $"Results for '{selectedSong.Title}'";
                }
            }
            else if (results.Any())
            {
                frmMain.Navigate(typeof(SearchResults), results);
                nvgMain.Header = $"Results for '{query}'";
                Headersearch = $"Results for '{query}'";
            }
            else
            {
                frmMain.Navigate(typeof(SearchResults), "no");
                nvgMain.Header = $"No results for '{query}'";
                Headersearch = $"No results for '{query}'";
            }
        }

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = ((SongModel)args.SelectedItem).Title;
            if (args.SelectedItem is SongModel sngl)
            {
                sender.Text = sngl.Title;
            }
            //       ObservableCollection<string> temp = new();
            //      temp.Add(((SongModel)args.SelectedItem).FilePath);
            //        LoadFileFromPath(temp);

        }
        TextBlock currentinfoedit = new();
        string old = "";
        private void txtInfoAlbum_PointerPressed(object sender, PointerRoutedEventArgs e)
        {

        }



        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var Buttond = sender as Button;
            if (Buttond == null) return;
            var txtBlock = Buttond.Content as TextBlock;
            if (txtBlock != null)
            {
                ttEditInfo.Target = txtBlock;
                ttEditInfo.IsOpen = true;
                ttEditInfo.Title = "Edit " + txtBlock.Tag + " info";
                currentinfoedit = txtBlock;
                txtEditTT.Tag = txtBlock.Tag;
                txtEditTT.Text = txtBlock.Text;
                old = txtBlock.Text;
            }
        }
        string oldpath = "";
        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            //Relocate missing file
            if (frmMain.Content is MusicLibrary activePage)
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.FileTypeFilter.Add("*");
                WinRT.Interop.InitializeWithWindow.Initialize(
                    picker,
                    WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance)
                );

                StorageFile newFile = await picker.PickSingleFileAsync();
                if (newFile != null)
                {
                    activePage.UpdatePath(oldpath, newFile.Path);
                    ttFileMissing.IsOpen = false;
                    iBFileMissing.Severity = InfoBarSeverity.Success;
                    iBFileMissing.Title = "";
                    iBFileMissing.Message = "File relocated successfully";
                    ttFileMissing.IsOpen = true;
                }
            }
        }

        private void txtEditTT_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtEditTT.Text == "")
            {
                txtEditTT.Text = old;
            }
            string tagtoedit = txtEditTT.Tag?.ToString() ?? "";
            if (tagtoedit == null) return;
            currentinfoedit.Text = txtEditTT.Text;
        }

        private void mnftEditProperty_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuFlyoutItem)sender;

            var flyout = menuItem.Parent as MenuFlyout;

            var button = flyout?.Target as Button;

            if (button != null)
            {

                if (button == null) return;
                var txtBlock = button.Content as TextBlock;
                if (txtBlock != null)
                {
                    ttEditInfo.Target = txtBlock;
                    ttEditInfo.IsOpen = true;
                    ttEditInfo.Title = "Edit " + txtBlock.Tag + " info";
                    currentinfoedit = txtBlock;
                    txtEditTT.Tag = txtBlock.Tag;
                    txtEditTT.Text = txtBlock.Text;
                    old = txtBlock.Text;
                }
            }

        }

        private void mnftCopyProperty_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuFlyoutItem)sender;

            var flyout = menuItem.Parent as MenuFlyout;

            var button = flyout?.Target as Button;

            if (button != null)
            {
                string textToCopy = button.Content?.ToString() ?? string.Empty;

                CopyToClipboard.CopyStringToClipboard(textToCopy);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {

            string pfn = Package.Current.Id.FamilyName;

            await Launcher.LaunchUriAsync(
                new Uri($"ms-settings:defaultapps?registeredAppUser={pfn}"));
        }

        private void hypGoToSettings_Click(object sender, RoutedEventArgs e)
        {
            frmMain.Navigate(typeof(SettingsPage), "WinRelate");

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox chck && chck.IsChecked == true)
            {
                txtUserInfoDefaultApp.Visibility = Visibility.Visible;
                hypGoToSettings.Visibility = Visibility.Visible;
            }
        }
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private async void ttDefaultAppSet_CloseButtonClick(TeachingTip sender, object args)
        {
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            if (chckShowDefault.IsChecked == true)
            {
                currentSettings.ShowDefaultMessage = true;
            }
            else
            {
                currentSettings.ShowDefaultMessage = false;
            }
            await SettingsHelper.SaveSettingsAsync(currentSettings);

        }

        private void btnEffects_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void btnSetReverb_Click(object sender, RoutedEventArgs e)
        {
        
        }
        private void sldPitch_ValueChanged(double obj)
        {
            if (PlayerService.MasterPlayer != null)
            {
                if (obj == 0) return;
                PlayerService.MasterPlayer.Config.Audio.Pitch = obj;
                PlayerService.MasterPlayer.Config.Audio.ReloadFilters();
                txtPitchValue.Text = obj.ToString("F4");
                if (stkPitchCustom.Visibility == Visibility.Visible)
                {
                    nmbPitchCustom.Value = sldPitch.Value;
                }
            }
        }
        private void nmbPitchCustom_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            sldPitch.Value = nmbPitchCustom.Value;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (stkPitchCustom.Visibility == Visibility.Visible)
            {
                stkPitchCustom.Visibility = Visibility.Collapsed;
            }
            else
            {
                stkPitchCustom.Visibility = Visibility.Visible;

            }
            nmbPitchCustom.Value = sldPitch.Value;
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            if (PlayerService.MasterPlayer != null)
            {
                if (nmbPitchCustom.Value == 0) return;
                PlayerService.MasterPlayer.Config.Audio.Pitch = nmbPitchCustom.Value;
                PlayerService.MasterPlayer.Config.Audio.ReloadFilters();
                txtPitchValue.Text = nmbPitchCustom.Value.ToString("F4");
            }
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            if (PlayerService.MasterPlayer != null)
            {
                PlayerService.MasterPlayer.Config.Audio.Pitch = 1;
                PlayerService.MasterPlayer.Config.Audio.ReloadFilters();
                txtPitchValue.Text = "1.0";
                sldPitch.Value = 1;
                if (stkPitchCustom.Visibility == Visibility.Visible)
                {
                    nmbPitchCustom.Value = sldPitch.Value;
                }
            }
        }
    
        private void mnftPitch_Click(object sender, RoutedEventArgs e)
        {
            ttPitch.IsOpen = true;
        }
        private void sldMain_DragStarted()
        {
            PlayerService.SldMain_DragStarted();
        }

        private void sldMain_DragCompleted()
        {
            PlayerService.SldMain_DragCompleted(sldMain);
        }


    }
}
