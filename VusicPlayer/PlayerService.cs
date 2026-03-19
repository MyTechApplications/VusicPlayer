using FlyleafLib.MediaPlayer;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using DispatcherTimer = Microsoft.UI.Xaml.DispatcherTimer;

namespace VusicPlayer
{

    public static class PlayerService
    {
        public static Player? MasterPlayer { get; set; }
        public static DispatcherTimer? maintimer { get; set; }
        public static TextBlock? txtRunningDur { get; set; }
        public static SliderReuse? sldMain { get; set; }
        public static void CreatePlayer()
        {
            if (MasterPlayer == null)
            {
                MasterPlayer = new Player();
            }
            if (maintimer == null)
            {
                maintimer = new DispatcherTimer();
                maintimer.Interval = TimeSpan.FromMilliseconds(250);
                maintimer.Tick += Maintimer_Tick;
            }

            txtRunningDur!.Text = "00:00:00";
            sldMain!.Value = 0;
            maintimer.Start();
        }
        public static ObservableCollection<string> OriginalPaths = new();
        public static ObservableCollection<string> ReceivedPaths = new();
        public static void PlayQueue(bool isshuffleenabled, ObservableCollection<string> Paths)
        {
            ReceivedPaths.Clear();
            OriginalPaths.Clear();
            OriginalPaths = Paths;
            ReceivedPaths = Paths;
            if (isshuffleenabled == true)
            {
                var shuffled = OriginalPaths.OrderBy(a => Guid.NewGuid()).ToList();
                ReceivedPaths.Clear();
                foreach (var shuffleditem in shuffled)
                {
                    ReceivedPaths.Add(shuffleditem);
                }
            }
            if (App.HomeWindowInstance is HomeWindow wind)
            {
                wind.LoadFileFromPath(ReceivedPaths);
            }
        }
        public static void UpdatePlayQueue(ObservableCollection<string> Paths)
        {
            if (App.HomeWindowInstance is HomeWindow wind)
            {
                wind.UpdateQueuePath(Paths);
            }
        }
        private static void SldMain_DragCompleted()
        {
            if (MasterPlayer == null) return;
            if (sldMain == null) return;
            if (txtRunningDur == null) return;
            double newPosition = sldMain.Value / sldMain.Maximum;
            MasterPlayer.CurTime = TimeSpan.FromSeconds(sldMain.Value).Ticks;
            var curTime = TimeSpan.FromTicks(MasterPlayer.CurTime);
            txtRunningDur.Text = curTime.ToString(@"hh\:mm\:ss");

            _isDragging = false;
            maintimer?.Start();
        }
        public static void AttachUI(TextBlock txtDur, SliderReuse slider)
        {
            txtRunningDur = txtDur;
            sldMain = slider;
            sldMain.DragCompleted += SldMain_DragCompleted;
            sldMain.DragStarted += SldMain_DragStarted;
        }

        private static void SldMain_DragStarted()
        {
            _isDragging = true;
            maintimer?.Stop();
        }

        private static bool _isDragging = false;
        private static void Maintimer_Tick(object? sender, object e)
        {
            if (!_isDragging && MasterPlayer != null && txtRunningDur != null && sldMain != null)
            {
                var curTime = TimeSpan.FromTicks(MasterPlayer.CurTime);
                txtRunningDur.Text = curTime.ToString(@"hh\:mm\:ss");
                sldMain.Value = curTime.TotalSeconds;
            }
        }
        public static void Play()
        {
            if (MasterPlayer == null) return;
            MasterPlayer.Play();
            maintimer?.Start();
        }
        public static void Pause()
        {
            if (MasterPlayer == null) return;
            MasterPlayer.Pause();
            maintimer?.Stop();
        }
    }
}
