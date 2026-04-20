using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace BankingAppTeamB.Converters
{
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        /// <summary>Maps <see langword="true"/> to <see cref="Visibility.Collapsed"/> and <see langword="false"/> (or non-bool) to <see cref="Visibility.Visible"/> — the inverse of <c>BoolToVisibilityConverter</c>.</summary>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isVisible)
            {
                return isVisible ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Visible;
        }

        /// <summary>Returns <see langword="true"/> when <paramref name="value"/> is <see cref="Visibility.Collapsed"/>.</summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Visibility visibility && visibility == Visibility.Collapsed;
        }
    }
}
