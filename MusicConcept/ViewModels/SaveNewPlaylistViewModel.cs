using MusicConcept.Common;
using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Resources;
using Windows.UI.Popups;

namespace MusicConcept.ViewModels
{
    class SaveNewPlaylistViewModel : NotifyPropertyChangedObject
    {
        private string _name;
        public string Name { get { return _name; } set { _name = value; NotifyPropertyChanged("Name"); } }

        public ICommand SaveCommand { get; private set; }

        public SaveNewPlaylistViewModel(MusicLibrary musicLibrary)
        {
            var resourceLoader = ResourceLoader.GetForViewIndependentUse();
            
            this.SaveCommand = new RelayCommandWithParameter(async o => {
                var commandAfter = o as ICommand;

                if (this.Name == null || this.Name.Trim() == "")
                    await new MessageDialog(resourceLoader.GetString("SaveNewPlaylistNameEmpty")).ShowAsync();
                else
                {
                    var lowerName = this.Name.ToLower();
                    var playlistWithSameName = (musicLibrary.SavedPlaylists).Where(p => p.Name.ToLower() == lowerName).ToArray();
                    if (playlistWithSameName.Any())
                    {
                        var messageDialog = new MessageDialog(resourceLoader.GetString("SaveNewPlaylistNameExists"));
                        messageDialog.Commands.Add(new UICommand(resourceLoader.GetString("MessageDialogNo")));
                        messageDialog.Commands.Add(new UICommand(resourceLoader.GetString("MessageDialogYes"), async c =>
                        {
                            foreach (var playlist in playlistWithSameName)
                                await musicLibrary.PlaylistManager.DeleteFromLibrary(musicLibrary, playlist);

                            await musicLibrary.PlaylistManager.SaveToLibrary(musicLibrary, this.Name);
                            if (commandAfter != null)
                                commandAfter.Execute(null);
                        }));
                        messageDialog.DefaultCommandIndex = 1;
                        messageDialog.CancelCommandIndex = 0;
                        await messageDialog.ShowAsync();
                    }
                    else
                    {
                        await musicLibrary.PlaylistManager.SaveToLibrary(musicLibrary, this.Name);
                        if (commandAfter != null)
                            commandAfter.Execute(null);
                    }
                }
            });
        }

    }
}
