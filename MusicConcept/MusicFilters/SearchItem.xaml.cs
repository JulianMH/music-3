using MusicConcept.ViewModels.MusicFilters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace MusicConcept.MusicFilters
{
    public sealed partial class SearchItem : FilterPivotItem
    {
        public SearchItem()
        {
            this.InitializeComponent();
        }
        /*
            this.Loaded +=SearchItem_Loaded;
            this.Unloaded += SearchItem_Unloaded;
        }

        async void SearchItem_Unloaded(object sender, RoutedEventArgs e)
        {
            var searchFilter = (SearchFilter)DataContext;
            searchFilter.PropertyChanged -= searchFilter_PropertyChanged;

           await StatusBar.GetForCurrentView().ProgressIndicator.HideAsync();
        }

        void SearchItem_Loaded(object sender, RoutedEventArgs e)
        {
            var searchFilter = (SearchFilter)DataContext;
            searchFilter.PropertyChanged += searchFilter_PropertyChanged;
        }

        async void searchFilter_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var searchFilter = (SearchFilter)sender;
            if(e.PropertyName == "IsLoading")
            {
                var progressIndicator = StatusBar.GetForCurrentView().ProgressIndicator;
                if (searchFilter.IsLoading)
                {
                    progressIndicator.ProgressValue = null;
                    progressIndicator.Text = new ResourceLoader().GetString("SearchItemProgressIndicator");
                    await progressIndicator.ShowAsync();
                }
                else
                    await progressIndicator.HideAsync();
            }
        }*/
        
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AnimateShift(false, (TextBox)sender);
        }

        public void AnimateShift(bool reverse, TextBox textBox)
        {
            var page = (Window.Current.Content as Frame).Content as Page;
            page.RenderTransform = new TranslateTransform();

            var upDistance = -textBox.TransformToVisual(page).TransformPoint(new Point(0, 0)).Y + textBox.Margin.Top + 32 - 8;
            var animation = new DoubleAnimation(){
                Duration = new Duration(TimeSpan.FromMilliseconds(250)),
                From = reverse ? upDistance : 0,
                To = reverse ? 0 : upDistance
            };

            Storyboard.SetTarget(animation, page.RenderTransform);
            Storyboard.SetTargetProperty(animation, "Y");

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            storyboard.Begin();
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            AnimateShift(true, (TextBox)sender);
        }
    }
}
