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
        private readonly IExchangeService _exchangeService;
        private readonly int _userId;

        private ObservableCollection<RateAlert> _alerts = new();
        private string _baseCurrency = string.Empty;
        private string _targetCurrency = string.Empty;
        private string _targetRateText = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isTriggered;
        private Dictionary<string, decimal> _liveRates = new();
        private bool _isBuyAlert;

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

        public string TargetRateText
        {
            get => _targetRateText;
            set => SetProperty(ref _targetRateText, value);
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

        public bool IsBuyAlert
        {
            get => _isBuyAlert;
            set => SetProperty(ref _isBuyAlert, value) ;
        }
        
        public bool IsTriggered
        {
            get => _isTriggered;
            set => SetProperty(ref _isTriggered, value);
        }
        
        public AsyncRelayCommand RefreshRatesCommand { get; }
        public AsyncRelayCommand CreateAlertCommand { get; }
        public RelayCommand DeleteAlertCommand { get; }

        public RateAlertViewModel(IExchangeService exchangeService, int userId)
        {
            _exchangeService = exchangeService;
            _userId = userId;

            RefreshRatesCommand = new AsyncRelayCommand(unusedParameter => LoadRatesAsync());
            CreateAlertCommand = new AsyncRelayCommand(unusedParameter => CreateAlertAsync());
            DeleteAlertCommand = new RelayCommand(commandParameter => DeleteAlert((RateAlert)commandParameter));
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

            AvailableCurrencies.Clear();

            var currencies = rates.Keys
                .SelectMany(currencyPair => currencyPair.Split('/')) 
                .Distinct()
                .OrderBy(currencyCode => currencyCode);

            foreach (var currency in currencies)
                AvailableCurrencies.Add(currency);

            foreach (var alert in Alerts)
            {
                var currentRate = _exchangeService.GetRate(alert.BaseCurrency, alert.TargetCurrency);

                if (!alert.IsBuyAlert && currentRate >= alert.TargetRate)
                    alert.IsTriggered = true;
                else if (alert.IsBuyAlert && currentRate <= alert.TargetRate)
                    alert.IsTriggered = true;
                else
                    alert.IsTriggered = false;
            }
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


            var text = TargetRateText.Trim();
            if (text.Contains(',') && text.Contains('.'))
            {
                ErrorMessage = "Invalid number format.";
                return;
            }

            text = text.Replace('.', ',');

            if (!decimal.TryParse(text,
                System.Globalization.NumberStyles.Number,
                new System.Globalization.CultureInfo("ro-RO"),
                out decimal parsedRate) || parsedRate <= 0)
            {
                ErrorMessage = "Target rate must be a valid number (ex: 1,2 or 1.2).";
                return;
            }

            try
            {
                var newAlert = await Task.Run(() =>
                    _exchangeService.CreateAlert(_userId, BaseCurrency, TargetCurrency, parsedRate, _isBuyAlert));

                Alerts.Add(newAlert);

                BaseCurrency = string.Empty;
                TargetCurrency = string.Empty;
                TargetRateText = string.Empty;
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private void DeleteAlert(object commandParameter)
        {
            var alert = (RateAlert)commandParameter;
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

        private ObservableCollection<string> _availableCurrencies = new();

        public ObservableCollection<string> AvailableCurrencies
        {
            get => _availableCurrencies;
            set => SetProperty(ref _availableCurrencies, value);
        }
    }
}