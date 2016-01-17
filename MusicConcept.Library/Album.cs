using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace MusicConcept.Library
{
    public sealed class Album : NotifyPropertyChangedObject
    {
        private MusicLibrary library;
        public string Name { get; private set; }
        public string Artist { get; private set; }
        public string ArtistAndAlbum { get { return Artist + " - " + Name; } }

        private Brush loadedCover = null;
        public Brush AlbumCover
        {
            get
            {
                if (loadedCover == null)
                {
                    loadedCover = (Brush)Application.Current.Resources["SystemColorControlAccentBrush"];
                    LoadCover();
                }
                return loadedCover;
            }
        }

        public async void LoadCover()
        {
            var cover = await library.GetAlbumCoverForSong(this.Name, this.Artist);
            if (cover != null)
            {
                var bitmapImage = new BitmapImage(new Uri(cover));
                loadedCover = new ImageBrush() { ImageSource = bitmapImage, Stretch = Stretch.UniformToFill };
                NotifyPropertyChanged("AlbumCover");
            }
        }

        public async Task<StorageFile> GetCoverFile()
        {
            return await library.GetAlbumCoverFileNameForSong(this.Name, this.Artist);
        }

        public Album(string name, string artist, MusicLibrary library)
        {
            this.Name = name;
            this.Artist = artist;
            this.library = library;
        }

        public bool Contains(Song song)
        {
            return song.Album.Equals(this.Name, StringComparison.CurrentCultureIgnoreCase) && song.AlbumArtist.Equals(this.Artist, StringComparison.CurrentCultureIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            var album = obj as Album;
            if (album != null)
                return album.Name.Equals(this.Name, StringComparison.CurrentCultureIgnoreCase) && album.Artist.Equals(this.Artist, StringComparison.CurrentCultureIgnoreCase);
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return (this.Name + this.Artist).ToLower().GetHashCode();
        }
    }
}
