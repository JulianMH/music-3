using MusicConcept.Common;
using MusicConcept.Library;
using MusicConcept.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MusicConcept
{
    public sealed partial class SaveNewPlaylistPage : AppPage
    {
        public SaveNewPlaylistPage()
        {
            this.InitializeComponent();

            this.NavigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.NavigationHelper.SaveState += this.NavigationHelper_SaveState;
        }


        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            this.DataContext = await ViewModel.Instance.GetSaveNewPlaylistViewModel();
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {

        }

        private void NameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                var viewModel = (SaveNewPlaylistViewModel)this.DataContext;
                viewModel.SaveCommand.Execute(Resources["NavigateBackCommand"]);
            }
        }

        private void thisPage_Loaded(object sender, RoutedEventArgs e)
        {

            NameTextBox.Focus(FocusState.Programmatic);
        }
    }
}
