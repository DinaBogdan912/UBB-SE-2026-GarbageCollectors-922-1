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
        private readonly IExchangeService exchangeService;
        private readonly int userId;

        private ObservableCollection<RateAlert> alerts = new ObservableCollection<RateAlert>();
        private string baseCurrency = string.Empty;
        private string targetCurrency = string.Empty;
        private string targetRateText = string.Empty;
        private string errorMessage = string.Empty;
        private bool isTriggered;
        private Dictionary<string, decimal> liveRates = new Dictionary<string, decimal>();
        private bool isBuyAlert;

        public ObservableCollection<RateAlert> Alerts
        {
            get => alerts;
            set => SetProperty(ref alerts, value);
        }

        public string BaseCurrency
        {
            get => baseCurrency;
            set => SetProperty(ref baseCurrency, value);
        }

        public string TargetCurrency
        {
            get => targetCurrency;
            set => SetProperty(ref targetCurrency, value);
        }

        public string TargetRateText
        {
            get => targetRateText;
            set => SetProperty(ref targetRateText, value);
        }

        public string ErrorMessage
        {
            get => errorMessage;
            set => SetProperty(ref errorMessage, value);
        }

        public Dictionary<string, decimal> LiveRates
        {
            get => liveRates;
            set => SetProperty(ref liveRates, value);
        }

        public bool IsBuyAlert
        {
            get => isBuyAlert;
            set => SetProperty(ref isBuyAlert, value);
        }

        public bool IsTriggered
        {
            get => isTriggered;
            set => SetProperty(ref isTriggered, value);
        }

        public AsyncRelayCommand RefreshRatesCommand { get; }
        public AsyncRelayCommand CreateAlertCommand { get; }
        public RelayCommand DeleteAlertCommand { get; }

        public RateAlertViewModel(IExchangeService exchangeService, int userId)
        {
            this.exchangeService = exchangeService;
            this.userId = userId;

            RefreshRatesCommand = new AsyncRelayCommand(unusedParameter => LoadRatesAsync());
            CreateAlertCommand = new AsyncRelayCommand(unusedParameter => CreateAlertAsync());
            DeleteAlertCommand = new RelayCommand(commandParameter => DeleteAlert((RateAlert)commandParameter));
        }

        public async Task LoadAsync()
        {
            var alerts = await Task.Run(() => exchangeService.GetUserAlerts(userId));
            Alerts = new ObservableCollection<RateAlert>(alerts);

            await LoadRatesAsync();
        }

        private async Task LoadRatesAsync()
        {
            var rates = await Task.Run(() => exchangeService.GetLiveRates());
            LiveRates = rates;

            AvailableCurrencies.Clear();

            var currencies = rates.Keys
                .SelectMany(currencyPair => currencyPair.Split('/'))
                .Distinct()
                .OrderBy(currencyCode => currencyCode);

            foreach (var currency in currencies)
            {
                AvailableCurrencies.Add(currency);
            }

            foreach (var alert in Alerts)
            {
                var currentRate = exchangeService.GetRate(alert.BaseCurrency, alert.TargetCurrency);

                if (!alert.IsBuyAlert && currentRate >= alert.TargetRate)
                {
                    alert.IsTriggered = true;
                }
                else if (alert.IsBuyAlert && currentRate <= alert.TargetRate)
                {
                    alert.IsTriggered = true;
                }
                else
                {
                    alert.IsTriggered = false;
                }
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
                    exchangeService.CreateAlert(userId, BaseCurrency, TargetCurrency, parsedRate, isBuyAlert));

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
            exchangeService.DeleteAlert(alert.Id);
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

        private ObservableCollection<string> availableCurrencies = new ObservableCollection<string>();

        public ObservableCollection<string> AvailableCurrencies
        {
            get => availableCurrencies;
            set => SetProperty(ref availableCurrencies, value);
        }
    }
}