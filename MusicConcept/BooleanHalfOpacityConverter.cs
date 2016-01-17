using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace MusicConcept
{
    /// <summary>
    /// Konvertiert Boolean zu 1 bzw 0.5
    /// </summary>
    public class BooleanHalfOpacityConverter : IValueConverter
    {
        public bool IsInverted { get; set; }

        public BooleanHalfOpacityConverter()
        {
            this.IsInverted = false;
        }
        
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool boolean = (bool)value;
            if (this.IsInverted)
                boolean = !boolean;

            if (boolean)
                return 1;
            else
                return 0.5;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (this.IsInverted)
                if ((double)value == 1)
                    return true;
                else 
                    return false;
            else
                if ((double)value == 1)
                    return false;
                else 
                    return true;
        }
    }
}
