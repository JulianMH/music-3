using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Resources;
using Windows.UI.Popups;

namespace MusicConcept.Commands
{
    class DeleteSavedPlaylistCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged { add { } remove { } }

        public async void Execute(object parameter)
        {
            var resourceLoader = ResourceLoader.GetForViewIndependentUse();
            var savedPlaylist = parameter as SavedPlaylist;
            if (parameter == null)
                throw new ArgumentException("The CommandParameter for the DeleteCommand must be a SavedPlaylist object.");

            var messageDialog = new MessageDialog(string.Format(resourceLoader.GetString("SavedPlaylistsViewModelDelete"), savedPlaylist.Name));
            messageDialog.Commands.Add(new UICommand(resourceLoader.GetString("MessageDialogNo")));
            messageDialog.Commands.Add(new UICommand(resourceLoader.GetString("MessageDialogYes"), async c =>
            {
                await ViewModels.ViewModel.Instance.DeleteSavedPlaylist((SavedPlaylist)parameter);
            }));
            messageDialog.DefaultCommandIndex = 1;
            messageDialog.CancelCommandIndex = 0;
            await messageDialog.ShowAsync();
        }
    }
}
