using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MusicConcept.Commands
{
    class AddToPlaylistCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged { add { } remove { } }

        public void Execute(object parameter)
        {
            if (parameter is Song)
                ViewModels.ViewModel.Instance.AddSongsToPlaylist(SongSource.OneSongSource(((Song)parameter).Id));
            else if (parameter is Album)
                ViewModels.ViewModel.Instance.AddSongsToPlaylist(SongSource.AlbumSource(((Album)parameter).Artist, ((Album)parameter).Name));
            else if (parameter is string)
                ViewModels.ViewModel.Instance.AddSongsToPlaylist(SongSource.ArtistSource((string)parameter));
        }
    }
}
