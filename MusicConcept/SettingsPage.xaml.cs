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
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MusicConcept
{
    public sealed partial class SettingsPage : AppPage
    {
        public SettingsPage()
        {
            this.InitializeComponent();

            this.DataContext = new SettingsPageViewModel();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ViewModel.Instance.ReloadFilters();
        }

        private async void ResetWallpaperButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await (await ApplicationData.Current.LocalFolder.GetFileAsync("Wallpaper")).DeleteAsync();
            }
            catch
            {

            }
            ViewModel.Instance.UpdateWallpaper();
        }

        private void SelectWallpaperButton_Click(object sender, RoutedEventArgs e)
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

            CoreApplication.GetCurrentView().Activated += SettingsPage_Activated;

            filePicker.PickSingleFileAndContinue();
        }
        async void SettingsPage_Activated(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args1)
        {
            var args = args1 as FileOpenPickerContinuationEventArgs;

            if (args != null)
            {
                CoreApplication.GetCurrentView().Activated -= SettingsPage_Activated;

                if (args.Files.Count == 0) return;

                var selectedImageFile = args.Files[0];

                await selectedImageFile.CopyAsync(ApplicationData.Current.LocalFolder, "Wallpaper", NameCollisionOption.ReplaceExisting);

                ViewModel.Instance.UpdateWallpaper();
            }
        }
    }
}
