using Microsoft.UI.Xaml.Controls;

namespace BankingAppTeamB.Services
{
    public static class NavigationService
    {
        public static Frame? Frame { get; set; }

        /// <summary>Navigates the shared <see cref="Frame"/> to a page of type <typeparamref name="T"/>, optionally passing <paramref name="parameter"/> as navigation data.</summary>
        public static void NavigateTo<T>(object? parameter = null)
        {
            Frame?.Navigate(typeof(T), parameter);
        }

        /// <summary>Navigates back in the frame's history if the frame has any back stack entries.</summary>
        public static void GoBack()
        {
            if (Frame != null && Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }
}