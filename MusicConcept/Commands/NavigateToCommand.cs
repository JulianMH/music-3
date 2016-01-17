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
    class NavigateToCommand : DependencyObject, ICommand
    {
        public Frame NavigationFrame
        {
            get { return (Frame)this.GetValue(NavigationFrameProperty); }
            set { this.SetValue(NavigationFrameProperty, value); }
        }

        public static readonly DependencyProperty NavigationFrameProperty = DependencyProperty.Register("NavigationFrame",
            typeof(Frame), typeof(NavigateToCommand), new PropertyMetadata(null));

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged { add { } remove { } }

        public void Execute(object parameter)
        {
            var parameterStrings = ((string)parameter).Split('?');

            if (parameterStrings.Length > 1)
                this.NavigationFrame.Navigate(Type.GetType(parameterStrings[0]), parameterStrings[1]);
            else
                this.NavigationFrame.Navigate(Type.GetType(parameterStrings[0]));
        }
    }
}
