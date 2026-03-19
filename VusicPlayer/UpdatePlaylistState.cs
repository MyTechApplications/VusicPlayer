using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class UpdatePlaylistState
    {
        public static PlaylistProperties? _currentPlaylist;
        public static async void UpdatePlaylistPlayState()
        {
            var currentSettings = await SettingsHelper.LoadSettingsAsync();
            if (_currentPlaylist != null)
            {
                foreach (var playlist in currentSettings.SavedPlaylists)
                {
                    playlist.PlaylistNowPlaying = string.Empty;
                }
                var playlistInMasterList = currentSettings.SavedPlaylists
               .FirstOrDefault(p => p.PlaylistName == _currentPlaylist.PlaylistName);
                if (playlistInMasterList != null)
                {
                    playlistInMasterList.PlaylistNowPlaying = "Now playing...";
                    await SettingsHelper.SaveSettingsAsync(currentSettings);
                }
            }
        }
    }
}
