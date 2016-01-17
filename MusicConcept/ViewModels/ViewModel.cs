using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MusicConcept.Library;
using Windows.UI.Xaml;
using System.Windows.Input;
using MusicConcept.Common;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
using System.Threading;
using MusicConcept.ViewModels.MusicFilters;
using System.Collections.ObjectModel;
using MusicConcept.Library.LibrarySource;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Storage;

namespace MusicConcept.ViewModels
{
    public class ViewModel : NotifyPropertyChangedObject
    {
        private MusicLibrary library;
        private Task<MusicLibrary> libraryLoadingTask;

        private AudioPlayerControl audioControls;

        private Song _animationPreviousNowPlaying;
        public Song AnimationPreviousNowPlaying { get { return _animationPreviousNowPlaying; } set { _animationPreviousNowPlaying = value; NotifyPropertyChanged("AnimationPreviousNowPlaying"); } }
        private Song _nowPlaying;
        public Song NowPlaying { get { return _nowPlaying; } set { this.AnimationPreviousNowPlaying = _nowPlaying; _nowPlaying = value; NotifyPropertyChanged("NowPlaying"); NotifyPropertyChanged("IsNowPlayingLoaded"); } }

        public bool IsNowPlayingLoaded { get { return _nowPlaying != null; } }

        private bool _isCreatingLibrary;
        public bool IsCreatingLibrary { get { return _isCreatingLibrary; } private set { _isCreatingLibrary = value; NotifyPropertyChanged("IsCreatingLibrary"); } }

        private double _nowPlayingLength;
        public double NowPlayingLength { get { return _nowPlayingLength; } private set { _nowPlayingLength = value; NotifyPropertyChanged("NowPlayingLength"); } }
        private double _nowPlayingProgress;
        public double NowPlayingProgress { get { return _nowPlayingProgress; } private set { _nowPlayingProgress = value; NotifyPropertyChanged("NowPlayingProgress"); } }

        public bool IsPlaying { get { return audioControls.IsPlaying; } }

        public PlaylistViewModel Playlist { get; private set; }

        public ICommand NextSongCommand { get; private set; }
        public ICommand PreviousSongCommand { get; private set; }
        public ICommand TogglePlayPauseCommand { get; private set; }
        public ICommand ClearPlaylistCommand { get; private set; }

        private ObservableCollection<FilterViewModel> _selectedFilters;
        public IEnumerable<FilterViewModel> SelectedFilters { get { return _selectedFilters; } }
        public bool SelectedFiltersEmpty { get { return !_selectedFilters.Any(); } }
        public ReadOnlyCollection<FilterViewModel> Filters { get; private set; }

        private bool _backgroundImageLoaded;
        private ImageSource _backgroundImage;
        public ImageSource BackgroundImage { get { if (!_backgroundImageLoaded) { _backgroundImageLoaded = true; UpdateWallpaper(); } return _backgroundImage; } set { _backgroundImage = value; NotifyPropertyChanged("BackgroundImage"); NotifyPropertyChanged("IsBackgroundImageSet"); } }

        public bool IsBackgroundImageSet { get { if (!_backgroundImageLoaded) { _backgroundImageLoaded = true; UpdateWallpaper(); } return _backgroundImage != null; } }

        public bool IsRandomOrder
        {
            get { return library == null ? false : library.PlaylistManager == null ? false : library.PlaylistManager.IsRandomOrder; }
            set { SetIsRandomOrder(value); }
        }

        private async void SetIsRandomOrder(bool value)
        {
            if (library.PlaylistManager != null)
            {
                await library.PlaylistManager.SetIsRandomOrder(value, this.NowPlaying == null ? null : (int?)this.NowPlaying.Id);
                await Playlist.Reset();
            }
        }

        public bool IsLoopingOne
        {
            get { return library == null ? false : library.PlaylistManager == null ? false : library.PlaylistManager.RepeatMode == SongRepeatMode.One; }
            set
            {
                if (library.PlaylistManager != null)
                {
                    library.PlaylistManager.RepeatMode = value ? SongRepeatMode.One : SongRepeatMode.None;
                    NotifyPropertyChanged("IsLoopingOne");
                    NotifyPropertyChanged("IsLoopingAll");
                }
            }
        }

        public bool IsLoopingAll
        {
            get { return library == null ? false : library.PlaylistManager == null ? false : library.PlaylistManager.RepeatMode == SongRepeatMode.All; }
            set
            {
                if (library.PlaylistManager != null)
                {
                    library.PlaylistManager.RepeatMode = value ? SongRepeatMode.All : SongRepeatMode.One;
                    NotifyPropertyChanged("IsLoopingOne");
                    NotifyPropertyChanged("IsLoopingAll");
                }
            }
        }

        private Album _nowPlayingAlbum;
        public Album NowPlayingAlbum { get { return _nowPlayingAlbum; } set { _nowPlayingAlbum = value; NotifyPropertyChanged("NowPlayingAlbum"); } }

        private CoreDispatcher dispatcher;

        private ViewModel()
        {
            var resourceLoader = ResourceLoader.GetForViewIndependentUse();
            this.Filters = new ReadOnlyCollection<FilterViewModel>(new List<FilterViewModel>() { new AllSongsFilter(resourceLoader, null, SongSource.AllSongsSource()), new AlbumsFilter(resourceLoader), new ArtistsFilter(resourceLoader), new SearchFilter(resourceLoader), new PlaylistsFilter(resourceLoader), new GenresFilter(resourceLoader) });

            var enabledFilters = ApplicationSettings.CurrentlyEnabledMusicFilters.Read();

            foreach (var filter in this.Filters)
                filter.IsSelected = enabledFilters.Contains(filter.GetType().Name);

            this._selectedFilters = new ObservableCollection<FilterViewModel>(this.Filters.Where(p => p.IsSelected));


            foreach (var filter in this.Filters)
                filter.PropertyChanged += filter_PropertyChanged;

            this.libraryLoadingTask = MusicLibrary.Open(new LocalLibrarySource(), this.Filters.Select(p => new PropertyChangedEventHandler(p.MusicLibrary_PropertyChanged))
                .Concat(new PropertyChangedEventHandler[] { library_PropertyChanged }).ToArray());

            this.dispatcher = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;
            this.LoadLibrary();
            this.audioControls = new AudioPlayerControl(dispatcher);
            this.NowPlayingProgress = this.audioControls.GetNowPlayingProgress();
            this.NowPlayingLength = this.audioControls.GetNowPlayingLength();

            this.NextSongCommand = new RelayCommand(this.audioControls.NextSong);
            this.PreviousSongCommand = new RelayCommand(this.audioControls.PreviousSong);
            this.TogglePlayPauseCommand = new RelayCommand(this.audioControls.TogglePlayPause);
            this.ClearPlaylistCommand = new RelayCommand(async () => { await this.library.PlaylistManager.Clear(); await this.Playlist.Reset(); });
        }

        void filter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsSelected")
            {
                var filter = (FilterViewModel)sender;

                if (filter.IsSelected && !this._selectedFilters.Contains(filter))
                    this._selectedFilters.Add(filter);
                else if (!filter.IsSelected && this._selectedFilters.Contains(filter))
                    this._selectedFilters.Remove(filter);

                NotifyPropertyChanged("SelectedFiltersEmpty");

                ApplicationSettings.CurrentlyEnabledMusicFilters.Save(string.Join(", ", _selectedFilters.Select(p => p.GetType().Name)));
            }
        }

        private async void LoadLibrary()
        {
            this.library = await libraryLoadingTask;

            this.audioControls.CurrentSongChanged += audioControls_CurrentSongChanged;
            this.audioControls.PropertyChanged += audioControls_PropertyChanged;

            audioControls_CurrentSongChanged(this, new CurrentSongChangedEventArgs(SongChangedType.None));

            if (ApplicationSettings.IsDatabaseSettingUp.Read())
            {
                this.IsCreatingLibrary = true;
                var progressBar = StatusBar.GetForCurrentView().ProgressIndicator;
                progressBar.ProgressValue = null;
                progressBar.Text = ResourceLoader.GetForViewIndependentUse().GetString("DatabaseSettingUpString");

                var showTask = progressBar.ShowAsync();
                await library.UpdateLibrary();

                await showTask;
                await progressBar.HideAsync();
                this.IsCreatingLibrary = false;
            }
            else
                await library.UpdateLibrary();
        }

        public event EventHandler<SongChangedType> SongChanged;

        async void audioControls_CurrentSongChanged(object sender, CurrentSongChangedEventArgs e)
        {
            var library = this.library ?? await libraryLoadingTask;

            var currentSongFileName = ApplicationSettings.CurrentSongFileName.Read();

            if (currentSongFileName == null)
            {
                this.NowPlaying = null;
            }
            else
            {
                var song = library.Songs.FirstOrDefault(p => p.FileName == currentSongFileName);
                this.NowPlaying = song;
                this.Playlist.UpdateSongChanged(e.Type, this.NowPlaying);
                if (SongChanged != null)
                    SongChanged(this, e.Type);

                if (this.NowPlayingAlbum == null || !this.NowPlayingAlbum.Contains(song))
                    this.NowPlayingAlbum = library.Albums.FirstOrDefault(p => p.Contains(song));
            }
        }

        void audioControls_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsPlaying")
                NotifyPropertyChanged("IsPlaying");
        }

        void library_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            MusicLibrary library = (MusicLibrary)sender;
            if (e.PropertyName == "PlaylistManager")
            {
                this.Playlist = new PlaylistViewModel(library.PlaylistManager);
                NotifyPropertyChanged("Playlist");
                library.PlaylistManager.PropertyChanged += PlaylistManager_PropertyChanged;
            }
        }

        async void PlaylistManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (e.PropertyName == "IsRandomOrder")
                    NotifyPropertyChanged("IsRandomOrder");
                if (e.PropertyName == "RepeatMode")
                    NotifyPropertyChanged("IsLooping");
            });
        }

        DispatcherTimer timer;
        public void SetUpdateNowPlayingProgress(bool update)
        {
            if (update)
            {
                if (timer == null)
                {
                    timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromMilliseconds(1000 / 16.0);
                    timer.Tick += (sender, e) =>
                    {
                        this.NowPlayingProgress = this.audioControls.GetNowPlayingProgress();
                        this.NowPlayingLength = this.audioControls.GetNowPlayingLength();
                    };
                }
                if (!timer.IsEnabled)
                    timer.Start();
            }
            else if (timer != null && timer.IsEnabled)
                timer.Stop();
        }

        private static ViewModel instance;
        public static ViewModel Instance { get { return instance ?? (instance = new ViewModel()); } }

        internal async void GoToPlayMode(Song song, SongSource songSource)
        {
            var library = this.library ?? await libraryLoadingTask;
            if (await library.PlaylistManager.PlayFromSource(songSource, song.Id))
                this.audioControls.Play();
        }

        internal void Seek(double seconds)
        {
            Debug.WriteLine("Seek: " + seconds);
            audioControls.Seek(seconds);
        }

        internal async Task<ArtistViewModel> GetArtistViewModel(string artist)
        {
            var library = this.library ?? await libraryLoadingTask;

            return new ArtistViewModel(library, artist);
        }

        internal async Task<AlbumViewModel> GetAlbumViewModel(string artistAndAlbum)
        {
            var library = this.library ?? await libraryLoadingTask;

            var album = library.Albums.FirstOrDefault(p => p.ArtistAndAlbum == artistAndAlbum);
            if (album == null)
                return null;
            return new AlbumViewModel(library, album);
        }

        internal async void AddSongsToPlaylist(SongSource songSource)
        {
            await library.PlaylistManager.AddToEnd(songSource);
            await Playlist.Reset();
        }

        internal async void AddToPlayNext(SongSource songSource)
        {
            await library.PlaylistManager.AddNext(songSource);
            await Playlist.Reset();
        }

        internal async Task<object> GetSaveNewPlaylistViewModel()
        {
            var library = this.library ?? await libraryLoadingTask;

            return new SaveNewPlaylistViewModel(library);
        }

        internal async void PlayPlaylist(SavedPlaylist savedPlaylist)
        {
            var library = this.library ?? await libraryLoadingTask;

            if (await library.PlaylistManager.LoadFromLibrary(savedPlaylist))
            {
                this.audioControls.Play();
                await this.Playlist.Reset();
            }
        }

        internal async Task DeleteSavedPlaylist(SavedPlaylist savedPlaylist)
        {
            var library = this.library ?? await libraryLoadingTask;

            await library.PlaylistManager.DeleteFromLibrary(library, savedPlaylist);
        }

        internal async Task<GenreViewModel> GetGenreViewModel(string genreName)
        {
            var library = this.library ?? await libraryLoadingTask;

            var genre = library.Genres.FirstOrDefault(p => string.Equals(genreName, p.Name, StringComparison.OrdinalIgnoreCase));
            if (genre == null)
                return null;
            return new GenreViewModel(library, genre);
        }

        internal async Task<AlbumCoverFetchViewModel> GetAlbumCoverFetchViewModel(string artistAndAlbum)
        {
            var library = this.library ?? await libraryLoadingTask;

            var album = library.Albums.FirstOrDefault(p => p.ArtistAndAlbum == artistAndAlbum);
            if (album == null)
                return null;
            return new AlbumCoverFetchViewModel(album);
        }

        internal async void ReloadFilters()
        {
            var library = this.library ?? await libraryLoadingTask;

            foreach (var filter in ViewModel.Instance.Filters)
                filter.LoadData(library);
        }

        public async void UpdateWallpaper()
        {
            try
            {
                var file = (await ApplicationData.Current.LocalFolder.GetFilesAsync()).FirstOrDefault(p => p.Name == "Wallpaper");
                if(file == null)
                    return;
                var bitmapImage = new BitmapImage();
                using (IRandomAccessStream stream = await file.OpenReadAsync())
                    await bitmapImage.SetSourceAsync(stream);
                this.BackgroundImage = bitmapImage;
            }
            catch
            {
                this.BackgroundImage = null;
            }
        }

        public void GoToPlaylistSong(int index)
        {
            audioControls.GoToSongIndex(index);
        }
    }
}
