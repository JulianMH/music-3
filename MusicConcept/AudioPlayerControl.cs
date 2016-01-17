using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI.Core;

namespace MusicConcept
{
    internal sealed class AudioPlayerControl : NotifyPropertyChangedObject
    {
        private bool isBackgroundTaskRunning = false;
        private AutoResetEvent isBackgroundTaskRunningEvent = new AutoResetEvent(false);
        CoreDispatcher dispatcher;

        public bool IsPlaying { get; private set; }

        private async Task CheckBackgroundTaskStarted()
        {
            if (!isBackgroundTaskRunning)
            {
                SubscribeEvents();

                var source = new TaskCompletionSource<object>();
                var result = this.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    if (!isBackgroundTaskRunningEvent.WaitOne(2000))
                        throw new Exception("Background Audio Task didn't start in expected time");
                });

                result.Completed = new AsyncActionCompletedHandler((action, status) => { source.SetResult(null); });
                await source.Task;
            }
        }

        internal AudioPlayerControl(CoreDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
            BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
            SendToBackground(CommunicationConstants.GetInfo);
            BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
        }

        private void SubscribeEvents()
        {
            BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
            SendToBackground(CommunicationConstants.GetInfo);
            BackgroundMediaPlayer.Current.CurrentStateChanged += BackgroundMediaPlayer_CurrentStateChanged;
        }

        async void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            //Avoids flashing of the IsPlaying Property while loading.
            await Task.Delay(300);
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var state = sender.CurrentState;
                    if (sender.CurrentState == MediaPlayerState.Playing)
                        this.IsPlaying = true;
                    else
                        this.IsPlaying = false;
                    NotifyPropertyChanged("IsPlaying");
                });
        }

        async void BackgroundMediaPlayer_MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (var item in e.Data)
            {
                Debug.WriteLine("received: " + item);
                switch (item.Key)
                {
                    case CommunicationConstants.PlaybackNewSong:
                        await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            if (CurrentSongChanged != null)
                                CurrentSongChanged(this, new CurrentSongChangedEventArgs(
                                    (SongChangedType)Enum.Parse(typeof(SongChangedType), (string)item.Value)));
                        });
                        break;
                    case CommunicationConstants.BackgroundTaskStarted:
                        isBackgroundTaskRunningEvent.Set();
                        isBackgroundTaskRunning = true;
                        if (item.Value != null && ((string)item.Value) != "")
                            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                            {
                                if (CurrentSongChanged != null)
                                    CurrentSongChanged(this, new CurrentSongChangedEventArgs(SongChangedType.None));
                            });
                        break;
                    case CommunicationConstants.BackgroundTaskStopped:
                        isBackgroundTaskRunningEvent.Reset();
                        isBackgroundTaskRunning = false;
                        await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            this.IsPlaying = false;
                            NotifyPropertyChanged("IsPlaying");
                        });
                        break;
                    case null:
                        Debug.WriteLine("WTF");
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private void SendToBackground(string command)
        {
            Debug.WriteLine("Message to background: " + command);
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet() { { command, "" } });
        }

        private void SendToBackground(string command, string parameter)
        {
            Debug.WriteLine("Message to background: " + command + " Parameter: " + parameter);
            BackgroundMediaPlayer.SendMessageToBackground(new ValueSet() { { command, parameter } });
        }

        internal async void Play()
        {
            await CheckBackgroundTaskStarted();
            SendToBackground(CommunicationConstants.PlaybackNewSong, SongChangedType.NewPlayback.ToString());
        }

        internal async void GoToSongIndex(int index)
        {
            ApplicationSettings.PlaylistManagerCurrentIndex.Save(index);
            await CheckBackgroundTaskStarted();
            SendToBackground(CommunicationConstants.PlaybackNewSong, SongChangedType.SkipToSong.ToString());
        }

        internal async void NextSong()
        {
            await CheckBackgroundTaskStarted();
            SendToBackground(CommunicationConstants.PlaybackNewSong, SongChangedType.NextSong.ToString());
        }

        internal async void PreviousSong()
        {
            await CheckBackgroundTaskStarted();
            SendToBackground(CommunicationConstants.PlaybackNewSong, SongChangedType.PreviousSong.ToString());
        }

        internal void Seek(double seconds)
        {
            Debug.WriteLine("Seek: " + seconds);
            if (ApplicationSettings.BackgroundTaskResumeSongTime.Read() != ApplicationSettings.BackgroundTaskResumeSongTimeNone)
                ApplicationSettings.BackgroundTaskResumeSongTime.Save(seconds);
            else if (isBackgroundTaskRunning)
                BackgroundMediaPlayer.Current.Position = TimeSpan.FromSeconds(seconds);
        }

        internal async void TogglePlayPause()
        {
            await CheckBackgroundTaskStarted();
            SendToBackground(CommunicationConstants.TogglePlayPause);
        }

        internal double GetNowPlayingProgress()
        {
            var value = ApplicationSettings.BackgroundTaskResumeSongTime.Read();

            if (value == ApplicationSettings.BackgroundTaskResumeSongTimeNone)
                value = BackgroundMediaPlayer.Current.Position.TotalSeconds;


            return value;
        }

        internal double GetNowPlayingLength()
        {
            return ApplicationSettings.CurrentSongLength.Read();
        }

        internal event EventHandler<CurrentSongChangedEventArgs> CurrentSongChanged;


    }
}
