using System;
using Microsoft.UI.Xaml.Data;

namespace BankingAppTeamB.Converters
{
    public class DecimalFormatConverter : IValueConverter
    {
        private const string DefaultDecimalFormat = "F2";
        private const string DefaultFormattedZeroValue = "0.00";

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is decimal amount)
            {
                string currencySymbol = parameter as string ?? string.Empty;
                return string.IsNullOrEmpty(currencySymbol)
                    ? amount.ToString(DefaultDecimalFormat)
                    : $"{currencySymbol}{amount:F2}";
            }

            return DefaultFormattedZeroValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}