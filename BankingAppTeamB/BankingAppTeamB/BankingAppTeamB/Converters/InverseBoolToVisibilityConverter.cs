using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace BankingAppTeamB.Converters
{
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        /// <summary>Maps true to Visibility.Collapsed and false (or non-bool) to Visibility.Visible — the inverse of BoolToVisibilityConverter.</summary>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isVisible)
            {
                return isVisible ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Visible;
        }

        /// <summary>Returns true when value is Visibility.Collapsed.</summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Visibility visibility && visibility == Visibility.Collapsed;
        }
    }
}
