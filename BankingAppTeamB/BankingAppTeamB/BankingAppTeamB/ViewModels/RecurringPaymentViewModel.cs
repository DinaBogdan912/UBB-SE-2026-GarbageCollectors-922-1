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

namespace BankingAppTeamB.ViewModels
{
    public class RecurringPaymentViewModel : ViewModelBase
    {
        private const int NoBillerSelected = 0;
        private const decimal NoAmount = 0m;

        private readonly IRecurringPaymentService recurringPaymentService;
        private readonly IBillPaymentService billPaymentService;

        private ObservableCollection<RecurringPayment> payments;
        private RecurringPayment? selectedPayment;
        private int selectedBillerId;
        private decimal amount;
        private RecurringFrequency frequency;
        private DateTime startDate;
        private DateTime? endDate;
        private string errorMessage;

        private ObservableCollection<Account> accounts;
        private Account? selectedAccount;

        private ObservableCollection<Biller> billers;
        private Biller? selectedBiller;

        private ObservableCollection<RecurringFrequency> frequencies;

        public RecurringPaymentViewModel(IRecurringPaymentService recurringPaymentService, IBillPaymentService billPaymentService)
        {
            this.recurringPaymentService = recurringPaymentService;
            this.billPaymentService = billPaymentService;

            payments = new ObservableCollection<RecurringPayment>();
            accounts = new ObservableCollection<Account>(ServiceLocator.UserSessionService.GetAccounts());
            billers = new ObservableCollection<Biller>();
            frequencies = new ObservableCollection<RecurringFrequency>
            {
                RecurringFrequency.Weekly,
                RecurringFrequency.Monthly,
                RecurringFrequency.Quarterly,
            };

            selectedPayment = null;
            selectedBillerId = NoBillerSelected;
            amount = NoAmount;
            frequency = RecurringFrequency.Weekly;
            startDate = DateTime.Today;
            endDate = null;
            errorMessage = string.Empty;
            selectedAccount = null;
            selectedBiller = null;

            CreateCommand = new AsyncRelayCommand(unusedParameter => ExecuteCreateAsync());
            PauseCommand = new RelayCommand(ExecutePause);
            ResumeCommand = new RelayCommand(ExecuteResume);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        public ObservableCollection<RecurringPayment> Payments
        {
            get => payments;
            set => SetProperty(ref payments, value);
        }

        public RecurringPayment? SelectedPayment
        {
            get => selectedPayment;
            set => SetProperty(ref selectedPayment, value);
        }

        public int SelectedBillerId
        {
            get => selectedBillerId;
            set => SetProperty(ref selectedBillerId, value);
        }

        public decimal Amount
        {
            get => amount;
            set => SetProperty(ref amount, value);
        }

        public RecurringFrequency Frequency
        {
            get => frequency;
            set => SetProperty(ref frequency, value);
        }

        public DateTime StartDate
        {
            get => startDate;
            set => SetProperty(ref startDate, value);
        }

        public DateTime? EndDate
        {
            get => endDate;
            set => SetProperty(ref endDate, value);
        }

        public string ErrorMessage
        {
            get => errorMessage;
            set
            {
                if (SetProperty(ref errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                    OnPropertyChanged(nameof(ErrorMessageVisibility));
                }
            }
        }

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public Microsoft.UI.Xaml.Visibility ErrorMessageVisibility => HasError ?
            Microsoft.UI.Xaml.Visibility.Visible :
            Microsoft.UI.Xaml.Visibility.Collapsed;

        public ObservableCollection<Account> Accounts
        {
            get => accounts;
            set => SetProperty(ref accounts, value);
        }

        public Account? SelectedAccount
        {
            get => selectedAccount;
            set => SetProperty(ref selectedAccount, value);
        }

        public ObservableCollection<Biller> Billers
        {
            get => billers;
            set => SetProperty(ref billers, value);
        }

        public Biller? SelectedBiller
        {
            get => selectedBiller;
            set
            {
                if (SetProperty(ref selectedBiller, value))
                {
                    SelectedBillerId = value?.Id ?? NoBillerSelected;
                }
            }
        }

        public ObservableCollection<RecurringFrequency> Frequencies
        {
            get => frequencies;
            set => SetProperty(ref frequencies, value);
        }

        public ICommand CreateCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand ResumeCommand { get; }
        public ICommand CancelCommand { get; }

        /// <summary>Loads the user's recurring payments and the biller directory from the services and refreshes the corresponding collections.</summary>
        public async Task LoadAsync()
        {
            try
            {
                ErrorMessage = string.Empty;

                var payments = await Task.Run(() =>
                    recurringPaymentService.GetByUser(ServiceLocator.UserSessionService.CurrentUserId));

                var availableBillers = await Task.Run(() =>
                    billPaymentService.GetBillerDirectory(null));

                Payments = new ObservableCollection<RecurringPayment>(payments);
                Billers = new ObservableCollection<Biller>(availableBillers);
            }
            catch (Exception loadException)
            {
                ErrorMessage = $"Failed to load recurring payments: {loadException.Message}";
            }
        }

        /// <summary>Public facade that delegates to <see cref="ExecuteCreateAsync"/> for use in non-command contexts such as tests or code-behind.</summary>
        public Task CreateAsync()
        {
            return ExecuteCreateAsync();
        }

        /// <summary>Public facade that delegates to <see cref="ExecutePause"/> for use in non-command contexts.</summary>
        public void Pause(RecurringPayment? payment)
        {
            ExecutePause(payment);
        }

        /// <summary>Public facade that delegates to <see cref="ExecuteResume"/> for use in non-command contexts.</summary>
        public void Resume(RecurringPayment? payment)
        {
            ExecuteResume(payment);
        }

        /// <summary>Public facade that delegates to <see cref="ExecuteCancel"/> for use in non-command contexts.</summary>
        public void Cancel(RecurringPayment? payment)
        {
            ExecuteCancel(payment);
        }

        /// <summary>Validates form inputs, creates the recurring payment via the service on a background thread, and adds the result to the <see cref="Payments"/> collection.</summary>
        private async Task ExecuteCreateAsync()
        {
            try
            {
                ErrorMessage = string.Empty;

                if (SelectedBiller == null)
                {
                    ErrorMessage = "Please select a biller.";
                    return;
                }

                if (SelectedAccount == null)
                {
                    ErrorMessage = "Please select a source account.";
                    return;
                }

                if (Amount <= NoAmount)
                {
                    ErrorMessage = "Please enter a valid amount.";
                    return;
                }

                if (EndDate.HasValue && EndDate.Value.Date < StartDate.Date)
                {
                    ErrorMessage = "End date cannot be earlier than start date.";
                    return;
                }

                var recurringPaymentDto = new RecurringPaymentDto
                {
                    UserId = ServiceLocator.UserSessionService.CurrentUserId,
                    BillerId = SelectedBiller.Id,
                    SourceAccountId = SelectedAccount.Id,
                    Amount = Amount,
                    IsPayInFull = false,
                    Frequency = Frequency,
                    StartDate = StartDate,
                    EndDate = EndDate
                };

                var createdPayment = await Task.Run(() =>
                    recurringPaymentService.Create(recurringPaymentDto));

                Payments.Add(createdPayment);
                ClearForm();
            }
            catch (Exception createPaymentException)
            {
                ErrorMessage = $"Failed to create recurring payment: {createPaymentException.Message}";
            }
        }

        /// <summary>Pauses the recurring payment supplied as <paramref name="parameter"/> and updates its status in the observable collection.</summary>
        private void ExecutePause(object? parameter)
        {
            try
            {
                ErrorMessage = string.Empty;

                if (parameter is not RecurringPayment payment)
                {
                    ErrorMessage = "Please select a recurring payment to pause.";
                    return;
                }

                recurringPaymentService.Pause(payment.Id);

                var existingPayment = Payments.FirstOrDefault(paymentEntry => paymentEntry.Id == payment.Id);
                if (existingPayment != null)
                {
                    var index = Payments.IndexOf(existingPayment);
                    existingPayment.Status = PaymentStatus.Paused;
                    Payments[index] = existingPayment;
                }
            }
            catch (Exception executePauseException)
            {
                ErrorMessage = $"Failed to pause recurring payment: {executePauseException.Message}";
            }
        }

        /// <summary>Resumes the recurring payment supplied as <paramref name="parameter"/> and updates its status in the observable collection.</summary>
        private void ExecuteResume(object? parameter)
        {
            try
            {
                ErrorMessage = string.Empty;

                if (parameter is not RecurringPayment payment)
                {
                    ErrorMessage = "Please select a recurring payment to resume.";
                    return;
                }

                recurringPaymentService.Resume(payment.Id);

                var existingPayment = Payments.FirstOrDefault(paymentEntry => paymentEntry.Id == payment.Id);
                if (existingPayment != null)
                {
                    var index = Payments.IndexOf(existingPayment);
                    existingPayment.Status = PaymentStatus.Active;
                    Payments[index] = existingPayment;
                }
            }
            catch (Exception executeResumeException)
            {
                ErrorMessage = $"Failed to resume recurring payment: {executeResumeException.Message}";
            }
        }

        /// <summary>Cancels the recurring payment supplied as <paramref name="parameter"/> and updates its status in the observable collection.</summary>
        private void ExecuteCancel(object? parameter)
        {
            try
            {
                ErrorMessage = string.Empty;

                if (parameter is not RecurringPayment payment)
                {
                    ErrorMessage = "Please select a recurring payment to cancel.";
                    return;
                }

                recurringPaymentService.Cancel(payment.Id);

                var existingPayment = Payments.FirstOrDefault(paymentEntry => paymentEntry.Id == payment.Id);
                if (existingPayment != null)
                {
                    var index = Payments.IndexOf(existingPayment);
                    existingPayment.Status = PaymentStatus.Cancelled;
                    Payments[index] = existingPayment;
                }
            }
            catch (Exception executeCancelException)
            {
                ErrorMessage = $"Failed to cancel recurring payment: {executeCancelException.Message}";
            }
        }

        /// <summary>Resets all create-form fields to their initial values after a successful payment creation.</summary>
        private void ClearForm()
        {
            SelectedPayment = null;
            SelectedBiller = null;
            SelectedBillerId = NoBillerSelected;
            SelectedAccount = null;
            Amount = NoAmount;
            Frequency = RecurringFrequency.Weekly;
            StartDate = DateTime.Today;
            EndDate = null;
            ErrorMessage = string.Empty;
        }
    }
}