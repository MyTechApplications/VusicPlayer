using Flyleaf.FFmpeg;
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
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    public sealed partial class PlaylistParameters : UserControl
    {
        #region Fields
        public ObservableCollection<SongModel> AllSongs { get; set; } = new();
        bool IsNewPlaylist = true;
        TimeSpan Duration = new();
        PlaylistProperties correspondPlaylist = new();
        Frame MainFrameNavig = new();
        #endregion

        #region Initialize
        private void InitializeMain()
        {
            AllSongs.CollectionChanged += AllSongs_CollectionChanged;
            lstViewPlaylistAddedSongs.ItemsSource = AllSongs;
            btnEditPlaylistCover.IsEnabled = false;

            
        }
        #endregion

        #region Loading
        public async Task LoadPlaylistCreationDialog(bool IsCreationOfPlaylist, PlaylistProperties playlistProperties, Frame frm)
        {
            if (IsCreationOfPlaylist == false)
            {
                correspondPlaylist = playlistProperties;
                AllSongs.Clear();
                MainFrameNavig = frm;
                txtEditPlaylistName.Text = playlistProperties.PlaylistName;
                txtEditGenre.Text = playlistProperties.PlaylistGenre;
                if (playlistProperties.Thumbnail != null)
                {
                    this.DispatcherQueue.TryEnqueue(() => {
                        imgPlaylistCov.Source = new BitmapImage(playlistProperties.Thumbnail);
                    });
                }

                txtAddedSongs.Text = $"Added Songs: {playlistProperties.PlaylistCount}";
                foreach (var song in playlistProperties.SongsPaths)
                {
                    if (File.Exists(song))
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(song);
                        MusicProperties properties = await file.Properties.GetMusicPropertiesAsync();

                        string title = !string.IsNullOrWhiteSpace(properties.Title) ? properties.Title : file.DisplayName;
                        string album = !string.IsNullOrWhiteSpace(properties.Album) ? properties.Album : "Unknown Album";
                        string artist = !string.IsNullOrWhiteSpace(properties.Artist) ? properties.Artist : "Unknown Artist";

                        Duration += properties.Duration;

                        AllSongs.Add(new SongModel
                        {
                            Title = title,
                            AlbumName = album,
                            Artist = artist,
                            SongDuration = properties.Duration,
                            FilePath = file.Path
                        });
                    }
                }
                playlistcov = correspondPlaylist.Thumbnail!.ToString();
            }
        }
        #endregion

        #region Saving
        public async void SavePla()
        {
            Modifiedplaylist.Name = txtEditPlaylistName.Text;
            Modifiedplaylist.Genre = txtEditGenre.Text;
            Modifiedplaylist.Thumbnail = playlistcov;
            Modifiedplaylist.SongsPaths = correspondPlaylist.SongsPaths;
            if (txtEditPlaylistName.Text == "")
            {
                mssgBar.IsOpen = true;
                mssgBar.Title = "Error";
                mssgBar.Message = "Playlist name cannot be empty.";
                mssgBar.Severity = InfoBarSeverity.Error;
                return;
            }
            var currentSettings = await SettingsHelper.LoadSettingsAsync();

            var playlistInMasterList = currentSettings.SavedPlaylists
           .FirstOrDefault(p => p.PlaylistId == correspondPlaylist.PlaylistId);
            if(playlistInMasterList != null)
            {
                string playlistname = correspondPlaylist.PlaylistName ?? "Unknown Playlist";
                string Genre = correspondPlaylist.PlaylistGenre ?? "";
                playlistInMasterList.PlaylistName =
     string.IsNullOrEmpty(txtEditPlaylistName.Text)
         ? playlistname
         : txtEditPlaylistName.Text;

                playlistcov = playlistInMasterList.Thumbnail!.ToString();

                playlistInMasterList.PlaylistGenre = txtEditGenre.Text;
                Uri defaultPath = new Uri(playlistcov);


                if (playlistcov != "")
                {
                    defaultPath = new Uri(playlistcov);
                }
                else
                {
                    Uri darkIcon = new Uri("ms-appx:///Assets/playlistdefaultdark.png");

                    defaultPath = darkIcon;
                }
                if (playlistcov != "")
                {
                    defaultPath = new Uri(playlistcov);
                }

                playlistInMasterList.Thumbnail = defaultPath;

                playlistInMasterList.SongsPaths?.Clear();
                foreach (var item in AllSongs)
                {
                    if (item.FilePath != null)
                    {
                        playlistInMasterList.SongsPaths?.Add(item.FilePath);
                    }
                }
                playlistInMasterList.PlaylistCount = $"{AllSongs.Count} {(AllSongs.Count == 1 ? "item" : "items")}";
                await SettingsHelper.SaveSettingsAsync(currentSettings);
            }
          
        }
        string playlistcov = "";
        public  async Task<PlaylistProperties> CreateNewPlaylistInData()
        {
            Debug.WriteLine("CreationCalled");
            var currentSettings = await SettingsHelper.LoadSettingsAsync();

            string baseName = txtEditPlaylistName.Text.Trim();
            if (string.IsNullOrEmpty(baseName)) baseName = "New Playlist";

            string finalName = baseName;
            int counter = 1;
            while (currentSettings.SavedPlaylists.Any(p =>
                string.Equals(p.PlaylistName, finalName, StringComparison.OrdinalIgnoreCase)))
            {
                finalName = $"{baseName} ({counter++})";
            }
            Uri defaultPath = new Uri("ms-appx:///Assets/playlistdefaultdark.png");
       

            if (playlistcov != "")
            {
               defaultPath = new Uri(playlistcov);
           }
            else
            {
                  Uri darkIcon = new Uri("ms-appx:///Assets/playlistdefaultdark.png");

                // Set your initial default (e.g., based on current theme)
                defaultPath = darkIcon;
            }
            string playlistID = Guid.NewGuid().ToString("N");
            var newPlaylist = new PlaylistProperties
            {
                PlaylistName = finalName,
                PlaylistId = playlistID,
                PlaylistCount = $"{AllSongs.Count} {(AllSongs.Count == 1 ? "item" : "items")}",
                PlaylistNowPlaying = "",
                PlaylistGenre = txtEditGenre.Text,
                SongsPaths = AllSongs
    .Select(s => s.FilePath)
    .Where(path => path != null)
    .ToHashSet()!,
                Thumbnail = defaultPath,
                DateCreation = DateTime.Now.Date,
            };
            currentSettings.SavedPlaylists.Add(newPlaylist);
            await SettingsHelper.SaveSettingsAsync(currentSettings);
            return newPlaylist;
        }
        #endregion

        #region ClickEvents
        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SongModel song)
            {
                AllSongs.Remove(song);
            }
            if (lstViewPlaylistAddedSongs.Items.Count == 0)
            {
                txtNullAddedSongs.Visibility = Visibility.Visible;
            }
        }
        private void btnActualReset_Click(object sender, RoutedEventArgs e)
        {
            txtEditPlaylistName.Text = "";

            txtEditGenre.Text = "";
            AllSongs.Clear();
            imgPlaylistCov.Source = null;
            CoverOptions.Visibility = Visibility.Collapsed;
            btnAddPlaylistCover.IsEnabled = true;
        }
        public async Task btnAddSongsClick()
        {
            if (App.OceanDialogInstance == null)
            {
                mssgBar.IsOpen = true;
                mssgBar.Title = "Error";
                mssgBar.Message = "An unexpected error occured. Check log details in Settings Page.";
                mssgBar.Severity = InfoBarSeverity.Error;
                Logger.Log("Error code 0x0012oc. Refer the github page for more details.", "PlaylistCreation", Logger.LogLevelType.Error);
                return;
            }

            var picker = new FileOpenPicker();
        
            var hwnd = WindowNative.GetWindowHandle(App.OceanDialogInstance);
            InitializeWithWindow.Initialize(picker, hwnd);

            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".m4a");
            picker.FileTypeFilter.Add(".ogg");

            var files = await picker.PickMultipleFilesAsync();

            if (files == null) return;

            foreach (var file in files)
            {
                if (!AllSongs.Any(s => s.FilePath == file.Path))
                {
                    var musicProps = await file.Properties.GetMusicPropertiesAsync();

                    string duration = FormatDuration(musicProps.Duration);
                    correspondPlaylist.SongsPaths?.Add(file.Path);
                    AllSongs.Add(new SongModel
                    {
                        Title = musicProps.Title,
                        SongDuration = musicProps.Duration,
                        FilePath = file.Path

                    });
                }
            }
            lstViewPlaylistAddedSongs.StartBringIntoView();
       lstViewPlaylistAddedSongs.ScrollIntoView(AllSongs.LastOrDefault());
        }
        public async void btnAddSongs_Click(object sender, RoutedEventArgs e)
        {

   
        await    btnAddSongsClick();
            btnAddPlaylistCover.IsEnabled = true;
            btnAddSongs.IsEnabled = true;
        }

        private async void btnAddPlaylistCover_Click(object sender, RoutedEventArgs e)
        {
          
            
          
        }
        private void btnRemovePlaylistCover_Click(object sender, RoutedEventArgs e)
        {
            ToolTipService.SetToolTip(imgPlaylistCov, "");
            CoverOptions.Visibility = Visibility.Collapsed;
            btnAddPlaylistCover.IsEnabled = true;
            imgPlaylistCov.Source = new BitmapImage(new Uri("ms-appx:///Assets/playlistdefaultdark.png"));
        }
        private void txtEditPlaylistName_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.DispatcherQueue.TryEnqueue(() =>
                {
                    textBox.SelectAll();
                });
            }
        }
        #endregion

        public PlaylistParameters()
        {
            InitializeComponent();
            InitializeMain();
        }

        private void AllSongs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            txtNullAddedSongs.Visibility = AllSongs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            txtAddedSongs.Text = "Added Songs: " + $"{AllSongs.Count} {(AllSongs.Count == 1 ? "item" : "items")}";
        }
        private string FormatDuration(TimeSpan duration)
        {
            if (duration.Hours > 0)
                return duration.ToString(@"hh\:mm\:ss");

            return duration.ToString(@"mm\:ss");
        }

    }
}
