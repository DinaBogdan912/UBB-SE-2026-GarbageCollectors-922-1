using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using BankingAppTeamB.Commands;
using BankingAppTeamB.Configuration;
using Microsoft.UI.Xaml;
using BankingAppTeamB.Models;
using BankingAppTeamB.Services;
using BankingAppTeamB.Mocks;
using BankingAppTeamB.Models.DTOs;

namespace BankingAppTeamB.ViewModels;

public class FXViewModel : ViewModelBase
{
    private const int InitialStep = 1;
    private const int LockedRateStep = 4;
    private const int ExchangeResultStep = 5;
    private const int SecondsRemainingDeadline = 0;
    private const decimal NoCommission = 0m;
    private const decimal EqualCurrencyRate = 1m;
    private const int CountdownTickSeconds = 1;
    private const int MinimumAmount = 0;
    private const int DefaultAmount = 0;
    private const int DefaultLiveRate = 0;
    private const int DefaultCommission = 0;
    private const int DefaultSecondsRemaining = 0;
    private const int DefaultTargetAmount = 0;
    private readonly IExchangeService exchangeService;

    private int currentStep;

    public int CurrentStep
    {
        get => currentStep;
        set => SetProperty(ref currentStep, value);
    }

    public ObservableCollection<Account> Accounts { get; } = new ObservableCollection<Account>();

    private Account? sourceAccount;

    public Account? SourceAccount
    {
        get => sourceAccount;
        set
        {
            if (SetProperty(ref sourceAccount, value))
            {
                SourceCurrency = value?.Currency ?? string.Empty;
            }
        }
    }

    private Account? targetAccount;

    public Account? TargetAccount
    {
        get => targetAccount;
        set
        {
            if (SetProperty(ref targetAccount, value))
            {
                TargetCurrency = value?.Currency ?? string.Empty;
            }
        }
    }

    private string sourceCurrency = string.Empty;

    public string SourceCurrency
    {
        get => sourceCurrency;
        set
        {
            if (SetProperty(ref sourceCurrency, value))
            {
                Recalculate();
            }
        }
    }

    private string targetCurrency = string.Empty;

    public string TargetCurrency
    {
        get => targetCurrency;
        set
        {
            if (SetProperty(ref targetCurrency, value))
            {
                Recalculate();
            }
        }
    }

    private decimal amount;

    public decimal Amount
    {
        get => amount;
        set
        {
            if (SetProperty(ref amount, value))
            {
                Recalculate();
            }
        }
    }

    private string amountText = string.Empty;

    public string AmountText
    {
        get => amountText;
        set
        {
            if (SetProperty(ref amountText, value))
            {
                if (decimal.TryParse(value, out var parsed))
                {
                    Amount = parsed;
                }
                else
                {
                    Amount = MinimumAmount;
                }
            }
        }
    }

    private decimal liveRate;

    public decimal LiveRate
    {
        get => liveRate;
        set => SetProperty(ref liveRate, value);
    }

    private decimal commission;

    public decimal Commission
    {
        get => commission;
        set => SetProperty(ref commission, value);
    }

    private decimal targetAmount;

    public decimal TargetAmount
    {
        get => targetAmount;
        set => SetProperty(ref targetAmount, value);
    }

    private int secondsRemaining;

    public int SecondsRemaining
    {
        get => secondsRemaining;
        set => SetProperty(ref secondsRemaining, value);
    }

    private bool isRateExpired;

    public bool IsRateExpired
    {
        get => isRateExpired;
        set => SetProperty(ref isRateExpired, value);
    }

    private string transactionReference = string.Empty;

    public string TransactionReference
    {
        get => transactionReference;
        set => SetProperty(ref transactionReference, value);
    }

    private string errorMessage = string.Empty;

    public string ErrorMessage
    {
        get => errorMessage;
        set => SetProperty(ref errorMessage, value);
    }

    public AsyncRelayCommand LoadRatesCommand { get; }
    public RelayCommand LockRateCommand { get; }

    public AsyncRelayCommand ExecuteExchangeCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand NewExchangeCommand { get; }

    private DispatcherTimer? timer;
    private LockedRate? lockedRate;

    public FXViewModel(IExchangeService exchangeService)
    {
        this.exchangeService = exchangeService;

        LoadRatesCommand = new AsyncRelayCommand(LoadRatesAsync);
        LockRateCommand = new RelayCommand(LockRate);

        ExecuteExchangeCommand = new AsyncRelayCommand(ExecuteExchanges);
        CancelCommand = new RelayCommand(Cancel);
        NewExchangeCommand = new RelayCommand(Reset);
        var commandParameter = LoadAccountsAsync();
    }

    /// <summary>Stops the countdown timer and resets all exchange state (equivalent to abandoning the current exchange flow).</summary>
    private void Cancel(object? unusedParameter)
    {
        timer?.Stop();
        Reset(null);
    }

    /// <summary>Clears all locked rates, resets all observable properties to their defaults, and returns the wizard to the first step.</summary>
    private void Reset(object? unusedParameter)
    {
        timer?.Stop();
        timer = null;

        exchangeService.ClearLocks(ServiceLocator.UserSessionService.CurrentUserId);

        SourceAccount = null;
        TargetAccount = null;

        lockedRate = null;

        SourceCurrency = string.Empty;
        TargetCurrency = string.Empty;

        Amount = DefaultAmount;
        LiveRate = DefaultLiveRate;
        Commission = DefaultCommission;
        TargetAmount = DefaultTargetAmount;

        SecondsRemaining = DefaultSecondsRemaining;
        IsRateExpired = false;

        ErrorMessage = string.Empty;

        CurrentStep = InitialStep;
        AmountText = string.Empty;
    }

    /// <summary>Executes the currency exchange using the locked rate; validates preconditions (non-expired lock, selected accounts) before calling the service.</summary>
    private Task ExecuteExchanges(object? unusedParameter)
    {
        try
        {
            ErrorMessage = string.Empty;

            if (IsRateExpired)
            {
                ErrorMessage = "The exchange rate has expired. Please lock a new rate.";
                return Task.CompletedTask;
            }

            if (SourceAccount == null || TargetAccount == null)
            {
                ErrorMessage = "Please select both source and target accounts.";
                return Task.CompletedTask;
            }

            if (lockedRate == null)
            {
                ErrorMessage = "Please lock a rate first.";
                return Task.CompletedTask;
            }

            var exchangeDto = new ExchangeDto
            {
                UserId = ServiceLocator.UserSessionService.CurrentUserId,
                SourceAccountId = SourceAccount!.Id,
                TargetAccountId = TargetAccount!.Id,
                SourceCurrency = SourceCurrency,
                TargetCurrency = TargetCurrency,
                SourceAmount = Amount,
                LockedRate = lockedRate!.Rate
            };

            var exchangeTransaction = exchangeService.ExecuteExchange(exchangeDto);

            timer?.Stop();

            TransactionReference = $"TX-{exchangeTransaction.Id}";
            CurrentStep = ExchangeResultStep;
        }
        catch (Exception executeChangesException)
        {
            ErrorMessage = executeChangesException.Message;
        }

        return Task.CompletedTask;
    }

    /// <summary>Populates the Accounts collection from the current user session.</summary>
    public Task LoadAccountsAsync()
    {
        Accounts.Clear();

        foreach (var account in ServiceLocator.UserSessionService.GetAccounts())
        {
            Accounts.Add(account);
        }

        return Task.CompletedTask;
    }

    /// <summary>Fetches the live rate for the currently selected currency pair and updates LiveRate.</summary>
    private Task LoadRatesAsync(object? unusedParameter)
    {
        try
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(SourceCurrency) ||
                string.IsNullOrWhiteSpace(TargetCurrency))
            {
                return Task.CompletedTask;
            }

            LiveRate = exchangeService.GetRate(SourceCurrency, TargetCurrency);
        }
        catch (Exception executeException)
        {
            ErrorMessage = executeException.Message;
        }

        return Task.CompletedTask;
    }

    /// <summary>Recomputes LiveRate, Commission, and TargetAmount whenever the source/target currencies or amount change.</summary>
    private void Recalculate()
    {
        try
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(SourceCurrency) ||
                string.IsNullOrWhiteSpace(TargetCurrency) ||
                Amount <= MinimumAmount)
            {
                return;
            }

            if (SourceCurrency == TargetCurrency)
            {
                LiveRate = EqualCurrencyRate;
                Commission = NoCommission;
                TargetAmount = Amount;
                return;
            }

            LiveRate = exchangeService.GetRate(SourceCurrency, TargetCurrency);
            Commission = exchangeService.CalculateCommission(Amount);
            TargetAmount = exchangeService.CalculateTargetAmount(Amount, LiveRate);
        }
        catch (Exception recalculateException)
        {
            ErrorMessage = recalculateException.Message;
        }
    }

    /// <summary>Locks the current exchange rate for the user, advances the wizard to the locked-rate step, and starts the 30-second countdown timer.</summary>
    private void LockRate(object? commandParameter)
    {
        try
        {
            ErrorMessage = string.Empty;

            lockedRate = exchangeService.LockRate(ServiceLocator.UserSessionService.CurrentUserId, SourceCurrency, TargetCurrency);

            CurrentStep = LockedRateStep;

            StartCountdownTimer();
        }
        catch (Exception lockRateException)
        {
            ErrorMessage = lockRateException.Message;
        }
    }

    /// <summary>Starts a UI-thread DispatcherTimer that ticks every second, updating SecondsRemaining and setting IsRateExpired when the lock window elapses.</summary>
    private void StartCountdownTimer()
    {
        if (lockedRate == null)
        {
            return;
        }

        IsRateExpired = false;

        timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(CountdownTickSeconds)
        };

        timer.Tick += (timerSender, timerEventArgs) =>
        {
            SecondsRemaining = lockedRate.GetSecondsRemaining();

            if (SecondsRemaining <= SecondsRemainingDeadline)
            {
                timer.Stop();
                IsRateExpired = true;
            }
        };

        timer.Start();
    }
}