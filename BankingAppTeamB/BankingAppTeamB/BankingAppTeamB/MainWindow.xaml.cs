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
                case "transfer": NavigationService.NavigateTo<TransferPage>(); break;
                case "beneficiaries": NavigationService.NavigateTo<BeneficiariesPage>(); break;
                case "bill": NavigationService.NavigateTo<BillPayPage>(); break;
                case "recurring": NavigationService.NavigateTo<RecurringPaymentsPage>(); break;
                case "exchange": NavigationService.NavigateTo<FXPage>(); break;
                case "alerts": NavigationService.NavigateTo<RateAlertsPage>(); break;
            }
        }
    }
}