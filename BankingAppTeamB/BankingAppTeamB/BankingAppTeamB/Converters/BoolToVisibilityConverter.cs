using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace BankingAppTeamB.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is bool isVisible && isVisible ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => value is Visibility visibility && visibility == Visibility.Visible;
    }
}