using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace MusicConcept.ViewModels.MusicFilters
{
    public sealed class SearchFilter : FilterViewModel
    {
        public SearchFilter(ResourceLoader resourceLoader)
            : base(resourceLoader, "SearchItemHeader", "Songs", "Albums", "Artists")
        {
        }

        private MusicLibrary musicLibrary;

        private IEnumerable<object> _items;
        public IEnumerable<object> Items { get { return _items; } private set { _items = value; NotifyPropertyChanged("Items"); NotifyPropertyChanged("ItemsIsEmpty"); } }
        public bool ItemsIsEmpty { get { return (_items == null || !_items.Any()) && (_searchString != null && _searchString != ""); } }

        private bool _isLoading;
        public bool IsLoading { get { return _isLoading; } private set { _isLoading = value; NotifyPropertyChanged("IsLoading"); } }

        private string _searchString;
        public string SearchString { get { return _searchString; } set { _searchString = value; NotifyPropertyChanged("SearchString"); StartSearchAsync(_searchString); } }

        private readonly object searchTaskLock = new object();
        private CancellationTokenSource searchTaskCancellationTokenSource;
        private async void StartSearchAsync(string searchString)
        {
            IsLoading = true;
            lock (searchTaskLock)
            {
                if (searchTaskCancellationTokenSource != null)
                    searchTaskCancellationTokenSource.Cancel();
                searchTaskCancellationTokenSource = new CancellationTokenSource();

                if (_searchString == null || _searchString == "")
                {
                    this.Items = null;
                    IsLoading = false;
                    return;
                }
            }


            var task = new Task<IEnumerable<object>>(() => Search(searchString), searchTaskCancellationTokenSource.Token);

            try
            {
                task.Start();
                var result = await task;
                lock (searchTaskLock)
                {
                    this.Items = result;
                    IsLoading = false;
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        private IEnumerable<object> Search(string searchString)
        {
            searchString = searchString.ToLower();

            Func<object, string> getNameFromItem = o =>
            {
                if (o is Song) return ((Song)o).Name.ToLower();
                if (o is Album) return ((Album)o).Name.ToLower();
                if (o is SavedPlaylist) return ((SavedPlaylist)o).Name.ToLower();
                else return o.ToString().ToLower();
            };
            Func<object, int> getRankFromItem = o =>
            {
                if (o is Song) return 4;
                if (o is Album) return 3;
                if (o is string) return 2;
                else return 1;
            };

            var songsContaining = musicLibrary.Songs.Where(s => s.Name.ToLower().Contains(searchString)).Select(o => (object)o);
            var albumsContaining = musicLibrary.Albums.Where(a => a.Name.ToLower().Contains(searchString)).Select(o => (object)o);
            var artistsContaining = musicLibrary.Artists.Where(a => a.ToLower().Contains(searchString)).Select(o => (object)o);
            var playlistsContaining = musicLibrary.SavedPlaylists.Where(a => a.Name.ToLower().Contains(searchString)).Select(o => (object)o);

            var allContaining = songsContaining.Concat(albumsContaining).Concat(artistsContaining).Concat(playlistsContaining);

            return allContaining.OrderBy(o => getNameFromItem(o).StartsWith(searchString)).ThenBy(o => getRankFromItem(o)).ThenBy(o => getNameFromItem(o));
        }

        public override void LoadData(MusicLibrary library)
        {
            this.musicLibrary = library;
            StartSearchAsync(_searchString);
        }
    }
}
