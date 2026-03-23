using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankingAppTeamB.Commands;
using BankingAppTeamB.Models;
using BankingAppTeamB.Services;

namespace BankingAppTeamB.ViewModels
{
    public class RateAlertViewModel : ViewModelBase
    {
        private readonly ExchangeService _exchangeService;
        private readonly int _userId;

        private ObservableCollection<RateAlert> _alerts = new();
        private string _baseCurrency = string.Empty;
        private string _targetCurrency = string.Empty;
        private decimal _targetRate;
        private string _errorMessage = string.Empty;
        private Dictionary<string, decimal> _liveRates = new();

        public ObservableCollection<RateAlert> Alerts
        {
            get => _alerts;
            set => SetProperty(ref _alerts, value);
        }

        public string BaseCurrency
        {
            get => _baseCurrency;
            set => SetProperty(ref _baseCurrency, value);
        }

        public string TargetCurrency
        {
            get => _targetCurrency;
            set => SetProperty(ref _targetCurrency, value);
        }

        public decimal TargetRate
        {
            get => _targetRate;
            set => SetProperty(ref _targetRate, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public Dictionary<string, decimal> LiveRates
        {
            get => _liveRates;
            set => SetProperty(ref _liveRates, value);
        }

        public AsyncRelayCommand RefreshRatesCommand { get; }
        public AsyncRelayCommand CreateAlertCommand { get; }
        public RelayCommand DeleteAlertCommand { get; }

        public RateAlertViewModel(ExchangeService exchangeService, int userId)
        {
            _exchangeService = exchangeService;
            _userId = userId;

            RefreshRatesCommand = new AsyncRelayCommand(_ => LoadRatesAsync());
            CreateAlertCommand = new AsyncRelayCommand(_ => CreateAlertAsync());
            DeleteAlertCommand = new RelayCommand(param => DeleteAlert((RateAlert)param));
        }

        public async Task LoadAsync()
        {
            var alerts = await Task.Run(() => _exchangeService.GetUserAlerts(_userId));
            Alerts = new ObservableCollection<RateAlert>(alerts);

            await LoadRatesAsync();
        }

        private async Task LoadRatesAsync()
        {
            var rates = await Task.Run(() => _exchangeService.GetLiveRates());
            LiveRates = rates;
        }

        private async Task CreateAlertAsync()
        {
            if (string.IsNullOrWhiteSpace(BaseCurrency) ||
                string.IsNullOrWhiteSpace(TargetCurrency))
            {
                ErrorMessage = "Base currency and target currency are required.";
                return;
            }

            if (string.Equals(BaseCurrency, TargetCurrency, StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Base currency and target currency must be different.";
                return;
            }

            if (TargetRate <= 0)
            {
                ErrorMessage = "Target rate must be greater than zero.";
                return;
            }

            try
            {
                var newAlert = await Task.Run(() =>
                    _exchangeService.CreateAlert(_userId, BaseCurrency, TargetCurrency, TargetRate, false));

                Alerts.Add(newAlert);

                BaseCurrency = string.Empty;
                TargetCurrency = string.Empty;
                TargetRate = 0;
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private void DeleteAlert(object param)
        {
            var alert = (RateAlert)param;
            _exchangeService.DeleteAlert(alert.Id);
            Alerts.Remove(alert);
        }

        private void OnAlertTriggered(RateAlert alert)
        {
            foreach (var existing in Alerts)
            {
                if (existing.Id == alert.Id)
                {
                    existing.IsTriggered = true;
                    break;
                }
            }
        }
    }
}