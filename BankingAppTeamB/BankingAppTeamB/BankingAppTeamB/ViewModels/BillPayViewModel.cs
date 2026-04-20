using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BankingAppTeamB.Commands;
using BankingAppTeamB.Configuration;
using BankingAppTeamB.Mocks;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Services;
using BankingAppTeamB.Views;
using Microsoft.UI.Xaml;

namespace BankingAppTeamB.ViewModels
{
    public class BillPayViewModel : ViewModelBase
    {
        private const int SelectBillerStep = 1;
        private const int PaymentDetailsStep = 2;
        private const int TwoFactorAuthenticationStep = 3;
        private const int ReviewAndConfirmStep = 4;
        private const int PaymentResultStep = 5;
        private const int MinimumTwoFactorToken = 100000;
        private const int MaximumTwoFactorTokenExclusive = 1000000;

        private readonly IBillPaymentService billPaymentService;

        private int currentStep;
        private ObservableCollection<Biller> billers;
        private ObservableCollection<SavedBiller> savedBillers;
        private ObservableCollection<Account> accounts;
        private Biller? selectedBiller;
        private string searchQuery = string.Empty;
        private string? selectedCategory;
        private string billerReference = string.Empty;
        private decimal amount;
        private bool isPayInFull;
        private Account? selectedAccount;
        private decimal fee;
        private string receiptNumber = string.Empty;
        private string errorMessage = string.Empty;

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

        private string twoFAToken = string.Empty;
        public string TwoFAToken
        {
            get => twoFAToken;
            set => SetProperty(ref twoFAToken, value);
        }

        private const int TwoFaTokenMinValue = 100000;
        private const int TwoFaTokenMaxValue = 999999;

        private string GenerateTwoFAToken()
        {
            var random = new Random();
            return random.Next(MinimumTwoFactorToken, MaximumTwoFactorTokenExclusive).ToString();
        }

        private bool shouldSaveBiller;

        public BillPayViewModel(IBillPaymentService billPaymentService)
        {
            this.billPaymentService = billPaymentService;

            billers = new ObservableCollection<Biller>();
            savedBillers = new ObservableCollection<SavedBiller>();
            accounts = new ObservableCollection<Account>();
            currentStep = SelectBillerStep;

            SearchCommand = new RelayCommand(unusedParameter => ExecuteSearch());
            SelectBillerCommand = new RelayCommand(ExecuteSelectBiller);
            NextStepCommand = new RelayCommand(unusedParameter => ExecuteNextStep());
            BackCommand = new RelayCommand(unusedParameter => ExecuteBack());
            PayAnotherBillCommand = new RelayCommand(unusedParameter => ResetForm());
            PayBillCommand = new AsyncRelayCommand(unusedParameter => ExecutePayBillAsync());
            CancelCommand = new RelayCommand(unusedParameter => NavigationService.NavigateTo<TransferPage>());
        }

        public int CurrentStep
        {
            get => currentStep;
            set => SetProperty(ref currentStep, value);
        }

        public ObservableCollection<Biller> Billers
        {
            get => billers;
            set => SetProperty(ref billers, value);
        }

        public ObservableCollection<SavedBiller> SavedBillers
        {
            get => savedBillers;
            set
            {
                if (SetProperty(ref savedBillers, value))
                {
                    OnPropertyChanged(nameof(HasSavedBillers));
                    OnPropertyChanged(nameof(SavedBillersVisibility));
                }
            }
        }

        public ObservableCollection<Account> Accounts
        {
            get => accounts;
            set => SetProperty(ref accounts, value);
        }

        public Biller? SelectedBiller
        {
            get => selectedBiller;
            set
            {
                if (SetProperty(ref selectedBiller, value))
                {
                    ApplySavedDefaultsForSelectedBiller();
                    OnPropertyChanged(nameof(SelectedBillerName));
                }
            }
        }

        public string SearchQuery
        {
            get => searchQuery;
            set => SetProperty(ref searchQuery, value);
        }

        public string? SelectedCategory
        {
            get => selectedCategory;
            set
            {
                if (SetProperty(ref selectedCategory, value))
                {
                    ExecuteSearch();
                }
            }
        }

        public string BillerReference
        {
            get => billerReference;
            set => SetProperty(ref billerReference, value);
        }

        public decimal Amount
        {
            get => amount;
            set
            {
                if (SetProperty(ref amount, value))
                {
                    OnPropertyChanged(nameof(ReviewAmountText));
                    OnPropertyChanged(nameof(Total));
                    OnPropertyChanged(nameof(TotalText));
                }
            }
        }

        public double AmountAsDouble
        {
            get => (double)amount;
            set => Amount = (decimal)value;
        }

        public bool IsPayInFull
        {
            get => isPayInFull;
            set => SetProperty(ref isPayInFull, value);
        }

        public Account? SelectedAccount
        {
            get => selectedAccount;
            set => SetProperty(ref selectedAccount, value);
        }

        public decimal Fee
        {
            get => fee;
            set
            {
                if (SetProperty(ref fee, value))
                {
                    OnPropertyChanged(nameof(ReviewFeeText));
                    OnPropertyChanged(nameof(Total));
                    OnPropertyChanged(nameof(TotalText));
                }
            }
        }

        public string ReceiptNumber
        {
            get => receiptNumber;
            set => SetProperty(ref receiptNumber, value);
        }

        public string ErrorMessage
        {
            get => errorMessage;
            set
            {
                if (SetProperty(ref errorMessage, value))
                {
                    OnPropertyChanged(nameof(ErrorMessageVisibility));
                }
            }
        }

        public bool ShouldSaveBiller
        {
            get => shouldSaveBiller;
            set => SetProperty(ref shouldSaveBiller, value);
        }

        public bool HasSavedBillers => SavedBillers != null && SavedBillers.Count > 0;

        public Visibility SavedBillersVisibility =>
            HasSavedBillers ? Visibility.Visible : Visibility.Collapsed;

        public Visibility ErrorMessageVisibility =>
            string.IsNullOrWhiteSpace(ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;

        public string SelectedBillerName =>
            SelectedBiller?.Name ?? "No biller selected";

        public string ReviewAmountText =>
            Amount > 0 ? $"{Amount:0.00} RON" : "No amount entered";

        public string ReviewFeeText =>
            $"{Fee:0.00} RON";

        public decimal Total => Amount + Fee;

        public string TotalText => $"{Total:0.00} RON";

        public ICommand SearchCommand { get; }
        public ICommand SelectBillerCommand { get; }
        public ICommand NextStepCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand PayAnotherBillCommand { get; }
        public ICommand PayBillCommand { get; }
        public ICommand CancelCommand { get; }

        public async Task LoadAsync()
        {
            try
            {
                ErrorMessage = string.Empty;
                ResetFormStateOnly();

                var directory = await Task.Run(() => billPaymentService.GetBillerDirectory(null));
                Billers = new ObservableCollection<Biller>(directory);

                var saved = await Task.Run(() => billPaymentService.GetSavedBillers(ServiceLocator.UserSessionService.CurrentUserId));
                SavedBillers = new ObservableCollection<SavedBiller>(saved);

                Accounts = new ObservableCollection<Account>(UserSession.GetAccounts());
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load data: {ex.Message}";
            }
        }

        private void ExecuteSearch()
        {
            try
            {
                ErrorMessage = string.Empty;

                var results = billPaymentService.SearchBillers(
                    SearchQuery ?? string.Empty,
                    SelectedCategory);

                Billers = new ObservableCollection<Biller>(results);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Search failed: {ex.Message}";
            }
        }

        private void ExecuteSelectBiller(object? parameter)
        {
            ErrorMessage = string.Empty;

            if (parameter is Biller biller)
            {
                SelectedBiller = biller;
                CurrentStep = PaymentDetailsStep;
                return;
            }

            if (parameter is SavedBiller savedBiller && savedBiller.Biller != null)
            {
                SelectedBiller = savedBiller.Biller;

                if (!string.IsNullOrWhiteSpace(savedBiller.DefaultReference))
                {
                    BillerReference = savedBiller.DefaultReference!;
                }

                CurrentStep = PaymentDetailsStep;
            }
        }

        private void ExecuteNextStep()
        {
            ErrorMessage = string.Empty;

            if (CurrentStep == SelectBillerStep)
            {
                if (SelectedBiller == null)
                {
                    ErrorMessage = "Please select a biller.";
                    return;
                }

                CurrentStep = PaymentDetailsStep;
                return;
            }

            if (CurrentStep == PaymentDetailsStep)
            {
                if (SelectedBiller == null)
                {
                    ErrorMessage = "Please select a biller.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(BillerReference))
                {
                    ErrorMessage = "Please enter a biller reference.";
                    return;
                }

                if (SelectedAccount == null)
                {
                    ErrorMessage = "Please select a source account.";
                    return;
                }

                if (Amount <= 0)
                {
                    ErrorMessage = "Please enter a valid amount.";
                    return;
                }

                Fee = billPaymentService.CalculateFee(Amount);
                Requires2FA = billPaymentService.Requires2FA(Amount);
                CurrentStep = Requires2FA ? TwoFactorAuthenticationStep : ReviewAndConfirmStep;
                return;
            }

            if (CurrentStep == TwoFactorAuthenticationStep)
            {
                if (!Is2FAConfirmed)
                {
                    ErrorMessage = "You must confirm the 2FA step.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(TwoFAToken))
                {
                    TwoFAToken = GenerateTwoFAToken();
                }

                CurrentStep = ReviewAndConfirmStep;
            }
        }

        private void ExecuteBack()
        {
            ErrorMessage = string.Empty;

            if (CurrentStep > SelectBillerStep)
            {
                if (CurrentStep == ReviewAndConfirmStep && Requires2FA)
                {
                    CurrentStep = TwoFactorAuthenticationStep;
                }
                else if (CurrentStep == ReviewAndConfirmStep && !Requires2FA)
                {
                    CurrentStep = PaymentDetailsStep;
                }
                else
                {
                    CurrentStep--;
                }
            }
        }

        private async Task ExecutePayBillAsync()
        {
            try
            {
                ErrorMessage = string.Empty;

                if (SelectedBiller == null)
                {
                    ErrorMessage = "Please select a biller.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(BillerReference))
                {
                    ErrorMessage = "Please enter a biller reference.";
                    return;
                }

                if (SelectedAccount == null)
                {
                    ErrorMessage = "Please select a source account.";
                    return;
                }

                if (Amount <= 0)
                {
                    ErrorMessage = "Please enter a valid amount.";
                    return;
                }

                var dto = new BillPaymentDto
                {
                    UserId = ServiceLocator.UserSessionService.CurrentUserId,
                    SourceAccountId = SelectedAccount.Id,
                    BillerId = SelectedBiller.Id,
                    BillerReference = BillerReference,
                    Amount = Amount,
                    IsPayInFull = false,
                    TwoFAToken = Requires2FA ? TwoFAToken : null
                };

                var result = await Task.Run(() => billPaymentService.PayBill(dto));

                if (ShouldSaveBiller)
                {
                    var alreadySaved = SavedBillers.Any(savedBillerEntry =>
                        savedBillerEntry.BillerId == SelectedBiller.Id &&
                        string.Equals(savedBillerEntry.DefaultReference, BillerReference, StringComparison.OrdinalIgnoreCase));

                    if (!alreadySaved)
                    {
                        await Task.Run(() => billPaymentService.SaveBiller(
                            ServiceLocator.UserSessionService.CurrentUserId,
                            SelectedBiller.Id,
                            BillerReference,
                            SelectedBiller.Name));

                        var savedBiller = new SavedBiller
                        {
                            BillerId = SelectedBiller.Id,
                            DefaultReference = BillerReference,
                            Nickname = SelectedBiller.Name,
                            Biller = SelectedBiller
                        };

                        SavedBillers.Add(savedBiller);
                    }
                }

                ReceiptNumber = result.ReceiptNumber;
                Fee = result.Fee;
                CurrentStep = PaymentResultStep;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Payment failed: {ex.Message}";
            }
        }

        private void ResetForm()
        {
            ErrorMessage = string.Empty;
            ResetFormStateOnly();
        }

        private void ResetFormStateOnly()
        {
            CurrentStep = SelectBillerStep;
            SelectedBiller = null;
            SearchQuery = string.Empty;
            SelectedCategory = null;
            BillerReference = string.Empty;
            Amount = 0;
            Fee = 0;
            ReceiptNumber = string.Empty;
            SelectedAccount = null;
            IsPayInFull = false;
            ShouldSaveBiller = false;
            Requires2FA = false;
            Is2FAConfirmed = false;
            TwoFAToken = string.Empty;
        }

        private void ApplySavedDefaultsForSelectedBiller()
        {
            if (SelectedBiller == null || SavedBillers == null || SavedBillers.Count == 0)
            {
                return;
            }

            var matchingSavedBiller = SavedBillers.FirstOrDefault(savedBillerEntry => savedBillerEntry.BillerId == SelectedBiller.Id);

            if (matchingSavedBiller != null &&
                string.IsNullOrWhiteSpace(BillerReference) &&
                !string.IsNullOrWhiteSpace(matchingSavedBiller.DefaultReference))
            {
                BillerReference = matchingSavedBiller.DefaultReference!;
            }
        }

        public override bool Equals(object? obj)
        {
            return obj is BillPayViewModel model &&
                   isPayInFull == model.isPayInFull;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(isPayInFull);
        }
    }
}

