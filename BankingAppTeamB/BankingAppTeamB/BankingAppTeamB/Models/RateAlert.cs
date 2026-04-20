using System;
using System.ComponentModel;

namespace BankingAppTeamB.Models
{
    public class RateAlert : INotifyPropertyChanged
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string BaseCurrency { get; set; } = string.Empty;

        public string TargetCurrency { get; set; } = string.Empty;

        public decimal TargetRate { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsBuyAlert { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool isAlertTriggered;

        public bool IsTriggered
        {
            get => this.isAlertTriggered;
            set
            {
                if (this.isAlertTriggered == value)
                {
                    return;
                }

                this.isAlertTriggered = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTriggered)));
            }
        }

        /// <summary>Creates a new rate alert for the specified user and currency pair, setting IsTriggered to false and stamping the creation time.</summary>
        public RateAlert(int userId, string baseCurrency, string targetCurrency, decimal targetRate, bool isBuyAlert)
        {
            UserId = userId;
            BaseCurrency = baseCurrency;
            TargetCurrency = targetCurrency;
            TargetRate = targetRate;
            IsTriggered = false;
            CreatedAt = DateTime.Now;
            IsBuyAlert = isBuyAlert;
        }

        public RateAlert()
        {
        }

        /// <summary>Sets whether this alert fires when the rate reaches the target from a buy perspective.</summary>
        public void SetBuyAlertState(bool isBuyAlert)
        {
            IsBuyAlert = isBuyAlert;
        }

        /// <summary>Returns the alert's database identifier.</summary>
        public int GetAlertId()
        {
            return Id;
        }

        /// <summary>Marks the alert as triggered or untriggered, raising PropertyChanged when the value changes.</summary>
        public void SetTriggeredState(bool isTriggered)
        {
            IsTriggered = isTriggered;
        }

        /// <summary>Returns the exchange rate that, when reached, fires this alert.</summary>
        public decimal GetAlertTargetRate()
        {
            return TargetRate;
        }

        /// <summary>Returns the base currency code (e.g. "EUR") of the monitored pair.</summary>
        public string GetAlertBaseCurrency()
        {
            return BaseCurrency;
        }

        /// <summary>Returns the target currency code (e.g. "USD") of the monitored pair.</summary>
        public string GetAlertTargetCurrency()
        {
            return TargetCurrency;
        }

        /// <summary>Alias for SetBuyAlertState.</summary>
        public void SetBuyAlert(bool isBuyAlert) => SetBuyAlertState(isBuyAlert);

        /// <summary>Alias for GetAlertId.</summary>
        public int GetId() => GetAlertId();

        /// <summary>Alias for SetTriggeredState.</summary>
        public void SetTriggered(bool isTriggered) => SetTriggeredState(isTriggered);

        /// <summary>Alias for GetAlertTargetRate.</summary>
        public decimal GetTargetRate() => GetAlertTargetRate();

        /// <summary>Alias for GetAlertBaseCurrency.</summary>
        public string GetBaseCurrency() => GetAlertBaseCurrency();

        /// <summary>Alias for GetAlertTargetCurrency.</summary>
        public string GetTargetCurrency() => GetAlertTargetCurrency();
    }
}
