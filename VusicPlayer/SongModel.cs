using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.System;
using Windows.UI;

namespace VusicPlayer
{
    public class SongModel : INotifyPropertyChanged
    {
        private string? _title;
        private int _year;
        private string? _artist;
        private string? _albumName;
        public SongModel()
        {
            PlaybackState.CurrentlyPlayingChanged += OnPlayingChanged;
            PlayerService.CurrentPlayState += PlayerService_CurrentPlayState;
            //       UpdateVisualState(PlaybackState.CurrentlyPlayingPath);
        }

        private void PlayerService_CurrentPlayState(string? obj)
        {
            if (obj == "Paused")
            {
                if (FilePath == PlaybackState.CurrentlyPlayingPath)
                {
                    Glyph = "\uE768"; // Paused icon
                    TitleColor = new SolidColorBrush(Microsoft.UI.Colors.Cyan);
                    isPaused = true;
                }

            }
            else
            {
                if (FilePath == PlaybackState.CurrentlyPlayingPath)
                {
                    isPaused = false;
                    Glyph = "\uE769"; // Paused icon
                    TitleColor = new SolidColorBrush(Microsoft.UI.Colors.Cyan);
                }
            }
        }

        public bool isPaused { get; set; }
        private void OnPlayingChanged(string? newPath)
        {
            UpdateVisualState(newPath);
        }
        private void UpdateVisualState(string? currentPath)
        {
            if (FilePath == currentPath)
            {
                if (PlayerService.MasterPlayer.IsPlaying)
                    Glyph = "\uE769"; // Playing icon
                else
                    Glyph = "\uE768"; // Paused icon
                TitleColor = new SolidColorBrush(Microsoft.UI.Colors.Cyan);
            }
            else
            {
                Glyph = "\uEC4F"; // Default icon
                TitleColor = new SolidColorBrush(Microsoft.UI.Colors.White);
            }
        }

        public bool isPlaying => FilePath == PlaybackState.CurrentlyPlayingPath;
        public string? Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }
        public int Year
        {
            get => _year;
            set { _year = value; OnPropertyChanged(); }
        }

        public string? Artist
        {
            get => _artist;
            set { _artist = value; OnPropertyChanged(); }
        }
        private Visibility isMovableitem = Visibility.Visible;
        public Visibility IsMovableItem
        {
            get => isMovableitem;   
            set
            {
                if (isMovableitem != value)
                {
                    isMovableitem = value;
                    OnPropertyChanged();
                }
            }
        }
        public string? AlbumName
        {
            get => _albumName;
            set { _albumName = value; OnPropertyChanged(); }
        }
        private Brush _titleColor = new SolidColorBrush(Microsoft.UI.Colors.White); // Safe for any thread!
        public Visibility VisibilityOfStrikethrough => IsCompleted
            ? Visibility.Visible
            : Visibility.Collapsed;
        public Brush TitleColor
        {
            get => _titleColor;
            set
            {
                _titleColor = value;
                OnPropertyChanged(nameof(TitleColor));
            }
        }
        private string _glyph = "\uEC4F";// Default color
        public string Glyph
        {
            get => _glyph;
            set { _glyph = value; OnPropertyChanged(nameof(Glyph)); }
        }
        private bool _isCompleted;
        private bool isFav;
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
                    {
                        OnPropertyChanged();

                        // Explicitly notify that the dependent property has changed
                        OnPropertyChanged(nameof(VisibilityOfStrikethrough));
                    });
                }
            }
        }
        public TimeSpan? SongDuration { get; set; }
        public string FormattedDuration => SongDuration.HasValue
            ? $"{(int)SongDuration.Value.TotalMinutes:D2}:{SongDuration.Value.Seconds:D2}"
            : "00:00";
        public string? FilePath { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        private bool _isFavorite;
        public bool IsFavourite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite != value)
                {
                    _isFavorite = value;
                    // This "notifies" the Button and the Menu to update their visuals
                    OnPropertyChanged();
                }
            }
        }
        private double opaci;
        public double FavOpacity
        {
            get => opaci;
            set
            {
                if (opaci != value)
                {
                    opaci = value;
                    // This "notifies" the Button and the Menu to update their visuals
                    OnPropertyChanged();
                }
            }
        }
        private string favtext = "Add to favourites";
        public string FavString
        {
            get => favtext;
            set
            {
                if (favtext != value)
                {
                    favtext = value;
                    // This "notifies" the Button and the Menu to update their visuals
                    OnPropertyChanged();
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            });
        }
        // You can add an Icon property here later!
    }
}
