using BankingAppTeamB.Configuration;
using BankingAppTeamB.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BankingAppTeamB.Views
{
    public sealed partial class BillPayPage : Page
    {
        private readonly BillPayViewModel _viewModel;

        public BillPayPage()
        {
            this.InitializeComponent();
            _viewModel = new BillPayViewModel(ServiceLocator.BillPaymentService);
            this.DataContext = _viewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await _viewModel.LoadAsync();
        }

        private void AmountBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (DataContext is BillPayViewModel vm)
            {
                vm.Amount = (decimal)sender.Value;
            }
        }
    }
}