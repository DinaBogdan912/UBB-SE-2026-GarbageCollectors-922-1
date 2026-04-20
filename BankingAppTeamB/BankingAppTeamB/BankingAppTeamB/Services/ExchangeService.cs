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

        /// <summary>Returns the hardcoded seed rates for the five supported currency pairs used when no live source is available.</summary>
        private static Dictionary<string, decimal> GetSeedExchangeRates() => new ()
        {
            { EurUsdPair, EurUsdSeedRate },
            { EurGbpPair, EurGbpSeedRate },
            { EurRonPair, EurRonSeedRate },
            { UsdRonPair, UsdRonSeedRate },
            { GbpRonPair, GbpRonSeedRate }
        };

        /// <summary>Returns all supported exchange rates (including computed inverse pairs), using a 30-second in-memory cache to avoid repeated recomputation.</summary>
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

        /// <summary>Returns the exchange rate for the given currency pair, checking both direct and inverse keys; throws if the pair is not supported.</summary>
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

        /// <summary>Fetches the current rate for the pair, creates a 30-second <see cref="LockedRate"/> for <paramref name="userId"/>, and stores it in memory for subsequent exchange execution.</summary>
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

        /// <summary>Returns <see langword="true"/> when <paramref name="userId"/> has an active, non-expired rate lock.</summary>
        public bool IsRateLockValid(int userId)
        {
            if (!lockedRates.ContainsKey(userId))
            {
                return false;
            }

            return !lockedRates[userId].IsExpired();
        }

        /// <summary>Calculates the exchange commission as 0.5 % of <paramref name="amount"/>, with a minimum of €0.50.</summary>
        public decimal CalculateCommission(decimal amount)
        {
            decimal percentage = amount * CommissionRate;
            return Math.Max(MinimumCommission, percentage);
        }

        /// <summary>Returns the amount to credit in the target currency: (<paramref name="sourceAmount"/> × <paramref name="rate"/>) minus the calculated commission.</summary>
        public decimal CalculateTargetAmount(decimal sourceAmount, decimal rate)
        {
            decimal commission = CalculateCommission(sourceAmount);
            return (sourceAmount * rate) - commission;
        }

        /// <summary>Executes the exchange using the user's locked rate: debits the source account, credits the target account, persists the exchange record, and removes the rate lock.</summary>
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

        /// <summary>Removes any active rate lock for <paramref name="userId"/>, typically called on cancel or navigation away from the exchange flow.</summary>
        public void ClearLocks(int userId)
        {
            lockedRates.Remove(userId);
        }

        /// <summary>Returns the non-triggered rate alerts for <paramref name="userId"/>.</summary>
        public List<RateAlert> GetUserAlerts(int userId)
        {
            return exchangeRepository.GetAlertsByUser(userId, isTriggered: false);
        }

        /// <summary>Validates the currency pair and rate, then persists and returns a new <see cref="RateAlert"/> for <paramref name="userId"/>.</summary>
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

        /// <summary>Permanently removes the rate alert identified by <paramref name="alertId"/>.</summary>
        public void DeleteAlert(int alertId)
        {
            exchangeRepository.DeleteAlert(alertId);
        }

        /// <summary>Evaluates all non-triggered alerts against live rates and sets <see cref="RateAlert.IsTriggered"/> in memory when the target rate condition is met.</summary>
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