using BankingAppTeamB.Configuration;
using BankingAppTeamB.Models;
using BankingAppTeamB.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace BankingAppTeamB.Views
{
    public sealed partial class RecurringPaymentsPage : Page
    {
        private readonly RecurringPaymentViewModel _viewModel;

        public RecurringPaymentsPage()
        {
            InitializeComponent();

            _viewModel = new RecurringPaymentViewModel(ServiceLocator.RecurringPaymentService, ServiceLocator.BillPaymentService);
            DataContext = _viewModel;

            StartDatePicker.Date = DateTimeOffset.Now;
            EndDatePicker.Date = DateTimeOffset.Now;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await _viewModel.LoadAsync();
        }

        private void AmountNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            _viewModel.Amount = double.IsNaN(sender.Value) ? 0 : (decimal)sender.Value;
        }

        private void StartDatePicker_DateChanged(object sender, DatePickerValueChangedEventArgs args)
        {
            if (sender is DatePicker picker)
            {
                _viewModel.StartDate = picker.Date.DateTime.Date;
            }

        }

        private void EndDatePicker_DateChanged(object sender, DatePickerValueChangedEventArgs args)
        {
            if (sender is DatePicker picker)
            {
                _viewModel.EndDate = picker.Date.DateTime.Date;
            }

        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            await _viewModel.CreateAsync();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is RecurringPayment payment)
            {
                _viewModel.Pause(payment);
            }
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is RecurringPayment payment)
            {
                _viewModel.Resume(payment);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is RecurringPayment payment)
            {
                _viewModel.Cancel(payment);
            }
        }
    }
}