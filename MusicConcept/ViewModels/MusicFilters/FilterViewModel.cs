using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace MusicConcept.ViewModels.MusicFilters
{
    public abstract class FilterViewModel : NotifyPropertyChangedObject
    {
        private bool _isSelected;
        public bool IsSelected { get { return _isSelected; } set { _isSelected = value; NotifyPropertyChanged("IsSelected"); } }

        public string Header { get; private set; }

        public SongSource SongSource { get; protected set; }

        private string[] libraryUpdateProperties;

        public FilterViewModel(ResourceLoader resourceLoader, string headerResource, params string[] libraryUpdateProperties)
        {
            this.Header = resourceLoader.GetString(headerResource);
            this.libraryUpdateProperties = libraryUpdateProperties;
        }

        public abstract void LoadData(MusicLibrary library);

        public void MusicLibrary_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var library = (MusicLibrary)sender;
            if(libraryUpdateProperties.Contains(e.PropertyName))
            {
                this.LoadData(library);
            }
        }
    }
}
