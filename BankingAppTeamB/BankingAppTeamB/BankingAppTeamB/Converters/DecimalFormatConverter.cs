using System;
using Microsoft.UI.Xaml.Data;

namespace BankingAppTeamB.Converters
{
    public class DecimalFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is decimal d)
            {
                string symbol = parameter as string ?? string.Empty;
                return string.IsNullOrEmpty(symbol) ? d.ToString("F2") : $"{symbol}{d:F2}";
            }
            return "0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}