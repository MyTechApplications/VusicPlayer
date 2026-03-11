using FlyleafLib.MediaPlayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{

    public class PlayerService
    {
        public static PlayerService Instance { get; } = new PlayerService();
        public static Player? MasterPlayer { get; set; }
        public void CreatePlayer()
        {
            if (MasterPlayer == null)
            {
                MasterPlayer = new Player();
            }
        }
        public void Play()
        {
            if (MasterPlayer == null) return;
            MasterPlayer.Play();
        }
        public void Pause()
        {
            if (MasterPlayer == null) return;
            MasterPlayer.Pause();
        }
    }
}
