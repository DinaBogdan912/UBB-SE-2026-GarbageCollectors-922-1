using System;
using System.Collections.Generic;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Repositories;
using Microsoft.IdentityModel.Tokens;

namespace BankingAppTeamB.Services
{
    public class ExchangeService : IExchangeService
    {
        private const decimal CommissionRate = 0.005m;
        private const decimal MinimumCommission = 0.50m;
        private const int ExchangeRatePrecisionDecimals = 2;
        private const int BaseCurrencyComponentIndex = 0;
        private const int TargetCurrencyComponentIndex = 1;

        private readonly IExchangeRepository exchangeRepository;
        private readonly ITransactionPipelineService transactionPipelineService;
        private readonly IAccountService accountService;

        private Dictionary<string, decimal> cachedRates;
        private DateTime ratesLastFetched;

        private readonly Dictionary<int, LockedRate> lockedRates = new Dictionary<int, LockedRate>();
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

        public ExchangeService(IExchangeRepository exchangeRepository,
            ITransactionPipelineService transactionPipelineService,
            IAccountService accountService)
        {
            this.exchangeRepository = exchangeRepository;
            this.transactionPipelineService = transactionPipelineService;
            this.accountService = accountService;
        }

        // TODO: Replace hardcoded seed rates with live API call (FR-FX-001)
        private static Dictionary<string, decimal> GetSeedExchangeRates() => new ()
        {
            { "EUR/USD", 1.15m },
            { "EUR/GBP", 0.86m },
            { "EUR/RON", 5.09m },
            { "USD/RON", 4.41m },
            { "GBP/RON", 5.90m }
        };

        public Dictionary<string, decimal> GetLiveRates()
        {
            if (cachedRates != null && DateTime.Now - ratesLastFetched < CacheDuration)
            {
                return cachedRates;
            }

            Dictionary<string, decimal> rates = GetSeedExchangeRates();

            List<string> keys = new List<string>(rates.Keys);
            foreach (string currencyPair in keys)
            {
                string[] currencyComponents = currencyPair.Split('/');
                string inverseKey = $"{currencyComponents[TargetCurrencyComponentIndex]}/{currencyComponents[BaseCurrencyComponentIndex]}";
                rates[inverseKey] = Math.Round(1 / rates[currencyPair], ExchangeRatePrecisionDecimals);
            }

            cachedRates = rates;
            ratesLastFetched = DateTime.Now;
            return cachedRates;
        }

        public decimal GetRate(string sourceCurrency, string targetCurrency)
        {
            Dictionary<string, decimal> rates = GetLiveRates();
            string key = $"{sourceCurrency}/{targetCurrency}";

            if (rates.ContainsKey(key))
            {
                return rates[key];
            }

            string inverseKey = $"{targetCurrency}/{sourceCurrency}";
            if (rates.ContainsKey(inverseKey))
            {
                return Math.Round(1 / rates[inverseKey], ExchangeRatePrecisionDecimals);
            }

            throw new Exception($"Rate not found for pair {sourceCurrency}/{targetCurrency}");
        }

        public LockedRate LockRate(int userId, string sourceCurrency, string targetCurrency)
        {
            decimal rate = GetRate(sourceCurrency, targetCurrency);

            LockedRate lockedRate = new LockedRate
            {
                UserId = userId,
                CurrencyPair = $"{sourceCurrency}/{targetCurrency}",
                Rate = rate,
                LockedAt = DateTime.Now
            };
            lockedRates[userId] = lockedRate;
            return lockedRate;
        }

        public bool IsRateLockValid(int userId)
        {
            if (!lockedRates.ContainsKey(userId))
            {
                return false;
            }

            return !lockedRates[userId].IsExpired();
        }

        public decimal CalculateCommission(decimal amount)
        {
            decimal percentage = amount * CommissionRate;
            return Math.Max(MinimumCommission, percentage);
        }

        public decimal CalculateTargetAmount(decimal sourceAmount, decimal rate)
        {
            decimal commission = CalculateCommission(sourceAmount);
            return (sourceAmount * rate) - commission;
        }

        public ExchangeTransaction ExecuteExchange(ExchangeDto dto)
        {
            if (!IsRateLockValid(dto.UserId))
            {
                throw new Exception("No valid rate lock found or the 3-second window has expired.");
            }

            LockedRate lockedRateEntry = lockedRates[dto.UserId];

            decimal commission = CalculateCommission(dto.SourceAmount);
            decimal targetAmount = CalculateTargetAmount(dto.SourceAmount, lockedRateEntry.Rate);

            PipelineContext context = new PipelineContext
            {
                UserId = dto.UserId,
                SourceAccountId = dto.SourceAccountId,
                Amount = dto.SourceAmount,
                Currency = dto.SourceCurrency,
                Type = "Exchange",
                Fee = 0,
                CounterpartyName = $"Exchange to {dto.TargetCurrency}",
                RelatedEntityType = "Exchange",
                RelatedEntityId = 0
            };

            Transaction transactionLog = transactionPipelineService.RunPipeline(context);
            accountService.CreditAccount(dto.TargetAccountId, targetAmount);

            ExchangeTransaction exchangeTransaction = new ExchangeTransaction
            {
                UserId = dto.UserId,
                SourceAccountId = dto.SourceAccountId,
                TargetAccountId = dto.TargetAccountId,
                TransactionId = transactionLog.Id,
                SourceCurrency = dto.SourceCurrency,
                TargetCurrency = dto.TargetCurrency,
                SourceAmount = dto.SourceAmount,
                TargetAmount = targetAmount,
                ExchangeRate = lockedRateEntry.Rate,
                Commission = commission,
                RateLockedAt = lockedRateEntry.LockedAt,
                Status = TransferStatus.Completed,
                CreatedAt = DateTime.Now
            };

            exchangeRepository.Add(exchangeTransaction);
            lockedRates.Remove(dto.UserId);

            return exchangeTransaction;
        }

        public void ClearLocks(int userId)
        {
            lockedRates.Remove(userId);
        }

        public List<RateAlert> GetUserAlerts(int userId)
        {
            return exchangeRepository.GetAlertsByUser(userId, isTriggered: false);
        }

        public RateAlert CreateAlert(int userId, string sourceCurrency, string targetCurrency, decimal rate, bool isBuyAlert)
        {
            if (string.IsNullOrEmpty(sourceCurrency))
            {
                throw new ArgumentException("Source currency cannot be null or empty.");
            }

            if (string.IsNullOrEmpty(targetCurrency))
            {
                throw new ArgumentException("Target currency cannot be null or empty.");
            }

            if (sourceCurrency.Equals(targetCurrency))
            {
                throw new ArgumentException("Source currency cannot be the same as target currency.");
            }

            if (rate <= 0)
            {
                throw new ArgumentException("Rate cannot be zero or negative.");
            }

            RateAlert rateAlert = new RateAlert(userId, sourceCurrency, targetCurrency, rate, isBuyAlert);
            return exchangeRepository.AddAlert(rateAlert);
        }

        public void DeleteAlert(int id)
        {
            exchangeRepository.DeleteAlert(id);
        }

        public void CheckRateAlerts()
        {
            List<RateAlert> activeAlerts = exchangeRepository.GetAllAlerts(isTriggered: false);

            foreach (var alert in activeAlerts)
            {
                var currentRate = Math.Round(GetRate(alert.BaseCurrency, alert.TargetCurrency), ExchangeRatePrecisionDecimals);
                var targetRate = Math.Round(alert.TargetRate, ExchangeRatePrecisionDecimals);

                if (alert.IsBuyAlert)
                {
                    alert.IsTriggered = currentRate <= targetRate;
                }
                else
                {
                    alert.IsTriggered = currentRate >= targetRate;
                }
            }
        }
    }
}