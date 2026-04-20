using System;
using Microsoft.UI.Xaml.Data;

namespace BankingAppTeamB.Converters
{
    public class BoolToBuySellConverter : IValueConverter
    {
        private const string BuyLabel = "BUY";
        private const string SellLabel = "SELL";

        /// <summary>Converts a bool to the string "BUY" (true) or "SELL" (false).</summary>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isBuyAction)
            {
                return isBuyAction ? BuyLabel : SellLabel;
            }

            return SellLabel;
        }

        /// <summary>Converts a "BUY" / "SELL" label back to the corresponding bool.</summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string actionLabel = value?.ToString() ?? string.Empty;
            return actionLabel == BuyLabel;
        }
    }
}
