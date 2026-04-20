using BankingAppTeamB.Configuration;
using BankingAppTeamB.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.
namespace BankingAppTeamB.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RateAlertsPage : Page
    {
        public RateAlertViewModel ViewModel => (RateAlertViewModel)DataContext;

        public RateAlertsPage()
        {
            InitializeComponent();

            // Initialize the view model with the active session's user ID so alerts are loaded for the current user.
            this.DataContext = new RateAlertViewModel(ServiceLocator.ExchangeService, ServiceLocator.UserSessionService.CurrentUserId);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs navigationEventArgs)
        {
            base.OnNavigatedTo(navigationEventArgs);
            if (ViewModel != null)
            {
                await ViewModel.LoadAsync();
            }
        }
    }
}
