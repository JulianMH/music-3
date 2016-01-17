using System;

namespace MusicConcept.Library
{
    public enum SongChangedType
    {
        NextSong,
        PreviousSong,
        NewPlayback,
        SkipToSong,

        /// <summary>
        /// No song change happend. Just making sure the information is up to date.
        /// </summary>
        None
    }
}
