using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI;

namespace VusicTools
{
    // Helper class to access ProtectedCursor since Rectangle is sealed

    public sealed partial class Timeline : UserControl
    {
        public Timeline()
        {
            InitializeComponent();
            CreateRuler();
            AddTrack(TrackType.Audio, "Audio Track 1");
            AddTrack(TrackType.Video, "Video Track 1");
        }
        private double _zoomLevel = 100.0;
        private const double MinZoom = 10.0;
        private const double MaxZoom = 500.0;
        private double _nextTrackY = 0; // Tracks the vertical position

        public void AddTrack(TrackType type, string name)
        {
            double height = (type == TrackType.Video) ? 80 : 60;
            var color = (type == TrackType.Video) ? Microsoft.UI.Colors.RoyalBlue : Microsoft.UI.Colors.SeaGreen;

            // 1. Create Label Control
            var label = new Border
            {
                Height = height,
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.DimGray),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = new TextBlock
                {
                    Text = name,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0),
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
                }
            };

            // 2. Create Track Lane (The background on the canvas)
            var lane = new Rectangle
            {
                Height = height,
                Width = canvTimeline.Width,
                Fill = new SolidColorBrush(color) { Opacity = 0.1 },
                Stroke = new SolidColorBrush(Microsoft.UI.Colors.Black) { Opacity = 0.2 },
                StrokeThickness = 0.5
            };

            // 3. Simple Logic: Add to the UI groups directly
            if (type == TrackType.Video)
            {
                VideoHeaderGroup.Children.Add(label);
            }
            else
            {
                AudioHeaderGroup.Children.Add(label);
            }

            canvTimeline.Children.Add(lane);

            // Re-position everything on the canvas so they match the StackPanel heights
            UpdateLanePositions();
        }

        private void UpdateLanePositions()
        {
            double currentY = 0;

            // Position all Video Lanes
            foreach (var child in canvTimeline.Children.OfType<Rectangle>())
            {
                // We calculate position based on the height of the elements in HeaderGroups
                // This keeps the Canvas and the Labels perfectly in sync
                Canvas.SetTop(child, currentY);
                currentY += child.Height;
            }
        }
        private string FormatTime(double totalSeconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(totalSeconds);
            return t.ToString(@"hh\:mm\:ss");
        }
        private double CalculateOptimalInterval()
        {
            // We want a label roughly every 100-150 pixels for neatness
            double desiredPixelGap = 120.0;
            double secondsPerGap = desiredPixelGap / _zoomLevel;

            // Define standard "human-friendly" time steps
            double[] intervals = { 1, 2, 5, 10, 15, 30, 60, 120, 300, 600 };

            // Find the first interval that is larger than our secondsPerGap
            return intervals.FirstOrDefault(i => i >= secondsPerGap, 600);
        }
        private void CreateRuler()
        {
            canvRuler.Children.Clear();
            double interval = CalculateOptimalInterval();
            double totalSeconds = canvTimeline.Width / _zoomLevel;

            for (double i = 0; i <= totalSeconds; i += interval)
            {
                double x = i * _zoomLevel;

                // 1. Draw Major Tick
                var tick = new Rectangle
                {
                    Width = 1,
                    Height = 12,
                    Fill = new SolidColorBrush(Microsoft.UI.Colors.Gray)
                };
                Canvas.SetLeft(tick, x);
                Canvas.SetTop(tick, 18);
                canvRuler.Children.Add(tick);

                // 2. Draw Time Label (00:00:00)
                var label = new TextBlock
                {
                    Text = FormatTime(i),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Silver),
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                };
                Canvas.SetLeft(label, x + 5);
                Canvas.SetTop(label, 2);
                canvRuler.Children.Add(label);

                // 3. Optional: Draw Sub-ticks (e.g., 4 ticks between labels)
                double subStep = interval / 5;
                for (int j = 1; j < 5; j++)
                {
                    double subX = x + (j * subStep * _zoomLevel);
                    if (subX > canvTimeline.Width) break;

                    var subTick = new Rectangle
                    {
                        Width = 1,
                        Height = 5,
                        Fill = new SolidColorBrush(Microsoft.UI.Colors.DimGray)
                    };
                    Canvas.SetLeft(subTick, subX);
                    Canvas.SetTop(subTick, 25);
                    canvRuler.Children.Add(subTick);
                }
            }
        }

        private void btnAddAudTr_Click(object sender, RoutedEventArgs e)
        {
            AddTrack(TrackType.Audio, "Audio Track 1");
        }

        private void btnAddVidTr_Click(object sender, RoutedEventArgs e)
        {
            AddTrack(TrackType.Video, "Video Track 1");
        }

        private void btnZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ZoomIn();
        }
        private void UpdateZoom(double factor)
        {
            _zoomLevel = Math.Clamp(_zoomLevel * factor, MinZoom, MaxZoom);
            RefreshLayout();
        }
        private double CalculateTotalWidth()
        {
            double maxEndOfClip = 0;

            // Look at every clip currently on the canvas
            foreach (var element in canvTimeline.Children)
            {
           /*     if (element is TimelineItem item)
                {
                    // Calculate the pixel position of the end of this clip
                    double endPosition = Canvas.GetLeft(item) + item.ActualWidth;

                    if (endPosition > maxEndOfClip)
                    {
                        maxEndOfClip = endPosition;
                    }}
              */  
            }

            // Add 500px of "empty space" buffer so the user can drag items 
            // past the current last clip easily.
            return Math.Max(this.ActualWidth, maxEndOfClip + 500);
        }
        private void RefreshLayout()
        {
            // 1. Update the Ruler ticks
            CreateRuler();

            // 2. Reposition all clips based on their StartTime (seconds)
       /*     foreach (TimelineItem item in TimelineCanvas.Children.OfType<TimelineItem>())
            {
                Canvas.SetLeft(item, item.ClipData.StartTime * _zoomLevel);
                item.Width = item.ClipData.Duration * _zoomLevel;
            }*/

            // 3. Update Canvas width to fit the longest clip
            canvTimeline.Width = CalculateTotalWidth();
        }
        public void ZoomIn() => UpdateZoom(1.2);
        public void ZoomOut() => UpdateZoom(0.8);
        private void btnZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ZoomOut();
        }
    }
}