using MusicConcept.Library;
using MusicConcept.ViewModels;
using MusicConcept.ViewModels.MusicFilters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MusicConcept.MusicFilters
{
    public abstract class FilterPivotItem : PivotItem
    {
        public Frame NavigationFrame
        {
            get { return (Frame)this.GetValue(NavigationFrameProperty); }
            set { this.SetValue(NavigationFrameProperty, value); }
        }

        public static readonly DependencyProperty NavigationFrameProperty = DependencyProperty.Register("NavigationFrame",
            typeof(Frame), typeof(FilterPivotItem), new PropertyMetadata(null));

        public bool CanChangeSelected
        {
            get { return (bool)this.GetValue(CanChangeSelectedProperty); }
            set { this.SetValue(CanChangeSelectedProperty, value); }
        }

        public static readonly DependencyProperty CanChangeSelectedProperty = DependencyProperty.Register("CanChangeSelected",
            typeof(bool), typeof(FilterPivotItem), new PropertyMetadata(false));

        public bool GoBackAfterClick
        {
            get { return (bool)this.GetValue(GoBackAfterClickProperty); }
            set { this.SetValue(GoBackAfterClickProperty, value); }
        }

        public static readonly DependencyProperty GoBackAfterClickProperty = DependencyProperty.Register("GoBackAfterClick",
            typeof(bool), typeof(FilterPivotItem), new PropertyMetadata(false));

        protected void SongListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var song = (Song)e.ClickedItem;
            ViewModel.Instance.GoToPlayMode(song, ((FilterViewModel)this.DataContext).SongSource);

            if (GoBackAfterClick)
                this.NavigationFrame.GoBack();
            else if (NavigationFrame.CurrentSourcePageType != typeof(PivotPage))
                this.NavigationFrame.Navigate(typeof(NowPlayingPage));
        }

        protected void ArtistListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            this.NavigationFrame.Navigate(typeof(ArtistPage), (string)e.ClickedItem);
        }

        protected void AlbumListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            this.NavigationFrame.Navigate(typeof(AlbumPage), ((Album)e.ClickedItem).ArtistAndAlbum);
        }
        protected void GenreListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            this.NavigationFrame.Navigate(typeof(GenrePage), ((Genre)e.ClickedItem).Name);
        }

        protected void SavedPlaylist_ItemClick(object sender, ItemClickEventArgs e)
        {
            ViewModel.Instance.PlayPlaylist((SavedPlaylist)e.ClickedItem);
            if (GoBackAfterClick)
                this.NavigationFrame.GoBack();
        }

        protected void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Album)
                AlbumListView_ItemClick(sender, e);
            else if (e.ClickedItem is Song)
                SongListView_ItemClick(sender, e);
            else if (e.ClickedItem is SavedPlaylist)
                SavedPlaylist_ItemClick(sender, e);
            else if (e.ClickedItem is Genre)
                GenreListView_ItemClick(sender, e);
            else if (e.ClickedItem is string)
                ArtistListView_ItemClick(sender, e);
            else
                throw new NotImplementedException();
        }

    }
}
