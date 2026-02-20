using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace VusicPlayer
{
    public sealed partial class SliderReuse : UserControl
    {
        public SliderReuse()
        {
            this.InitializeComponent();
        }

        public double Minimum { get; set; } = 0;
        public double Maximum { get; set; } = 100;
        public double Value
        {
            get => _value;
            set
            {
                _value = Math.Clamp(value, Minimum, Maximum);
                UpdateVisuals();
            }
        }
        private double _value = 0;

        public event Action DragStarted;
        public event Action DragCompleted;
        public event Action<double> ValueChanged;

        private bool isDragging = false;

        #region Interaction Logic

        private void Root_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            isDragging = true;
            VisualStateManager.GoToState(this, "Pressed", true);
            InputLayer.CapturePointer(e.Pointer);
            DragStarted?.Invoke();
            UpdateFromPointer(e);
        }

        private void Root_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isDragging) UpdateFromPointer(e);
        }

        private void Root_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!isDragging) return;
            isDragging = false;
            TimePopup.IsOpen = false; // Hide tooltip
            VisualStateManager.GoToState(this, "Normal", true);
            InputLayer.ReleasePointerCapture(e.Pointer);
            DragCompleted?.Invoke();
        }

        public bool IsTooltipEnabled { get; set; } = true; // Default to true for the time slider

        private void UpdateFromPointer(PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(InputLayer).Position;
            double width = ControlRoot.ActualWidth;
            if (width <= 0) return;

            double percent = Math.Clamp(point.X / width, 0, 1);
            Value = Minimum + (percent * (Maximum - Minimum));

            // --- Conditional Tooltip Logic ---
            if (IsTooltipEnabled)
            {
                TimePopup.IsOpen = true;
                TimePopup.HorizontalOffset = point.X - (TimeLabel.ActualWidth / 2);
                TimeLabel.Text = FormatTime(Value);
            }
            else
            {
                TimePopup.IsOpen = false;
            }

            ValueChanged?.Invoke(Value);
        }

        private void InputLayer_PointerEntered(object sender, PointerRoutedEventArgs e) => VisualStateManager.GoToState(this, "PointerOver", true);
        private void InputLayer_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!isDragging)
            {
                TimePopup.IsOpen = false; // Hide tooltip
                VisualStateManager.GoToState(this, "Normal", true);
            }
        }
        #endregion

        #region Visual Engine

        private void ControlRoot_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateVisuals();

        private void UpdateVisuals()
        {
            if (ControlRoot.ActualWidth <= 0) return;

            double percent = (Maximum > Minimum) ? (Value - Minimum) / (Maximum - Minimum) : 0;
            double xPos = percent * ControlRoot.ActualWidth;

            // Update Progress Bar
            Progress.Width = xPos;

            // Center the thumb on the xPos
            Canvas.SetLeft(Thumb, xPos - (Thumb.ActualWidth / 2));

            // Ensure thumb vertical centering (Canvas.Top = (TotalHeight - ThumbHeight) / 2)
            Canvas.SetTop(Thumb, (ControlRoot.ActualHeight - Thumb.ActualHeight) / 2);
        }
        private string FormatTime(double seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return t.TotalHours >= 1
                ? t.ToString(@"hh\:mm\:ss")
                : t.ToString(@"mm\:ss");
        }
        public void SetValueFromPlayer(double value)
        {
            if (!isDragging) Value = value;
        }

        #endregion
    }

}
