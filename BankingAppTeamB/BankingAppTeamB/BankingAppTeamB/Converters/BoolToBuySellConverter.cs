using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace BankingAppTeamB.Converters
{
    public class BoolToBuySellConverter : IValueConverter
    {
        private const string BuyLabel = "BUY";
        private const string SellLabel = "SELL";

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isBuy)
            {
                return isBuy ? BuyLabel : SellLabel;
            }

            return SellLabel;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value?.ToString() == BuyLabel;
        }
    }
}