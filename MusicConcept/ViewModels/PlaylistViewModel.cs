using MusicConcept.Common;
using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Data;

namespace MusicConcept.ViewModels
{
    public class PlaylistViewModel : ObservableCollection<PlaylistSongViewModel>
    {
        private ListPlaylistManager playlistManager;
        private const int firstFewSongCount = 3;

        private ObservableCollection<PlaylistSongViewModel> _firstFewSongs;
        public ObservableCollection<PlaylistSongViewModel> FirstFewSongs { get { return _firstFewSongs; } private set { _firstFewSongs = value; OnPropertyChanged(new PropertyChangedEventArgs("FirstFewSongs")); } }

        public ICommand RemoveSongCommand { get; private set; }

        public PlaylistViewModel(ListPlaylistManager playlistManager)
        {
            this.playlistManager = playlistManager;
            this._firstFewSongs = new ObservableCollection<PlaylistSongViewModel>();
            this.RemoveSongCommand = new RelayCommandWithParameter(p =>
            {
                var song = (PlaylistSongViewModel)p;
                this.Remove(song);
                UpdateFirstFewSongs(playlistManager.GetCurrentSongIndex());
            });

            FirstReset();
        }

        private async void FirstReset()
        {
            await Reset();
        }

        protected async override void RemoveItem(int index)
        {
            base.RemoveItem(index);

            await playlistManager.Remove(index);

            Debug.WriteLine(string.Concat((await playlistManager.GetCompletePlaylist()).Select(p => p.Name + ", ")));
        }

        protected async override void InsertItem(int index, PlaylistSongViewModel song)
        {
            base.InsertItem(index, song);

            await playlistManager.AddSong(index, song.Song.Id, song.IsPlaying);

            if (song.IsPlaying)
            {
                UpdateSongsIsPlayedAroundCurrentSong();
            }
            UpdateFirstFewSongs(playlistManager.GetCurrentSongIndex());

            song.IsPlayed = index < playlistManager.GetCurrentSongIndex();

            Debug.WriteLine(string.Concat((await playlistManager.GetCompletePlaylist()).Select(p => p.Name + ", ")));
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            throw new NotImplementedException();
        }

        protected override void SetItem(int index, PlaylistSongViewModel item)
        {
            throw new NotImplementedException();
        }

        protected override void ClearItems()
        {
            throw new NotImplementedException();
        }

        public async void UpdateSongChanged(SongChangedType type, Song lastPlayedSong)
        {
            if (type == SongChangedType.NextSong || type == SongChangedType.PreviousSong || type == SongChangedType.SkipToSong)
            {
                int currentSongIndex = playlistManager.GetCurrentSongIndex();

                UpdateSongsIsPlayedAroundCurrentSong();

                var currentSong = this[currentSongIndex];
                currentSong.IsPlayed = false;
                currentSong.IsPlaying = true;

                UpdateFirstFewSongs(currentSongIndex);
            }
            else if (type != SongChangedType.None)
                await Reset();
        }

        private void UpdateSongsIsPlayedAroundCurrentSong()
        {
            int currentSongIndex = playlistManager.GetCurrentSongIndex();

            var index = currentSongIndex - 1;

            while (index >= 0 && (this[index].IsPlayed != true || this[index].IsPlaying != false))
            {
                this[index].IsPlayed = true;
                this[index].IsPlaying = false;
                index--;
            }

            index = currentSongIndex + 1;
            while (index < this.Count && (this[index].IsPlayed != false || this[index].IsPlaying != false))
            {
                this[index].IsPlayed = false;
                this[index].IsPlaying = false;
                index++;
            }
        }

        public PlaylistSongViewModel GetSongAfterCurrentSong()
        {
            return playlistManager.GetCurrentSongIndex() + 1 < this.Count ? this[playlistManager.GetCurrentSongIndex() + 1] : null;
        }

        private void UpdateFirstFewSongs(int currentSongIndex)
        {
            var actualFirstFewSongs = this.Skip(currentSongIndex + 1).Take(firstFewSongCount).ToArray();

            for (int i = 0; i < actualFirstFewSongs.Length; ++i)
            {
                if (_firstFewSongs.Count <= i)
                    _firstFewSongs.Add(actualFirstFewSongs[i]);
                else
                    if (_firstFewSongs[i] != actualFirstFewSongs[i])
                    {
                        if (_firstFewSongs.Count > i + 1 && _firstFewSongs[i + 1] == actualFirstFewSongs[i])
                            _firstFewSongs.RemoveAt(i);
                        else
                            _firstFewSongs.Insert(i, actualFirstFewSongs[i]);
                    }
            }

            while (_firstFewSongs.Count > actualFirstFewSongs.Length)
                _firstFewSongs.RemoveAt(actualFirstFewSongs.Length);
        }

        public async Task Reset()
        {
            base.ClearItems();
            _firstFewSongs.Clear();

            var playlist = await playlistManager.GetCompletePlaylist();
            Debug.WriteLine(playlist.Count());
            int currentSongIndex = playlistManager.GetCurrentSongIndex();

            int index = 0;
            foreach (var song in playlist)
            {
                base.InsertItem(index, new PlaylistSongViewModel(song, index < currentSongIndex, index == currentSongIndex));
                index++;
            }

            UpdateFirstFewSongs(currentSongIndex);
        }
    }
}
