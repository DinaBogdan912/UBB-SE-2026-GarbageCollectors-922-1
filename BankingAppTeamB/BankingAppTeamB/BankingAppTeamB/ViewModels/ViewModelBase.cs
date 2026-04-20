using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BankingAppTeamB.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Sets <paramref name="field"/> to <paramref name="value"/> and raises <see cref="PropertyChanged"/> if the value changed; returns <see langword="true"/> when a change occurred.</summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>Raises <see cref="PropertyChanged"/> for <paramref name="propertyName"/>, notifying the UI that the property value may have changed.</summary>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}