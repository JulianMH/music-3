using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicConcept.Library
{
    public sealed class SongSource
    {
        public string Sql { get; private set; }
        public object[] SqlArguments { get; private set; }

        private SongSource(string sql, params object[] args)
        {
            this.Sql = sql;
            this.SqlArguments = args;
        }

        public static SongSource ArtistSource(string artist)
        {
            return new SongSource("from Song where LOWER(AlbumArtist) = LOWER(?) order by SortingLetter, Name", artist);
        }

        public static SongSource AllSongsSource()
        {
            return new SongSource("from Song order by SortingLetter, Name");
        }

        public static SongSource AlbumSource(string artist, string album)
        {
            return new SongSource("from Song where LOWER(AlbumArtist) = LOWER(?) and LOWER(Album) = LOWER(?) order by AlbumCDNumber, AlbumTrackNumber",
                artist, album);
        }

        public static SongSource GenreSource(int genreId)
        {
            return new SongSource("from Song song, SongGenre songGenre where song.Id = songGenre.SongId and songGenre.GenreId = ?", genreId);
        }

        public static SongSource OneSongSource(int songId)
        {
            return new SongSource("from Song where Id = ?", songId);
        }
    }
}
