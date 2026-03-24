using BankingAppTeamB.Commands;
using BankingAppTeamB.Mocks;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Services;
using BankingAppTeamB.Views;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BankingAppTeamB.ViewModels
{
    public class BillPayViewModel : ViewModelBase
    {
        private readonly BillPaymentService _billPaymentService;

        private int _currentStep;
        private ObservableCollection<Biller> _billers;
        private ObservableCollection<SavedBiller> _savedBillers;
        private ObservableCollection<Account> _accounts;
        private Biller? _selectedBiller;
        private string _searchQuery = string.Empty;
        private string? _selectedCategory;
        private string _billerReference = string.Empty;
        private decimal _amount;
        private bool _isPayInFull;
        private Account? _selectedAccount;
        private decimal _fee;
        private string _receiptNumber = string.Empty;
        private string _errorMessage = string.Empty;

        public BillPayViewModel(BillPaymentService billPaymentService)
        {
            _billPaymentService = billPaymentService;

            _billers = new ObservableCollection<Biller>();
            _savedBillers = new ObservableCollection<SavedBiller>();
            _accounts = new ObservableCollection<Account>();

            _currentStep = 1;

            SearchCommand = new RelayCommand(_ => ExecuteSearch());
            NextStepCommand = new RelayCommand(_ => ExecuteNextStep());
            PayBillCommand = new AsyncRelayCommand(_ => ExecutePayBillAsync());
            CancelCommand = new RelayCommand(_ => NavigationService.NavigateTo<TransferPage>());

        }

        #region Properties (All using SetProperty)

        public int CurrentStep { get => _currentStep; set => SetProperty(ref _currentStep, value); }
        public ObservableCollection<Biller> Billers { get => _billers; set => SetProperty(ref _billers, value); }
        public ObservableCollection<SavedBiller> SavedBillers { get => _savedBillers; set => SetProperty(ref _savedBillers, value); }
        public ObservableCollection<Account> Accounts { get => _accounts; set => SetProperty(ref _accounts, value); }
        public Biller? SelectedBiller { get => _selectedBiller; set => SetProperty(ref _selectedBiller, value); }
        public string SearchQuery { get => _searchQuery; set => SetProperty(ref _searchQuery, value); }
        public string BillerReference { get => _billerReference; set => SetProperty(ref _billerReference, value); }
        public decimal Amount { get => _amount; set => SetProperty(ref _amount, value); }
        public bool IsPayInFull { get => _isPayInFull; set => SetProperty(ref _isPayInFull, value); }
        public Account? SelectedAccount { get => _selectedAccount; set => SetProperty(ref _selectedAccount, value); }
        public decimal Fee { get => _fee; set => SetProperty(ref _fee, value); }
        public string ReceiptNumber { get => _receiptNumber; set => SetProperty(ref _receiptNumber, value); }
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

        public string? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    ExecuteSearch(); 
                }
            }
        }

        #endregion

        #region Commands
        public ICommand SearchCommand { get; }
        public ICommand NextStepCommand { get; }
        public ICommand PayBillCommand { get; }
        public ICommand CancelCommand { get; }
        #endregion

        public async Task LoadAsync()
        {
            try
            {
                ErrorMessage = string.Empty;

                var directory = await Task.Run(() => _billPaymentService.GetBillerDirectory(null));
                Billers = new ObservableCollection<Biller>(directory);

                var saved = await Task.Run(() => _billPaymentService.GetSavedBillers(UserSession.CurrentUserId));
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
                var results = _billPaymentService.SearchBillers(SearchQuery, SelectedCategory);
                Billers = new ObservableCollection<Biller>(results);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }

        private void ExecuteNextStep()
        {
            ErrorMessage = string.Empty;

            if (CurrentStep == 1)
            {
                if (SelectedBiller == null)
                {
                    ErrorMessage = "Please select a biller.";
                    return;
                }
                CurrentStep = 2;
                return;
            }

            if (CurrentStep == 2)
            {
                if (!IsPayInFull && string.IsNullOrWhiteSpace(BillerReference))
                {
                    ErrorMessage = "Reference is required for partial payments.";
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

                Fee = _billPaymentService.CalculateFee(Amount);
                CurrentStep = 3;
            }
        }

        private async Task ExecutePayBillAsync()
        {
            try
            {
                ErrorMessage = string.Empty;

                var dto = new BillPaymentDto
                {
                    UserId = UserSession.CurrentUserId,
                    SourceAccountId = SelectedAccount!.Id,
                    BillerId = SelectedBiller!.Id,
                    BillerReference = BillerReference,
                    Amount = Amount,
                    IsPayInFull = IsPayInFull
                };

                var result = await Task.Run(() => _billPaymentService.PayBill(dto));

                ReceiptNumber = result.ReceiptNumber;
                CurrentStep = 4;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
        }
    }
}