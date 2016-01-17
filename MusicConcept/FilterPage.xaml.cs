using MusicConcept.Common;
using MusicConcept.MusicFilters;
using MusicConcept.ViewModels;
using MusicConcept.ViewModels.MusicFilters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MusicConcept
{
    public sealed partial class FilterPage : AppPage
    {
        public FilterPage()
        {
            this.InitializeComponent();

            this.NavigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.NavigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            var parameters = ((string)e.NavigationParameter).Split('|');

            bool canChangeSelected;
            if (parameters.Count() > 1 && bool.TryParse(parameters[1], out canChangeSelected))
                ((FilterViewModelsToPivotItemsConverter)this.Resources["FilterViewModelsToPivotItemsConverter"]).CanChangeSelected = canChangeSelected;
            
            var dataContext = ViewModel.Instance.Filters.Where(p => p.GetType().Name == parameters[0]);

            bool goBackAfterClick;
            if (parameters.Count() > 2 && bool.TryParse(parameters[2], out goBackAfterClick) && goBackAfterClick)
                ((FilterViewModelsToPivotItemsConverter)this.Resources["FilterViewModelsToPivotItemsConverter"]).GoBackAfterClick = goBackAfterClick;


            if (!dataContext.Any())
                this.NavigationHelper.GoBack();
            else
                this.DataContext = dataContext;
        }

        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

    }
}
