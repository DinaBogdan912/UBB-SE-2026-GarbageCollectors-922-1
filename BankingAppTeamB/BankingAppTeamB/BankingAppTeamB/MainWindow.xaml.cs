using BankingAppTeamB.Configuration;
using BankingAppTeamB.Services;
using BankingAppTeamB.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BankingAppTeamB
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            NavigationService.Frame = MainFrame;
        }

        private void NavView_SelectionChanged(NavigationView navigationView, NavigationViewSelectionChangedEventArgs selectionChangedEventArgs)
        {
            if (NavigationService.Frame == null)
            {
                return;
            }

            var selectedNavigationItem = selectionChangedEventArgs.SelectedItem as NavigationViewItem;
            if (selectedNavigationItem == null)
            {
                return;
            }

            switch (selectedNavigationItem.Tag?.ToString())
            {
                case NavigationTags.Transfer: NavigationService.NavigateTo<TransferPage>(); break;
                case NavigationTags.Beneficiaries: NavigationService.NavigateTo<BeneficiariesPage>(); break;
                case NavigationTags.Bill: NavigationService.NavigateTo<BillPayPage>(); break;
                case NavigationTags.Recurring: NavigationService.NavigateTo<RecurringPaymentsPage>(); break;
                case NavigationTags.Exchange: NavigationService.NavigateTo<FXPage>(); break;
                case NavigationTags.Alerts: NavigationService.NavigateTo<RateAlertsPage>(); break;
            }
        }
    }
}