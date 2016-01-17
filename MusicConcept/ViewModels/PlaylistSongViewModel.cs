using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicConcept.ViewModels
{
    public class PlaylistSongViewModel : NotifyPropertyChangedObject
    {
        public Song Song { get; private set; }

        private bool _isPlaying;
        public bool IsPlaying { get { return _isPlaying; } set { if (_isPlaying != value) { _isPlaying = value; NotifyPropertyChanged("IsPlaying"); } } }

        private bool _isPlayed;
        public bool IsPlayed { get { return _isPlayed; } set { if (_isPlayed != value) { _isPlayed = value; NotifyPropertyChanged("IsPlayed"); } } }

        public PlaylistSongViewModel(Song song, bool isPlayed, bool isPlaying)
        {
            this.Song = song;
            this._isPlayed = isPlayed;
            this._isPlaying = isPlaying;
        }
    }
}
