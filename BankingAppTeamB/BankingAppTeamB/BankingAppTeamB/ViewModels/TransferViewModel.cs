using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BankingAppTeamB.Commands;
using BankingAppTeamB.Configuration;
using BankingAppTeamB.Mocks;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Services;
using BankingAppTeamB.ViewModels;

public class TransferViewModel : ViewModelBase
{
    private const int AccountSelectionStep = 1;
    private const int RecipientDetailsStep = 2;
    private const int AmountDetailsStep = 3;
    private const int TwoFactorAuthenticationStep = 4;
    private const int ReviewAndConfirmationStep = 5;
    private const int TransferCompletedStep = 6;
    private const int TransferErrorStep = 7;
    private const int MinimumTwoFactorToken = 100000;
    private const int MaximumTwoFactorTokenExclusive = 1000000;
    private const decimal ZeroAmount = 0m;
    private const decimal IdentityExchangeRate = 1m;
    private const string DefaultTransferCurrency = "EUR";

    private readonly ITransferService transferService;

    public TransferViewModel(ITransferService transferService)
    {
        this.transferService = transferService ?? throw new ArgumentNullException(nameof(transferService));

        Accounts = new ObservableCollection<Account>();
        CurrentStep = AccountSelectionStep;

        Currency = DefaultTransferCurrency;
        AmountText = string.Empty;

        NextStepCommand = new RelayCommand(unusedParameter => ExecuteNextStep());
        TransferCommand = new AsyncRelayCommand(unusedParameter => ExecuteTransferAsync());
        CancelCommand = new RelayCommand(unusedParameter => ExecuteCancel());
        SendAgainCommand = new RelayCommand(unusedParameter => ExecuteSendAgain());

        LoadAccounts();
    }

    public string SelectedAccountName => SelectedAccount?.AccountName ?? string.Empty;

    private int currentStep;
    public int CurrentStep
    {
        get => currentStep;
        set => SetProperty(ref currentStep, value);
    }

    private ObservableCollection<Account> accounts;
    public ObservableCollection<Account> Accounts
    {
        get => accounts;
        set => SetProperty(ref accounts, value);
    }

    private Account selectedAccount;
    public Account SelectedAccount
    {
        get => selectedAccount;
        set
        {
            SetProperty(ref selectedAccount, value);
            OnPropertyChanged(nameof(SelectedAccountName));
            UpdateFxPreview();
        }
    }

    private string recipientName;
    public string RecipientName
    {
        get => recipientName;
        set => SetProperty(ref recipientName, value);
    }

    private string recipientIBAN;
    public string RecipientIBAN
    {
        get => recipientIBAN;
        set
        {
            SetProperty(ref recipientIBAN, value);
            UpdateIBANValidation(value);
        }
    }

    private bool isIBANValid;
    public bool IsIBANValid
    {
        get => isIBANValid;
        set => SetProperty(ref isIBANValid, value);
    }

    private string bankName;
    public string BankName
    {
        get => bankName;
        set => SetProperty(ref bankName, value);
    }

    private decimal amount;
    public decimal Amount
    {
        get => amount;
        set
        {
            SetProperty(ref amount, value);
            UpdateFxPreview();
            UpdateRequires2FA();
        }
    }

    private string currency;
    public string Currency
    {
        get => currency;
        set
        {
            SetProperty(ref currency, value);
            UpdateFxPreview();
        }
    }

    private string fxPreviewText;
    public string FxPreviewText
    {
        get => fxPreviewText;
        set => SetProperty(ref fxPreviewText, value);
    }

    private string twoFAToken;
    public string TwoFAToken
    {
        get => twoFAToken;
        set => SetProperty(ref twoFAToken, value);
    }

    private bool requires2FA;
    public bool Requires2FA
    {
        get => requires2FA;
        set => SetProperty(ref requires2FA, value);
    }

    private bool is2FAConfirmed;
    public bool Is2FAConfirmed
    {
        get => is2FAConfirmed;
        set => SetProperty(ref is2FAConfirmed, value);
    }

    private string transactionRef;
    public string TransactionRef
    {
        get => transactionRef;
        set => SetProperty(ref transactionRef, value);
    }

    private string errorMessage;
    public string ErrorMessage
    {
        get => errorMessage;
        set
        {
            if (SetProperty(ref errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    private string amountText;
    public string AmountText
    {
        get => amountText;
        set
        {
            SetProperty(ref amountText, value);

            if (decimal.TryParse(value, out decimal parsed))
            {
                Amount = parsed;
            }
            else
            {
                Amount = 0;
            }
        }
    }

    public RelayCommand NextStepCommand { get; }
    public AsyncRelayCommand TransferCommand { get; }
    public RelayCommand CancelCommand { get; }
    public RelayCommand SendAgainCommand { get; }

    public void LoadAccounts()
    {
        try
        {
            var userAccounts = ServiceLocator.UserSessionService.GetAccounts();

            Accounts.Clear();

            if (userAccounts != null)
            {
                foreach (var account in userAccounts)
                {
                    Accounts.Add(account);
                }

                if (Accounts.Count > 0)
                {
                    SelectedAccount = Accounts[0];
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private string GenerateTwoFAToken()
    {
        var random = new Random();
        return random.Next(MinimumTwoFactorToken, MaximumTwoFactorTokenExclusive).ToString();
    }

    private void ExecuteNextStep()
    {
        ErrorMessage = string.Empty;

        if (CurrentStep == RecipientDetailsStep && !IsIBANValid)
        {
            ErrorMessage = "Invalid IBAN format.";
            CurrentStep = TransferErrorStep;
            return;
        }

        if (CurrentStep == AmountDetailsStep)
        {
            if (Amount <= ZeroAmount)
            {
                ErrorMessage = "The amount must be greater than 0.";
                CurrentStep = TransferErrorStep;
                return;
            }

            CurrentStep = Requires2FA ? TwoFactorAuthenticationStep : ReviewAndConfirmationStep;
            return;
        }

        if (CurrentStep == TwoFactorAuthenticationStep)
        {
            if (!Is2FAConfirmed)
            {
                ErrorMessage = "You must confirm the 2FA step.";
                CurrentStep = TransferErrorStep;
                return;
            }

            if (Requires2FA && string.IsNullOrWhiteSpace(TwoFAToken))
            {
                TwoFAToken = GenerateTwoFAToken();
            }

            CurrentStep = ReviewAndConfirmationStep;
            return;
        }

        CurrentStep++;
    }

    private async Task ExecuteTransferAsync()
    {
        try
        {
            ErrorMessage = string.Empty;

            if (SelectedAccount == null)
            {
                throw new Exception("No account selected.");
            }

            var dto = new TransferDto
            {
                UserId = ServiceLocator.UserSessionService.CurrentUserId,
                SourceAccountId = SelectedAccount.Id,
                RecipientName = RecipientName,
                RecipientIBAN = RecipientIBAN,
                Amount = Amount,
                Currency = Currency,
                TwoFAToken = Requires2FA ? TwoFAToken : null
            };

            var result = await Task.Run(() => transferService.ExecuteTransfer(dto));

            TransactionRef = result.TransactionId.HasValue
                ? $"TXN-{result.CreatedAt:yyyyMMdd}-{result.TransactionId:D4}"
                : result.Id.ToString();

            CurrentStep = TransferCompletedStep;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            CurrentStep = TransferErrorStep;
        }
    }

    private void ExecuteCancel()
    {
    }

    private void ExecuteSendAgain()
    {
        SelectedAccount = Accounts.Count > 0 ? Accounts[0] : null;
        RecipientName = string.Empty;
        RecipientIBAN = string.Empty;
        IsIBANValid = false;
        BankName = string.Empty;
        Amount = ZeroAmount;
        Currency = DefaultTransferCurrency;
        FxPreviewText = string.Empty;
        TwoFAToken = string.Empty;
        Requires2FA = false;
        Is2FAConfirmed = false;
        TransactionRef = string.Empty;
        ErrorMessage = string.Empty;
        AmountText = string.Empty;

        CurrentStep = AccountSelectionStep;
    }

    private void UpdateIBANValidation(string iban)
    {
        try
        {
            IsIBANValid = transferService.ValidateIBAN(iban);

            if (IsIBANValid)
            {
                BankName = transferService.GetBankNameFromIBAN(iban);
            }
            else
            {
                BankName = string.Empty;
            }
        }
        catch
        {
            IsIBANValid = false;
            BankName = string.Empty;
        }
    }

    private void UpdateFxPreview()
    {
        try
        {
            if (SelectedAccount == null || Amount <= 0 || string.IsNullOrWhiteSpace(Currency))
            {
                FxPreviewText = string.Empty;
                return;
            }

            var preview = transferService.GetFxPreview(
                SelectedAccount.Currency,
                Currency,
                Amount);

            if (preview.ExchangeRate == IdentityExchangeRate)
            {
                FxPreviewText = $"{Amount:F2} {Currency}";
            }
            else
            {
                FxPreviewText =
                    $"{Amount:F2} {SelectedAccount.Currency} → {preview.ConvertedAmount:F2} {Currency} (rate: {preview.ExchangeRate:F4})";
            }
        }
        catch
        {
            FxPreviewText = string.Empty;
        }
    }

    private void UpdateRequires2FA()
    {
        try
        {
            Requires2FA = transferService.Requires2FA(Amount);
        }
        catch
        {
            Requires2FA = false;
        }
    }
}