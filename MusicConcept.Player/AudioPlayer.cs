using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;

namespace MusicConcept.Player
{
    public sealed class AudioPlayer : IBackgroundTask
    {
        private SystemMediaTransportControls mediaControls;
        private ListPlaylistManager playlistManager;

        private AutoResetEvent isRunningEvent = new AutoResetEvent(false);
        //   private bool isRunning;

        private string nowPlayingFileName = null;

        private async void LoadPlaylistManager()
        {
            this.playlistManager = new ListPlaylistManager(await MusicLibrary.ConnectToDatabase());
        }

        private BackgroundTaskDeferral _deferral;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            LoadPlaylistManager();

            this.mediaControls = SystemMediaTransportControls.GetForCurrentView();
            this.mediaControls.IsEnabled = true;
            this.mediaControls.IsPlayEnabled = true;
            this.mediaControls.IsPauseEnabled = true;
            this.mediaControls.IsNextEnabled = true;
            this.mediaControls.IsPreviousEnabled = true;

            this.mediaControls.ButtonPressed += mediaControls_ButtonPressed;
            BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayer_MessageReceivedFromForeground;
            BackgroundMediaPlayer.Current.MediaEnded += Current_MediaEnded;
            BackgroundMediaPlayer.Current.MediaFailed += Current_MediaFailed;
            BackgroundMediaPlayer.Current.MediaOpened += Current_MediaOpened;
            BackgroundMediaPlayer.Current.AutoPlay = false;
            taskInstance.Canceled += TaskInstance_Canceled;

            this.SendToForeground(CommunicationConstants.BackgroundTaskStarted);
            _deferral = taskInstance.GetDeferral();
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            this.SendToForeground(CommunicationConstants.BackgroundTaskStopped);

            try
            {
                ApplicationSettings.BackgroundTaskResumeSongTime.Save(BackgroundMediaPlayer.Current.Position.TotalSeconds);
                /*
                //save state
                ApplicationSettingsHelper.SaveSettingsValue(Constants.CurrentTrack, Playlist.CurrentTrackName);
                ApplicationSettingsHelper.SaveSettingsValue(Constants.Position, BackgroundMediaPlayer.Current.Position.ToString());
                ApplicationSettingsHelper.SaveSettingsValue(Constants.BackgroundTaskState, Constants.BackgroundTaskCancelled);
                ApplicationSettingsHelper.SaveSettingsValue(Constants.AppState, Enum.GetName(typeof(ForegroundAppStatus), foregroundAppState));
                backgroundtaskrunning = false;
                //unsubscribe event handlers
                systemmediatransportcontrol.ButtonPressed -= systemmediatransportcontrol_ButtonPressed;
                systemmediatransportcontrol.PropertyChanged -= systemmediatransportcontrol_PropertyChanged;
                Playlist.TrackChanged -= playList_TrackChanged;

                //clear objects task cancellation can happen uninterrupted
                playlistManager.ClearPlaylist();
                playlistManager = null;
                 */
                this.mediaControls.ButtonPressed -= mediaControls_ButtonPressed;
                playlistManager = null;

                BackgroundMediaPlayer.Shutdown(); // shutdown media pipeline
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            if (_deferral != null)
                _deferral.Complete();
        }


        void Current_MediaOpened(MediaPlayer sender, object args)
        {
            var resumeProgress = ApplicationSettings.BackgroundTaskResumeSongTime.ReadReset();
            if (resumeProgress != ApplicationSettings.BackgroundTaskResumeSongTimeNone)
                BackgroundMediaPlayer.Current.Position = TimeSpan.FromSeconds(resumeProgress);

            if (!BackgroundMediaPlayer.Current.AutoPlay)
                ApplicationSettings.BackgroundTaskResumeSongTime.Save(BackgroundMediaPlayer.Current.Position.TotalSeconds);

            ApplicationSettings.CurrentSongLength.Save(BackgroundMediaPlayer.Current.NaturalDuration.TotalSeconds);
            UpdateTile(BackgroundMediaPlayer.Current.AutoPlay);
        }

        async void Current_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            ApplicationSettings.BackgroundTaskResumeSongTime.ReadReset();
            ApplicationSettings.CurrentSongLength.ReadReset();

            Debug.WriteLine(args.ToString());
            UpdateTile(false);
            await PlayNextSong(false, BackgroundMediaPlayer.Current.AutoPlay, SongChangedType.NextSong);
        }

        async void Current_MediaEnded(MediaPlayer sender, object args)
        {
            await PlayNextSong(true, false, SongChangedType.NextSong);
            UpdateTile(false);
        }

        async void mediaControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    await this.Resume();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    this.Pause();
                    break;
                case SystemMediaTransportControlsButton.Next:
                    ApplicationSettings.BackgroundTaskResumeSongTime.ReadReset();
                    await PlayNextSong(false, true, SongChangedType.NextSong);
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    ApplicationSettings.BackgroundTaskResumeSongTime.ReadReset();
                    await PlayPreviousSong();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private async Task Resume()
        {
            if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Paused)
            {
                BackgroundMediaPlayer.Current.Position = TimeSpan.FromSeconds(ApplicationSettings.BackgroundTaskResumeSongTime.ReadReset());
                BackgroundMediaPlayer.Current.Play();
                UpdateTile(true);
            }
            else
            {
                var resumeFileName = ApplicationSettings.CurrentSongFileName.Read();

                if (resumeFileName != null)
                {
                    await this.PlayFile(resumeFileName, SongChangedType.None, () => true);
                }
            }
        }

        private void Pause()
        {
            try
            {
                BackgroundMediaPlayer.Current.Pause();
                ApplicationSettings.BackgroundTaskResumeSongTime.Save(BackgroundMediaPlayer.Current.Position.TotalSeconds);
                UpdateTile(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private async void UpdateMediaControls(StorageFile playingFile)
        {
            MusicProperties musicProperties = await playingFile.Properties.GetMusicPropertiesAsync();

            this.mediaControls.PlaybackStatus = BackgroundMediaPlayer.Current.AutoPlay ? MediaPlaybackStatus.Playing : MediaPlaybackStatus.Paused;
            this.mediaControls.DisplayUpdater.Type = MediaPlaybackType.Music;
            this.mediaControls.DisplayUpdater.MusicProperties.Title = musicProperties.Title;
            this.mediaControls.DisplayUpdater.MusicProperties.Artist = musicProperties.Artist;
            this.mediaControls.DisplayUpdater.MusicProperties.AlbumArtist = musicProperties.AlbumArtist;

            this.mediaControls.DisplayUpdater.Update();
        }

        private async void UpdateTile(bool isPlaying)
        {
            var coverFileName = isPlaying ? await playlistManager.GetCurrentAlbumCover() : null;

            var updater = TileUpdateManager.CreateTileUpdaterForApplication("App");
            if (coverFileName != null)
            {
                var xml = "<tile><visual version=\"2\"><binding template=\"TileSquare150x150Image\" fallback=\"TileSquareImage\"><image id=\"1\" src=\"{0}\"/></binding><binding template=\"TileSquare71x71Image\" fallback=\"TileSquareImage\"><image id=\"1\" src=\"{1}\"/></binding></visual></tile>";
                XmlDocument tileXml = new XmlDocument();
                tileXml.LoadXml(string.Format(xml, coverFileName ?? "ms-appx:///Assets/Logo.scale-240.png", coverFileName ?? "ms-appx:///Assets/SmallLogo.scale-240.png"));
                updater.Update(new TileNotification(tileXml));
            }
            else
                updater.Clear();
        }

        async void BackgroundMediaPlayer_MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (var item in e.Data)
            {
                switch (item.Key)
                {
                    case CommunicationConstants.TogglePlayPause:
                        if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing)
                            this.Pause();
                        else
                            await this.Resume();
                        break;
                    case CommunicationConstants.GetInfo:
                        SendToForeground(CommunicationConstants.BackgroundTaskStarted);
                        break;
                    case CommunicationConstants.PlaybackNewSong:
                        var type = (SongChangedType)Enum.Parse(typeof(SongChangedType), (string)item.Value);
                        switch (type)
                        {
                            case SongChangedType.NewPlayback:
                            case SongChangedType.SkipToSong:
                                ApplicationSettings.BackgroundTaskResumeSongTime.ReadReset();
                                await PlayCurrentSong(true, type);
                                break;
                            case SongChangedType.NextSong:
                                ApplicationSettings.BackgroundTaskResumeSongTime.ReadReset();
                                await PlayNextSong(false, true, type);
                                break;
                            case SongChangedType.PreviousSong:
                                ApplicationSettings.BackgroundTaskResumeSongTime.ReadReset();
                                await PlayPreviousSong();
                                break;
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private void SendToForeground(string command)
        {
            Debug.WriteLine("Message to foreground: " + command);
            BackgroundMediaPlayer.SendMessageToForeground(new ValueSet() { { command, "" } });
        }

        private void SendToForeground(string command, string parameter)
        {
            Debug.WriteLine("Message to foreground: " + command + " Parameter: " + parameter);
            BackgroundMediaPlayer.SendMessageToForeground(new ValueSet() { { command, parameter } });
        }

        private async Task PlayFile(string fileName, SongChangedType changedType, Func<bool> newAutoPlayValue)
        {
            if (fileName == null)
            {
                BackgroundMediaPlayer.Current.Pause();
                BackgroundMediaPlayer.Current.Position = TimeSpan.Zero;
                ApplicationSettings.CurrentSongFileName.Save(null);

                fileName = await playlistManager.GetCurrentFileToPlay();
                newAutoPlayValue = () => false;
            }

            if (fileName != null)
            {
                ApplicationSettings.CurrentSongFileName.Save(fileName);
                this.SendToForeground(CommunicationConstants.PlaybackNewSong, changedType.ToString());

                var file = await StorageFile.GetFileFromPathAsync(fileName);
                this.nowPlayingFileName = fileName;

                var autoplay = newAutoPlayValue();
                BackgroundMediaPlayer.Current.AutoPlay = autoplay;
                BackgroundMediaPlayer.Current.SetFileSource(file);
                UpdateMediaControls(file);
            }
        }

        private async Task PlayNextSong(bool forceAutoplay, bool forceNext, SongChangedType songChangedType)
        {
            if (playlistManager == null)
                return;

            await this.PlayFile(
                await playlistManager.GetNextFileToPlay(forceNext),
                songChangedType,
                () => forceAutoplay || BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Paused);
        }

        private async Task PlayCurrentSong(bool forceAutoplay, SongChangedType songChangedType)
        {
            if (playlistManager == null)
                return;

            await this.PlayFile(
                await playlistManager.GetCurrentFileToPlay(),
                songChangedType,
                () => forceAutoplay || BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Paused);
        }

        private async Task PlayPreviousSong()
        {
            if (playlistManager == null)
                return;

            await this.PlayFile(
                await playlistManager.GetPreviousFileToPlay(),
                SongChangedType.PreviousSong,
                () => BackgroundMediaPlayer.Current.CurrentState != MediaPlayerState.Paused);
        }
    }
}
