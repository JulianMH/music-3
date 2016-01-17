using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MusicConcept
{
    public sealed partial class EmptyListViewText : UserControl
    {
        private string currentState = "Blank";

        public bool IsEmpty
        {
            get { return (bool)this.GetValue(IsEmptyProperty); }
            set { this.SetValue(IsEmptyProperty, value); }
        }

        public static readonly DependencyProperty IsEmptyProperty = DependencyProperty.Register("IsEmpty",
            typeof(bool), typeof(EmptyListViewText), new PropertyMetadata(false, IsEmptyChanged));

        public EmptyListViewText()
        {
            this.InitializeComponent();

            ViewModels.ViewModel.Instance.PropertyChanged += Instance_PropertyChanged;
        }

        void Instance_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsCreatingLibrary")
                this.UpdateVisualState();
        }

        private static void IsEmptyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisControl = (EmptyListViewText)d;

            thisControl.UpdateVisualState();
        }


        private void UpdateVisualState()
        {
            string newState = "Blank";
            if(IsEmpty)
            {
                if (ViewModels.ViewModel.Instance.IsCreatingLibrary)
                    newState = "Loading";
                else
                    newState = "Emtpy";
            }

            Debug.WriteLine(newState);
            
            if (currentState != newState)
            {
                currentState = newState;
                switch(currentState)
                {
                    case "Loading":
                        EmptyTextBlock.Visibility = Visibility.Collapsed;
                        LoadingLibraryTextBlock.Visibility = Visibility.Visible;
                        break;
                    case "Emtpy":
                        EmptyTextBlock.Visibility = Visibility.Visible;
                        LoadingLibraryTextBlock.Visibility = Visibility.Collapsed;
                        break;
                    default:
                        EmptyTextBlock.Visibility = Visibility.Collapsed;
                        LoadingLibraryTextBlock.Visibility = Visibility.Collapsed;
                        break;
                }
            }
        }
    }
}
