using System;
using Microsoft.UI.Xaml.Data;

namespace BankingAppTeamB.Converters
{
    public class DecimalFormatConverter : IValueConverter
    {
        private const string DefaultDecimalFormatPattern = "F2";
        private const string DefaultFormattedZeroValue = "0.00";

        /// <summary>Formats a decimal to two decimal places; prepends the currency symbol from parameter when provided.</summary>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is decimal decimalValue)
            {
                string currencySymbol = parameter as string ?? string.Empty;
                if (string.IsNullOrEmpty(currencySymbol))
                {
                    return decimalValue.ToString(DefaultDecimalFormatPattern);
                }

                return $"{currencySymbol}{decimalValue.ToString(DefaultDecimalFormatPattern)}";
            }

            return DefaultFormattedZeroValue;
        }

        /// <summary>Not supported — this converter is one-way only.</summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
