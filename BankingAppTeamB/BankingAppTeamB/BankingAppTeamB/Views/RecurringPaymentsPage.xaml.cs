using System;
using BankingAppTeamB.Configuration;
using BankingAppTeamB.Models;
using BankingAppTeamB.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BankingAppTeamB.Views
{
    public sealed partial class RecurringPaymentsPage : Page
    {
        private const decimal ZeroAmount = 0m;

        private readonly RecurringPaymentViewModel viewModel;

        public RecurringPaymentsPage()
        {
            InitializeComponent();

            viewModel = new RecurringPaymentViewModel(ServiceLocator.RecurringPaymentService, ServiceLocator.BillPaymentService);
            DataContext = viewModel;

            StartDatePicker.Date = DateTimeOffset.Now;
            EndDatePicker.Date = DateTimeOffset.Now;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs navigationEventArgs)
        {
            base.OnNavigatedTo(navigationEventArgs);
            await viewModel.LoadAsync();
        }

        private void AmountNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs numberBoxValueChangedEventArgs)
        {
            viewModel.Amount = double.IsNaN(sender.Value) ? ZeroAmount : (decimal)sender.Value;
        }

        private void StartDatePicker_DateChanged(object sender, DatePickerValueChangedEventArgs datePickerValueChangedEventArgs)
        {
            if (sender is DatePicker picker)
            {
                viewModel.StartDate = picker.Date.DateTime.Date;
            }
        }

        private void EndDatePicker_DateChanged(object sender, DatePickerValueChangedEventArgs datePickerValueChangedEventArgs)
        {
            if (sender is DatePicker picker)
            {
                viewModel.EndDate = picker.Date.DateTime.Date;
            }
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            await viewModel.CreateAsync();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            if (sender is Button button && button.Tag is RecurringPayment payment)
            {
                viewModel.Pause(payment);
            }
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            if (sender is Button button && button.Tag is RecurringPayment payment)
            {
                viewModel.Resume(payment);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            if (sender is Button button && button.Tag is RecurringPayment payment)
            {
                viewModel.Cancel(payment);
            }
        }
    }
}