using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MusicConcept
{
    class ListItemTemplateSelector : DataTemplateSelector
    {
        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is Song)
                return (DataTemplate)App.Current.Resources["SongListItemTemplate"];
            else if (item is Album)
                return (DataTemplate)App.Current.Resources["AlbumListItemTemplate"];
            else if (item is SavedPlaylist)
                return (DataTemplate)App.Current.Resources["SavedPlaylistItemTemplate"];
            else if (item is Genre)
                return (DataTemplate)App.Current.Resources["GenreItemTemplate"];
            else if (item is string)
                return (DataTemplate)App.Current.Resources["ArtistListItemTemplate"];
            else
                throw new NotImplementedException();
        }
    }
}
