using SQLite;
using System;

namespace MusicConcept.Library
{
    public class Song
    {
        public static string UnknownArtist = "unknown artist";

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Album { get; set; }
        public uint AlbumTrackNumber { get; set; }
        public uint AlbumCDNumber { get; set; }

        private string _artist;
        public string Artist
        {
            get
            {
                if (_artist.Trim() == "")
                {
                    if (_albumArtist.Trim() == "")
                        return UnknownArtist;
                    else
                        return _albumArtist;
                }
                else return _artist;
            }
            set { _artist = value; }
        }
        private string _albumArtist;
        public string AlbumArtist
        {
            get
            {
                if (_albumArtist.Trim() == "")
                {
                    if (_artist.Trim() == "")
                        return UnknownArtist;
                    else
                        return _artist;
                }
                else return _albumArtist;
            }
            set { _albumArtist = value; }
        }
        public string FileName { get; set; }
        public string SortingLetter { get; set; }

        public string ArtistAndAlbum { get { return Artist + " - " + Album; } }

        public Song()
        {

        }

        Random random = new Random();
        public void SetAllData(string name, string album, string albumArtist, uint albumTrackNumber, uint albumCDNumber, string artist, string fileName)
        {
            this.Name = name;
            this.Album = album;
            this.AlbumArtist = albumArtist;
            this.AlbumTrackNumber = albumTrackNumber;
            this.AlbumCDNumber = albumCDNumber;
            this.Artist = artist;
            this.FileName = fileName;
            string alphabet = ApplicationSettings.SortingAlphabet.Read();
            this.SortingLetter = SortingHelper.GetAlphabetGroup(this.Name, alphabet).ToString();
        }

        public override bool Equals(object obj)
        {
            var other = obj as Song;
            if (other != null)
                return this.Id == other.Id;
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }
    }
}
