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
        private const int CacheDurationSeconds = 30;
        private const string EurUsdPair = "EUR/USD";
        private const string EurGbpPair = "EUR/GBP";
        private const string EurRonPair = "EUR/RON";
        private const string UsdRonPair = "USD/RON";
        private const string GbpRonPair = "GBP/RON";

        private const decimal EurUsdSeedRate = 1.15m;
        private const decimal EurGbpSeedRate = 0.86m;
        private const decimal EurRonSeedRate = 5.09m;
        private const decimal UsdRonSeedRate = 4.41m;
        private const decimal GbpRonSeedRate = 5.90m;

        private const decimal NoFee = 0m;
        private const int NoRelatedEntityId = 0;
        private const decimal MinimumAllowedRate = 0m;

        private readonly IExchangeRepository exchangeRepository;
        private readonly ITransactionPipelineService transactionPipelineService;
        private readonly IAccountService accountService;

        private Dictionary<string, decimal> cachedRates;
        private DateTime ratesLastFetched;

        private readonly Dictionary<int, LockedRate> lockedRates = new Dictionary<int, LockedRate>();
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(CacheDurationSeconds);

        public ExchangeService(IExchangeRepository exchangeRepository,
            ITransactionPipelineService transactionPipelineService,
            IAccountService accountService)
        {
            this.exchangeRepository = exchangeRepository;
            this.transactionPipelineService = transactionPipelineService;
            this.accountService = accountService;
        }

        /// <summary>Returns the built-in exchange rates for the 5 supported currency pairs.</summary>
        private static Dictionary<string, decimal> GetSeedExchangeRates() => new ()
        {
            { EurUsdPair, EurUsdSeedRate },
            { EurGbpPair, EurGbpSeedRate },
            { EurRonPair, EurRonSeedRate },
            { UsdRonPair, UsdRonSeedRate },
            { GbpRonPair, GbpRonSeedRate }
        };

        /// <summary>Gets all exchange rates including the reverse ones, caches them for 30 seconds so it's not recalculating constantly.</summary>
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

        /// <summary>Finds the exchange rate between two currencies, tries the reverse if the direct rate isn't there, throws if neither exists.</summary>
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

        /// <summary>Locks in the current exchange rate for a user for 30 seconds so it can't change mid-transaction.</summary>
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

        /// <summary>Checks if the user has a rate lock that hasn't expired yet.</summary>
        public bool IsRateLockValid(int userId)
        {
            if (!lockedRates.ContainsKey(userId))
            {
                return false;
            }

            return !lockedRates[userId].IsExpired();
        }

        /// <summary>Calculates the exchange fee — 0.5% of the amount but at least 50 cents.</summary>
        public decimal CalculateCommission(decimal amount)
        {
            decimal percentage = amount * CommissionRate;
            return Math.Max(MinimumCommission, percentage);
        }

        /// <summary>Calculates how much you'll actually receive in the target currency after fees are taken out.</summary>
        public decimal CalculateTargetAmount(decimal sourceAmount, decimal rate)
        {
            decimal commission = CalculateCommission(sourceAmount);
            return (sourceAmount * rate) - commission;
        }

        /// <summary>Does the actual currency swap using the locked rate — takes money from one account, puts it in another, saves the record.</summary>
        public ExchangeTransaction ExecuteExchange(ExchangeDto exchangeDto)
        {
            if (!IsRateLockValid(exchangeDto.UserId))
            {
                throw new Exception("No valid rate lock found or the 3-second window has expired.");
            }

            LockedRate lockedRateEntry = lockedRates[exchangeDto.UserId];

            decimal commission = CalculateCommission(exchangeDto.SourceAmount);
            decimal targetAmount = CalculateTargetAmount(exchangeDto.SourceAmount, lockedRateEntry.Rate);

            PipelineContext context = new PipelineContext
            {
                UserId = exchangeDto.UserId,
                SourceAccountId = exchangeDto.SourceAccountId,
                Amount = exchangeDto.SourceAmount,
                Currency = exchangeDto.SourceCurrency,
                Type = "Exchange",
                Fee = NoFee,
                CounterpartyName = $"Exchange to {exchangeDto.TargetCurrency}",
                RelatedEntityType = "Exchange",
                RelatedEntityId = NoRelatedEntityId
            };

            Transaction transactionLog = transactionPipelineService.RunPipeline(context);
            accountService.CreditAccount(exchangeDto.TargetAccountId, targetAmount);

            ExchangeTransaction exchangeTransaction = new ExchangeTransaction
            {
                UserId = exchangeDto.UserId,
                SourceAccountId = exchangeDto.SourceAccountId,
                TargetAccountId = exchangeDto.TargetAccountId,
                TransactionId = transactionLog.Id,
                SourceCurrency = exchangeDto.SourceCurrency,
                TargetCurrency = exchangeDto.TargetCurrency,
                SourceAmount = exchangeDto.SourceAmount,
                TargetAmount = targetAmount,
                ExchangeRate = lockedRateEntry.Rate,
                Commission = commission,
                RateLockedAt = lockedRateEntry.LockedAt,
                Status = TransferStatus.Completed,
                CreatedAt = DateTime.Now
            };

            exchangeRepository.Add(exchangeTransaction);
            lockedRates.Remove(exchangeDto.UserId);

            return exchangeTransaction;
        }

        /// <summary>Gets rid of the user's rate lock, like when they hit cancel.</summary>
        public void ClearLocks(int userId)
        {
            lockedRates.Remove(userId);
        }

        /// <summary>Gets all the rate alerts for a user that haven't fired yet.</summary>
        public List<RateAlert> GetUserAlerts(int userId)
        {
            return exchangeRepository.GetAlertsByUser(userId, isTriggered: false);
        }

        /// <summary>Makes a new rate alert after checking the currencies are valid and the rate makes sense.</summary>
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

            if (rate <= MinimumAllowedRate)
            {
                throw new ArgumentException("Rate cannot be zero or negative.");
            }

            RateAlert rateAlert = new RateAlert(userId, sourceCurrency, targetCurrency, rate, isBuyAlert);
            return exchangeRepository.AddAlert(rateAlert);
        }

        /// <summary>Permanently deletes a rate alert.</summary>
        public void DeleteAlert(int alertId)
        {
            exchangeRepository.DeleteAlert(alertId);
        }

        /// <summary>Goes through all active alerts and marks the ones that have hit their target rate as triggered.</summary>
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