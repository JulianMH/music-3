using MusicConcept.Common;
using MusicConcept.Library;
using MusicConcept.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Storage;
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
    public sealed partial class PivotPage : AppPage
    {
        public PivotPage()
        {
            this.InitializeComponent();

            this.NavigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.NavigationHelper.SaveState += this.NavigationHelper_SaveState;
        }
        
        #region Save and Load State
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            this.DataContext = ViewModel.Instance;
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }
        #endregion
        private void NowPlaying_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(NowPlayingPage));
        }
    }
}
