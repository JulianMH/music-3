using System;
using MusicConcept.Library;

namespace MusicConcept
{
    class CurrentSongChangedEventArgs : EventArgs
    {
        public SongChangedType Type { get; private set; }

        public CurrentSongChangedEventArgs(SongChangedType type)
        {
            this.Type = type;
        }
    }
}
