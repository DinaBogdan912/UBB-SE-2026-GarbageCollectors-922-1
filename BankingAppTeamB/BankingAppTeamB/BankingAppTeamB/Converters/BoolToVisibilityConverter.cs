using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace BankingAppTeamB.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>Maps <see langword="true"/> to <see cref="Visibility.Visible"/> and <see langword="false"/> (or non-bool) to <see cref="Visibility.Collapsed"/>.</summary>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isVisible)
            {
                return isVisible ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        /// <summary>Returns <see langword="true"/> when <paramref name="value"/> is <see cref="Visibility.Visible"/>.</summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Visibility visibility && visibility == Visibility.Visible;
        }
    }
}
