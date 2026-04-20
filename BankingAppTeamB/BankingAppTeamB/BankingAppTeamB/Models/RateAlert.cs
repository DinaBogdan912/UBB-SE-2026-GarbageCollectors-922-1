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

        public void SetBuyAlertState(bool isBuyAlert)
        {
            IsBuyAlert = isBuyAlert;
        }

        public int GetAlertId()
        {
            return Id;
        }

        public void SetTriggeredState(bool isTriggered)
        {
            IsTriggered = isTriggered;
        }

        public decimal GetAlertTargetRate()
        {
            return TargetRate;
        }

        public string GetAlertBaseCurrency()
        {
            return BaseCurrency;
        }

        public string GetAlertTargetCurrency()
        {
            return TargetCurrency;
        }

        public void SetBuyAlert(bool isBuyAlert) => SetBuyAlertState(isBuyAlert);

        public int GetId() => GetAlertId();

        public void SetTriggered(bool isTriggered) => SetTriggeredState(isTriggered);

        public decimal GetTargetRate() => GetAlertTargetRate();

        public string GetBaseCurrency() => GetAlertBaseCurrency();

        public string GetTargetCurrency() => GetAlertTargetCurrency();
    }
}
