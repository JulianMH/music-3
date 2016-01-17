using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicConcept.Library
{
    public class CurrentPlaylistSong
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int SongId { get; set; }
        public int Rank { get; set; }
        public int ActualRank { get; set; }

        public CurrentPlaylistSong() { }
        public CurrentPlaylistSong(int songId, int rank, int actualRank)
        {
            this.SongId = songId;
            this.Rank = rank;
            this.ActualRank = actualRank;
        }
    }
}
