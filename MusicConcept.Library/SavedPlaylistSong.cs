using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicConcept.Library
{
    public class SavedPlaylistSong
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int SongId { get; set; }
        public int Rank { get; set; }
        public int PlaylistId { get; set; }

        public SavedPlaylistSong() { }
        public SavedPlaylistSong(int songId, int rank, int playlistId)
        {
            this.SongId = songId;
            this.Rank = rank;
            this.PlaylistId = playlistId;
        }
    }
}
