using System;
using Microsoft.UI.Xaml.Data;

namespace BankingAppTeamB.Converters
{
    public class BoolToBuySellConverter : IValueConverter
    {
        private const string BuyLabel = "BUY";
        private const string SellLabel = "SELL";

        /// <summary>Converts a <see cref="bool"/> to the string <c>"BUY"</c> (true) or <c>"SELL"</c> (false).</summary>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isBuyAction)
            {
                return isBuyAction ? BuyLabel : SellLabel;
            }

            return SellLabel;
        }

        /// <summary>Converts a <c>"BUY"</c> / <c>"SELL"</c> label back to the corresponding <see cref="bool"/>.</summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string actionLabel = value?.ToString() ?? string.Empty;
            return actionLabel == BuyLabel;
        }
    }
}
