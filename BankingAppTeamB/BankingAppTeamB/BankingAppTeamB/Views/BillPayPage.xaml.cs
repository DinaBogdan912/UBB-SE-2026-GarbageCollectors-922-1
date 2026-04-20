using System;
using System.ComponentModel;
using BankingAppTeamB.Configuration;
using BankingAppTeamB.Models;
using BankingAppTeamB.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BankingAppTeamB.Views
{
    public sealed partial class BillPayPage : Page
    {
        private const decimal ZeroAmount = 0m;

        public BillPayViewModel ViewModel { get; }

        public BillPayPage()
        {
            InitializeComponent();
            ViewModel = new BillPayViewModel(ServiceLocator.BillPaymentService);
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs navigationEventArgs)
        {
            base.OnNavigatedTo(navigationEventArgs);
            await ViewModel.LoadAsync();
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs autoSuggestBoxTextChangedEventArgs)
        {
            if (autoSuggestBoxTextChangedEventArgs.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ViewModel.SearchCommand.Execute(null);
            }
        }

        private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            ViewModel.SearchCommand.Execute(null);
        }

        private void BillersList_ItemClick(object sender, ItemClickEventArgs itemClickEventArgs)
        {
            if (itemClickEventArgs.ClickedItem is Biller biller)
            {
                ViewModel.SelectBillerCommand.Execute(biller);
            }
        }

        private void SavedBillersList_ItemClick(object sender, ItemClickEventArgs itemClickEventArgs)
        {
            if (itemClickEventArgs.ClickedItem is SavedBiller savedBiller)
            {
                ViewModel.SelectBillerCommand.Execute(savedBiller);
            }
        }

        private void AmountBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs numberBoxValueChangedEventArgs)
        {
            if (!double.IsNaN(sender.Value) && !double.IsInfinity(sender.Value))
            {
                ViewModel.Amount = Convert.ToDecimal(sender.Value);
            }
            else
            {
                ViewModel.Amount = ZeroAmount;
            }
        }
    }
}
