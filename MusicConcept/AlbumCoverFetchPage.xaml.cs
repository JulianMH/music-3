using MusicConcept.Common;
using MusicConcept.Library;
using MusicConcept.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MusicConcept
{
    public sealed partial class AlbumCoverFetchPage : AppPage
    {
        public AlbumCoverFetchPage()
        {
            this.InitializeComponent();

            this.NavigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.NavigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            this.DataContext = await ViewModel.Instance.GetAlbumCoverFetchViewModel((string)e.NavigationParameter);
            if (this.DataContext == null)
                this.NavigationHelper.GoBack();
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var viewModel = (AlbumCoverFetchViewModel)this.DataContext;

            viewModel.SaveCover(e.ClickedItem);

            Frame.GoBack();
        }

        private void ChoosePictureButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            filePicker.ViewMode = PickerViewMode.Thumbnail;

            // Filter to include a sample subset of file types
            filePicker.FileTypeFilter.Clear();
            filePicker.FileTypeFilter.Add(".bmp");
            filePicker.FileTypeFilter.Add(".png");
            filePicker.FileTypeFilter.Add(".jpeg");
            filePicker.FileTypeFilter.Add(".jpg");

            CoreApplication.GetCurrentView().Activated += AlbumCoverFetchPage_Activated;

            filePicker.PickSingleFileAndContinue();

        }

        void AlbumCoverFetchPage_Activated(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args1)
        {
            var args = args1 as FileOpenPickerContinuationEventArgs;

            if (args != null)
            {
                CoreApplication.GetCurrentView().Activated -= AlbumCoverFetchPage_Activated;

                if (args.Files.Count == 0) return;

                var selectedImageFile = args.Files[0];

                var viewModel = (AlbumCoverFetchViewModel)this.DataContext;

                viewModel.SaveAndCropCover(selectedImageFile);

                this.Frame.GoBack();
            }
        }
    }
}
