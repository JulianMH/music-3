using MusicConcept.Common;
using MusicConcept.Library;
using MusicConcept.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
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
    public sealed partial class NowPlayingPage : AppPage
    {
        private bool isInPlaylistView = false;
        private bool hasScrolledInPlaylist = false;

        public NowPlayingPage()
        {
            this.InitializeComponent();

            this.NavigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.NavigationHelper.SaveState += this.NavigationHelper_SaveState;

            ViewModel.Instance.SongChanged += (sender, e) =>
            {
                if (e != SongChangedType.None)
                {
                    Storyboard storyboard = (Storyboard)this.Resources[(e == SongChangedType.PreviousSong) ? "AnimatePreviousSongStoryboard" : "AnimateNextSongStoryboard"];
                    storyboard.Begin();
                }
            };
        }
        

        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            this.DataContext = ViewModel.Instance;
            ViewModel.Instance.PropertyChanged += ViewModel_PropertyChanged;

            ViewModel.Instance.SetUpdateNowPlayingProgress(true);
            Window.Current.VisibilityChanged += Window_VisibilityChanged;
        }

        double blockSliderChangeValue = -1;

        void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var viewModel = (ViewModel)sender;
            if (e.PropertyName == "NowPlayingProgress")
            {
                blockSliderChangeValue = viewModel.NowPlayingProgress;
                this.SeekSlider.Maximum = viewModel.NowPlayingLength;
                this.SeekSlider.Value = viewModel.NowPlayingProgress;
            }
            else if (e.PropertyName == "NowPlayingLength")
            {
                if (this.SeekSlider.Value >= viewModel.NowPlayingLength)
                    blockSliderChangeValue = viewModel.NowPlayingLength;
                this.SeekSlider.Maximum = viewModel.NowPlayingLength;
            }
            else if (e.PropertyName == "NowPlaying")
            {
                if (isInPlaylistView && !hasScrolledInPlaylist)
                    PlaylistScrollToCurrentSong();
            }
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (blockSliderChangeValue == e.NewValue || e.NewValue >= ViewModel.Instance.NowPlayingLength)
                blockSliderChangeValue = -1;
            else
                ViewModel.Instance.Seek(e.NewValue);
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            Window.Current.VisibilityChanged -= Window_VisibilityChanged;
            ViewModel.Instance.SetUpdateNowPlayingProgress(false);
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ViewModel.Instance.SetUpdateNowPlayingProgress(true);
            Window.Current.VisibilityChanged += Window_VisibilityChanged;

            if (isInPlaylistView)
            {
                PlaylistScrollToCurrentSong();
            }
        }

        void Window_VisibilityChanged(object sender, Windows.UI.Core.VisibilityChangedEventArgs e)
        {
            ViewModel.Instance.SetUpdateNowPlayingProgress(e.Visible);
        }

        private void PlaylistScrollToCurrentSong()
        {
            var playlistViewModel = ViewModel.Instance.Playlist;
            var song = playlistViewModel.GetSongAfterCurrentSong();

            if (song == null)
                song = playlistViewModel.FirstOrDefault();
            if (song != null)
                LargePlaylist.ScrollIntoView(song, ScrollIntoViewAlignment.Leading);

            hasScrolledInPlaylist = false;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Window.Current.VisibilityChanged -= Window_VisibilityChanged;
            ViewModel.Instance.SetUpdateNowPlayingProgress(false);
        }

        private async void AnimateToPlaylistView(object sender, PointerRoutedEventArgs e)
        {
            if (!isInPlaylistView)
            {
                isInPlaylistView = true;
                Storyboard storyboard = (Storyboard)this.Resources["SwitchViewStoryboard"];
                storyboard.AutoReverse = false;
                storyboard.Begin();
                await Task.Delay(100);
                
                var scrollViewer = LargePlaylist.GetFirstDescendantOfType<ScrollViewer>();
                scrollViewer.ViewChanged += (s, ev) => { if (ev.IsIntermediate) { hasScrolledInPlaylist = true; } };
            }
            PlaylistScrollToCurrentSong();
        }

        private async void AnimateToNowPlaying(object sender, PointerRoutedEventArgs e)
        {
            if (isInPlaylistView)
            {
                CommandBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                isInPlaylistView = false;

                await Task.Delay(50);

                Storyboard storyboard = (Storyboard)this.Resources["SwitchViewStoryboard"];

                storyboard.AutoReverse = true;
                storyboard.Begin();
                storyboard.Seek(storyboard.Duration.TimeSpan);
            }
        }

        private void SmallPlaylist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = ((ListView)sender);
            if (listView.SelectedItem != null)
            {
                AnimateToPlaylistView(sender, null);
                listView.SelectedItem = null;
            }
        }

        private void NowPlayingListViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            AnimateToNowPlaying(null, null);
        }

        private void PlaylistListViewItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            AnimateToPlaylistView(null, null);
        }

        private void SmallPlaylist_ItemClick(object sender, ItemClickEventArgs e)
        {
            AnimateToPlaylistView(null, null);
        }

        private void LargePlaylist_Holding(object sender, HoldingRoutedEventArgs e)
        {
            ((ListView)sender).ReorderMode = ListViewReorderMode.Enabled;
        }

        Storyboard currentStoryboard;
        bool isManipulationCanceled = false;

        private void LargeAlbumCover_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (currentStoryboard != null)
                currentStoryboard.Stop();

            isManipulationCanceled = false;
        }

        private void LargeAlbumCover_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            LargeAlbumCoverTranslateTransform.X += e.Delta.Translation.X;

            if (isManipulationCanceled)
                return;

            if (Math.Abs(e.Cumulative.Translation.X) > 250)
            {
                isManipulationCanceled = true;
                e.Complete();

                if (e.Cumulative.Translation.X > 0)
                    ViewModel.Instance.PreviousSongCommand.Execute(null);
                else
                    ViewModel.Instance.NextSongCommand.Execute(null);

                var duration = 500;

                var offset = duration * e.Velocities.Linear.X;
                EasingFunctionBase easing = null;
                if (offset < 100)
                {
                    offset = 100;

                }

                var animation = new DoubleAnimationUsingKeyFrames();
                animation.KeyFrames.Add(new EasingDoubleKeyFrame()
                {
                    KeyTime = TimeSpan.Zero,
                    Value = e.Cumulative.Translation.X,
                    EasingFunction = easing
                });
                animation.KeyFrames.Add(new EasingDoubleKeyFrame()
                {
                    KeyTime = TimeSpan.FromMilliseconds(duration),
                    Value = e.Cumulative.Translation.X + (duration * e.Velocities.Linear.X),
                    EasingFunction = easing
                });
                animation.KeyFrames.Add(new EasingDoubleKeyFrame()
                {
                    KeyTime = TimeSpan.FromMilliseconds(duration + 1),
                    Value = 0,
                    EasingFunction = easing
                });

                Storyboard.SetTarget(animation, this.LargeAlbumCoverTranslateTransform);
                Storyboard.SetTargetProperty(animation, "X");

                var fadeAnimation = new DoubleAnimationUsingKeyFrames();
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame()
                {
                    KeyTime = TimeSpan.FromMilliseconds(duration - 100),
                    Value = 1,
                });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame()
                {
                    KeyTime = TimeSpan.FromMilliseconds(duration),
                    Value = 0,
                });
                fadeAnimation.KeyFrames.Add(new EasingDoubleKeyFrame()
                {
                    KeyTime = TimeSpan.FromMilliseconds(duration * 2),
                    Value = 1,
                });

                Storyboard.SetTarget(fadeAnimation, this.LargeAlbumCover);
                Storyboard.SetTargetProperty(fadeAnimation, "Opacity");

                var storyboard = new Storyboard();
                storyboard.Children.Add(animation);
                storyboard.Children.Add(fadeAnimation);
                storyboard.Begin();
            }
        }

        private void LargeAlbumCover_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (!isManipulationCanceled)
            {
                var x = LargeAlbumCoverTranslateTransform.X;

                if (x == 0)
                    return;

                var easing = new QuadraticEase() { EasingMode = EasingMode.EaseOut };

                var movementEnd = TimeSpan.FromMilliseconds(200);
                var animation = new DoubleAnimationUsingKeyFrames();
                animation.KeyFrames.Add(new EasingDoubleKeyFrame()
                {
                    KeyTime = TimeSpan.Zero,
                    Value = x,
                    EasingFunction = easing
                });
                animation.KeyFrames.Add(new EasingDoubleKeyFrame()
                {
                    KeyTime = movementEnd,
                    Value = 0,
                    EasingFunction = easing
                });

                Storyboard.SetTarget(animation, this.LargeAlbumCoverTranslateTransform);
                Storyboard.SetTargetProperty(animation, "X");

                var storyboard = new Storyboard();

                storyboard.Children.Add(animation);
                storyboard.Begin();
                storyboard.Completed += (s, ev) => LargeAlbumCoverTranslateTransform.X = 0;
                currentStoryboard = storyboard;
            }
        }

        private void LargeAlbumCover_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 0.001;
        }

        private void LargePlaylist_ItemClick(object sender, ItemClickEventArgs e)
        {
            if(((ListView)sender).ReorderMode != ListViewReorderMode.Enabled)
                ViewModel.Instance.GoToPlaylistSong(ViewModel.Instance.Playlist.IndexOf((PlaylistSongViewModel)e.ClickedItem));
        }
    }
}
