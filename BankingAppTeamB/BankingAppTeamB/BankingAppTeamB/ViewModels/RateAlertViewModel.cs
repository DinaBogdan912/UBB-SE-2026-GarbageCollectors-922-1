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
        private const int MinimumRate = 0;
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

        /// <summary>Loads the user's active alerts and refreshes live rates on a background thread, then evaluates each alert against current rates.</summary>
        public async Task LoadAsync()
        {
            var alerts = await Task.Run(() => exchangeService.GetUserAlerts(userId));
            Alerts = new ObservableCollection<RateAlert>(alerts);

            await LoadRatesAsync();
        }

        /// <summary>Fetches live rates on a background thread, rebuilds the AvailableCurrencies list, and re-evaluates trigger state for each existing alert.</summary>
        private async Task LoadRatesAsync()
        {
            var liveRates = await Task.Run(() => exchangeService.GetLiveRates());
            LiveRates = liveRates;

            AvailableCurrencies.Clear();

            var currencies = liveRates.Keys
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

        /// <summary>Validates the currency pair and target rate inputs, creates the alert via the service on a background thread, and adds it to the observable collection.</summary>
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

            var targetRateText = TargetRateText.Trim();
            if (targetRateText.Contains(',') && targetRateText.Contains('.'))
            {
                ErrorMessage = "Invalid number format.";
                return;
            }

            targetRateText = targetRateText.Replace('.', ',');

            if (!decimal.TryParse(targetRateText,
                System.Globalization.NumberStyles.Number,
                new System.Globalization.CultureInfo("ro-RO"),
                out decimal parsedRate) || parsedRate <= MinimumRate)
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
            catch (Exception createAlertException)
            {
                ErrorMessage = createAlertException.Message;
            }
        }

        /// <summary>Deletes the given alert via the service and removes it from the Alerts collection.</summary>
        private void DeleteAlert(object commandParameter)
        {
            var alert = (RateAlert)commandParameter;
            exchangeService.DeleteAlert(alert.Id);
            Alerts.Remove(alert);
        }

        /// <summary>Marks the matching alert in the Alerts collection as triggered when notified by an external source.</summary>
        private void OnAlertTriggered(RateAlert alert)
        {
            foreach (var existingAlert in Alerts)
            {
                if (existingAlert.Id == alert.Id)
                {
                    existingAlert.IsTriggered = true;
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