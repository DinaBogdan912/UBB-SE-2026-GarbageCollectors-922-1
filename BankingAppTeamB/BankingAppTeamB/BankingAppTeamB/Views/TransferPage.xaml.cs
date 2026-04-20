using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.ViewModels;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Controls;
using BankingAppTeamB.Configuration;

namespace BankingAppTeamB.Views
{
    public sealed partial class TransferPage : Page
    {
        public TransferViewModel ViewModel { get; }

        public TransferPage()
        {
            this.InitializeComponent();

            ViewModel = new TransferViewModel(ServiceLocator.TransferService);
            this.DataContext = ViewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs navigationEventArgs)
        {
            base.OnNavigatedTo(navigationEventArgs);

            ViewModel.LoadAccounts();

            if (navigationEventArgs.Parameter is TransferDto transferDto)
            {
                ViewModel.RecipientName = transferDto.RecipientName;
                ViewModel.RecipientIBAN = transferDto.RecipientIBAN;
                ViewModel.Amount = transferDto.Amount;
                ViewModel.Currency = transferDto.Currency;
                ViewModel.TwoFAToken = transferDto.TwoFAToken;
            }
        }
    }
}
