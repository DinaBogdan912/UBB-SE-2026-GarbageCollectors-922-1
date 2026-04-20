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

            NavigationViewItem? selectedNavigationItem = args.SelectedItem as NavigationViewItem;
            if (selectedNavigationItem == null)
            {
                return;
            }

            switch (selectedNavigationItem.Tag?.ToString())
            {
                case NavigationTags.Transfer: NavigationService.NavigateTo<TransferPage>(); break;
                case NavigationTags.Beneficiaries: NavigationService.NavigateTo<BeneficiariesPage>(); break;
                case NavigationTags.BillPayments: NavigationService.NavigateTo<BillPayPage>(); break;
                case NavigationTags.RecurringPayments: NavigationService.NavigateTo<RecurringPaymentsPage>(); break;
                case NavigationTags.ForeignExchange: NavigationService.NavigateTo<FXPage>(); break;
                case NavigationTags.ExchangeRateAlerts: NavigationService.NavigateTo<RateAlertsPage>(); break;
            }
        }
    }
}
