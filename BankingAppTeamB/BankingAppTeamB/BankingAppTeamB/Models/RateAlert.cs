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

        private bool isTriggered;

        public bool IsTriggered
        {
            get => this.isTriggered;
            set
            {
                if (this.isTriggered == value)
                {
                    return;
                }

                this.isTriggered = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTriggered)));
            }
        }

        public RateAlert(int userId, string baseCurrency, string targetCurrency, decimal rate, bool isBuyAlert)
        {
            UserId = userId;
            BaseCurrency = baseCurrency;
            TargetCurrency = targetCurrency;
            TargetRate = rate;
            IsTriggered = false;
            CreatedAt = DateTime.Now;
            IsBuyAlert = isBuyAlert;
        }

        public RateAlert()
        {
        }

        public void SetBuyAlert(bool v)
        {
            IsBuyAlert = v;
        }

        public int GetId()
        {
            return Id;
        }

        public void SetTriggered(bool v)
        {
            IsTriggered = v;
        }

        public decimal GetTargetRate()
        {
            return TargetRate;
        }

        public string GetBaseCurrency()
        {
            return BaseCurrency;
        }

        public string GetTargetCurrency()
        {
            return TargetCurrency;
        }
    }
}
