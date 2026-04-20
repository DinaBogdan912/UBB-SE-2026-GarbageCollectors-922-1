using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace BankingAppTeamB.Converters
{
    public class StepToVisibilityConverter : IValueConverter
    {
        /// <summary>Returns <see cref="Visibility.Visible"/> when the current wizard step (int) matches the target step number supplied as a string converter parameter; collapses otherwise.</summary>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int currentStepNumber
                && parameter is string targetStepText
                && int.TryParse(targetStepText, out int targetStepNumber))
            {
                return currentStepNumber == targetStepNumber ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        /// <summary>Not supported — this converter is one-way only.</summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
