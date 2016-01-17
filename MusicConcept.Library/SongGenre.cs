using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicConcept.Library
{
    public class SongGenre
    {
        [PrimaryKey, AutoIncrement]
        public int SongGenreId { get; set; }

        [Indexed]
        public int SongId { get; set; }

        [Indexed]
        public int GenreId { get; set; }

        public SongGenre(int songId, int genreId)
        {
            this.SongId = songId;
            this.GenreId = genreId;
        }

        public SongGenre()
        {

        }
    }
}
