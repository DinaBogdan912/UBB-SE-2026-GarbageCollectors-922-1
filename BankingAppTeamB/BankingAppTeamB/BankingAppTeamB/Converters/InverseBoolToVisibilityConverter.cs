using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace BankingAppTeamB.Converters
{
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is bool isVisible && isVisible ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => value is Visibility visibility && visibility == Visibility.Collapsed;
    }
}