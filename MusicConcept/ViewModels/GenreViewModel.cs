using MusicConcept.Library;
using MusicConcept.ViewModels.MusicFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace MusicConcept.ViewModels
{
    public class GenreViewModel : NotifyPropertyChangedObject
    {
        public string Name { get; private set; }

        private object _filters;
        public object Filters { get { return _filters; } set { _filters = value; NotifyPropertyChanged("Filters"); } }

        private MusicLibrary library;
        private Genre genre;

        public GenreViewModel(MusicLibrary library, Genre genre)
        {
            this.Name = genre.Name;
            this.genre = genre;
            this._filters = new FilterViewModel[0];
            this.library = library;
            library.PropertyChanged += library_PropertyChanged;

            if (library.Songs != null) LoadFilters();
        }

        void library_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Songs") LoadFilters();
        }

        private async void LoadFilters()
        {
            var resourceLoader = ResourceLoader.GetForViewIndependentUse();

            var songs = await library.GetSongsForGenre(genre);
            var filters = new FilterViewModel[]{
                new AllSongsFilter(resourceLoader, s => 
                    songs.Any(p => p.Id == s.Id), SongSource.GenreSource(genre.Id))
            };

            foreach (var filter in filters)
                filter.LoadData(library);

            this.Filters = filters;
        }
    }
}
