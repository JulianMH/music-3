using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MusicConcept.Commands
{
    class NavigateToAlbumCoverFetchCommand : DependencyObject, ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged { add { } remove { } }

        public async void Execute(object parameter)
        {
            await Task.Delay(50);
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(AlbumCoverFetchPage), ((Album)parameter).ArtistAndAlbum);
        }
    }
}
