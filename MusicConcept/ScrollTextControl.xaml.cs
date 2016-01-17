using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace MusicConcept
{
    public sealed partial class ScrollTextControl : UserControl
    {

        public string LongText
        {
            get { return (string)this.GetValue(LongTextProperty); }
            set { this.SetValue(LongTextProperty, value); }
        }

        public static readonly DependencyProperty LongTextProperty = DependencyProperty.Register("LongText",
            typeof(string), typeof(ScrollTextControl), new PropertyMetadata("", PropertyChangedCallback));

        public Style TextStyle
        {
            get { return (Style)this.GetValue(TextStyleProperty); }
            set { this.SetValue(TextStyleProperty, value); }
        }

        public static readonly DependencyProperty TextStyleProperty = DependencyProperty.Register("TextStyle",
            typeof(Style), typeof(ScrollTextControl), new PropertyMetadata("", PropertyChangedCallback));

        private Storyboard currentStoryboard;

        public ScrollTextControl()
        {
            this.InitializeComponent();

            Loaded += ScrollTextControl_Loaded;
        }

        void ScrollTextControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.UpdateTextAndAnimation();
        }

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scrollTextControl = (ScrollTextControl)d;
            scrollTextControl.UpdateTextAndAnimation();
        }



        private void UpdateTextAndAnimation()
        {
            this.TextBlock.Text = LongText;
            this.TextBlock.Style = TextStyle;

            if (currentStoryboard != null)
                currentStoryboard.Stop();

            this.TextBlock.Measure(new Size(1000, 100));
            var x = this.TextBlock.DesiredSize.Width - this.ActualWidth;
            this.Height = this.TextBlock.DesiredSize.Height;

            if (x < 10 || this.ActualWidth == 0)
                return;

            if (x < 50)
                x = 50;
            else
                x += 8;

            var easing = new QuadraticEase() { EasingMode = EasingMode.EaseInOut };
            var movementBegin = TimeSpan.FromMilliseconds(3000);
            var movementEnd = movementBegin + TimeSpan.FromMilliseconds(200 + (15 * x));
            var animation = new DoubleAnimationUsingKeyFrames();
            animation.KeyFrames.Add(new EasingDoubleKeyFrame()
            {
                KeyTime = TimeSpan.Zero,
                Value = 0,
                EasingFunction = easing
            });
            animation.KeyFrames.Add(new EasingDoubleKeyFrame()
            {
                KeyTime = movementBegin,
                Value = 0,
                EasingFunction = easing
            });
            animation.KeyFrames.Add(new EasingDoubleKeyFrame()
            {
                KeyTime = movementEnd,
                Value = -x,
                EasingFunction = easing
            });
            animation.KeyFrames.Add(new EasingDoubleKeyFrame()
            {
                KeyTime = movementEnd + TimeSpan.FromMilliseconds(400),
                Value = -x,
            });
            animation.KeyFrames.Add(new EasingDoubleKeyFrame()
            {
                KeyTime = movementEnd + TimeSpan.FromMilliseconds(401),
                Value = 0,
            });

            Storyboard.SetTarget(animation, this.TextBlockTranslateTransform);
            Storyboard.SetTargetProperty(animation, "X");

            var fadeAnimation = new DoubleAnimationUsingKeyFrames();
            fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame()
            {
                KeyTime = movementEnd,
                Value = 1
            });
            fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame()
            {
                KeyTime = movementEnd + TimeSpan.FromMilliseconds(400),
                Value = 0
            });
            fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame()
            {
                KeyTime = movementEnd + TimeSpan.FromMilliseconds(800),
                Value = 1
            });

            Storyboard.SetTarget(fadeAnimation, this.TextBlock);
            Storyboard.SetTargetProperty(fadeAnimation, "Opacity");

            var storyboard = new Storyboard();
           
            storyboard.RepeatBehavior = RepeatBehavior.Forever;
            storyboard.Children.Add(animation);
            storyboard.Children.Add(fadeAnimation);
            storyboard.Begin();
            currentStoryboard = storyboard;
        }

    }
}
