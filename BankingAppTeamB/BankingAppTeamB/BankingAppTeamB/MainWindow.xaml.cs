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

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (NavigationService.Frame == null) return;

            var item = args.SelectedItem as NavigationViewItem;
            if (item == null) return;

            switch (item.Tag?.ToString())
            {
                case NavigationTags.Transfer: NavigationService.NavigateTo<TransferPage>(); break;
                case NavigationTags.Beneficiaries: NavigationService.NavigateTo<BeneficiariesPage>(); break;
                case NavigationTags.Bill: NavigationService.NavigateTo<BillPayPage>(); break;
                case NavigationTags.Reccurring: NavigationService.NavigateTo<RecurringPaymentsPage>(); break;
                case NavigationTags.Exchange: NavigationService.NavigateTo<FXPage>(); break;
                case NavigationTags.Alerts: NavigationService.NavigateTo<RateAlertsPage>(); break;
            }
        }
    }
}