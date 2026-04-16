using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace BankingAppTeamB.Converters
{
    public class BoolToBuySellConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isBuy)
            {
                return isBuy ? "BUY" : "SELL";
            }

            return "SELL";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value?.ToString() == "BUY";
        }
    }
}