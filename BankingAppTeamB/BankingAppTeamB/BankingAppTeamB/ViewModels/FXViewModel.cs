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
                    Amount = parsed;   // this will trigger Recalculate()
                }
                else
                {
                    Amount = 0;
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

    private string transactionRef = string.Empty;

    public string TransactionRef
    {
        get => transactionRef;
        set => SetProperty(ref transactionRef, value);
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

        // Load mock accounts immediately
        _ = LoadAccountsAsync();
    }

    private void Cancel(object? unusedParameter)
    {
        timer?.Stop();
        Reset(null);
    }

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

        Amount = 0;
        LiveRate = 0;
        Commission = 0;
        TargetAmount = 0;

        SecondsRemaining = 0;
        IsRateExpired = false;

        ErrorMessage = string.Empty;

        CurrentStep = 1;
        AmountText = string.Empty;
    }

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

            var dto = new ExchangeDto
            {
                UserId = ServiceLocator.UserSessionService.CurrentUserId,
                SourceAccountId = SourceAccount!.Id,
                TargetAccountId = TargetAccount!.Id,
                SourceCurrency = SourceCurrency,
                TargetCurrency = TargetCurrency,
                SourceAmount = Amount,
                LockedRate = lockedRate!.Rate
            };

            var result = exchangeService.ExecuteExchange(dto);

            timer?.Stop();

            TransactionRef = $"TX-{result.Id}";

            CurrentStep = 5;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return Task.CompletedTask;
    }

    public Task LoadAccountsAsync()
    {
        Accounts.Clear();

        foreach (var account in ServiceLocator.UserSessionService.GetAccounts())
        {
            Accounts.Add(account);
        }

        return Task.CompletedTask;
    }

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
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return Task.CompletedTask;
    }

    private void Recalculate()
    {
        try
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(SourceCurrency) ||
                string.IsNullOrWhiteSpace(TargetCurrency) ||
                Amount <= 0)
            {
                return;
            }

            // Same currency shortcut
            if (SourceCurrency == TargetCurrency)
            {
                LiveRate = 1;
                Commission = 0;
                TargetAmount = Amount;
                return;
            }

            LiveRate = exchangeService.GetRate(SourceCurrency, TargetCurrency);
            Commission = exchangeService.CalculateCommission(Amount);
            TargetAmount = exchangeService.CalculateTargetAmount(Amount, LiveRate);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void LockRate(object? unusedParameter)
    {
        try
        {
            ErrorMessage = string.Empty;

            lockedRate = exchangeService.LockRate(ServiceLocator.UserSessionService.CurrentUserId, SourceCurrency, TargetCurrency);

            CurrentStep = 4;

            StartCountdownTimer();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private void StartCountdownTimer()
    {
        if (lockedRate == null)
        {
            return;
        }

        IsRateExpired = false;

        timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        timer.Tick += (timerSender, timerEventArgs) =>
        {
            SecondsRemaining = lockedRate.SecondsRemaining();

            if (SecondsRemaining <= 0)
            {
                timer.Stop();
                IsRateExpired = true;
            }
        };

        timer.Start();
    }
}