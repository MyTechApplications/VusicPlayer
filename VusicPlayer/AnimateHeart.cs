using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VusicPlayer
{
    public class AnimateHeart
    {
        public static void AnimateHeartIcon(FontIcon target, double targetOpacity, double targetScale)
        {
            var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();

            // Scale X Animation
            var scaleXAnim = new DoubleAnimation { To = targetScale, Duration = TimeSpan.FromMilliseconds(200) };
            Storyboard.SetTarget(scaleXAnim, target.RenderTransform);
            Storyboard.SetTargetProperty(scaleXAnim, "ScaleX");

            // Scale Y Animation
            var scaleYAnim = new DoubleAnimation { To = targetScale, Duration = TimeSpan.FromMilliseconds(200) };
            Storyboard.SetTarget(scaleYAnim, target.RenderTransform);
            Storyboard.SetTargetProperty(scaleYAnim, "ScaleY");

            // Opacity Animation
            var opacityAnim = new DoubleAnimation { To = targetOpacity, Duration = TimeSpan.FromMilliseconds(150) };
            Storyboard.SetTarget(opacityAnim, target);
            Storyboard.SetTargetProperty(opacityAnim, "Opacity");

            storyboard.Children.Add(scaleXAnim);
            storyboard.Children.Add(scaleYAnim);
            storyboard.Children.Add(opacityAnim);

            storyboard.Begin();
        }

    }
}
