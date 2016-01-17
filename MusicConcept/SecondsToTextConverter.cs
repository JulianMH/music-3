using System;
using Windows.UI.Xaml.Data;

namespace MusicConcept
{
    class SecondsToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var timeSpan = TimeSpan.FromSeconds((double)value);
            return string.Format("{0}:{1:00}", (int)timeSpan.TotalMinutes, timeSpan.Seconds);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
