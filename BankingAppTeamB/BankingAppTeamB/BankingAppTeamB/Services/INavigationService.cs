using Microsoft.UI.Xaml.Controls;

namespace BankingAppTeamB.Services
{
    public interface INavigationService
    {
        static abstract Frame? Frame { get; set; }

        static abstract void GoBack();
        static abstract void NavigateTo<T>(object? parameter = null);
    }
}