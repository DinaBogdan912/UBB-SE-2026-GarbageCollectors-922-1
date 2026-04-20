using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BankingAppTeamB.Commands;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Services;
using BankingAppTeamB.Views;

namespace BankingAppTeamB.ViewModels
{
    public class BeneficiariesViewModel : ViewModelBase
    {
        private const int PlaceholderSourceAccountId = 0;

        private readonly IBeneficiaryService beneficiaryService;

        // Hardcoded for this example.
        private readonly int currentUserId = 1;

        // Backing Fields
        private ObservableCollection<Beneficiary> beneficiaries = new ObservableCollection<Beneficiary>();
        private Beneficiary? selectedBeneficiary;
        private string newName = string.Empty;
        private string newIBAN = string.Empty;
        private string newBankName = string.Empty;
        private string errorMessage = string.Empty;
        private bool isAddFormVisible;

        // Properties
        public ObservableCollection<Beneficiary> Beneficiaries
        {
            get => beneficiaries;
            set => SetProperty(ref beneficiaries, value);
        }

        public Beneficiary? SelectedBeneficiary
        {
            get => selectedBeneficiary;
            set => SetProperty(ref selectedBeneficiary, value);
        }

        public string NewName
        {
            get => newName;
            set => SetProperty(ref newName, value);
        }

        public string NewIBAN
        {
            get => newIBAN;
            set => SetProperty(ref newIBAN, value);
        }

        public string NewBankName
        {
            get => newBankName;
            set => SetProperty(ref newBankName, value);
        }

        public string ErrorMessage
        {
            get => errorMessage;
            set => SetProperty(ref errorMessage, value);
        }

        public bool IsAddFormVisible
        {
            get => isAddFormVisible;
            set => SetProperty(ref isAddFormVisible, value);
        }

        // Commands
        public AsyncRelayCommand AddCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand ShowAddFormCommand { get; }
        public RelayCommand UseForTransferCommand { get; }

        // Constructor
        public BeneficiariesViewModel(IBeneficiaryService beneficiaryService)
        {
            this.beneficiaryService = beneficiaryService;

            // Initialize Commands
            AddCommand = new AsyncRelayCommand(_ => AddBeneficiaryAsync());
            DeleteCommand = new RelayCommand(commandParameter => DeleteBeneficiary(commandParameter as Beneficiary));
            ShowAddFormCommand = new RelayCommand(_ => ShowAddForm());
            UseForTransferCommand = new RelayCommand(commandParameter => UseForTransfer(commandParameter as Beneficiary));
        }
        // Methods
        public async Task LoadAsync()
        {
            var data = beneficiaryService.GetByUser(currentUserId);

            Beneficiaries.Clear();
            foreach (var beneficiary in data)
            {
                Beneficiaries.Add(beneficiary);
            }

            await Task.CompletedTask;
        }

        private async Task AddBeneficiaryAsync()
        {
            ErrorMessage = string.Empty;

            try
            {
                beneficiaryService.Add(NewName, NewIBAN, currentUserId);

                NewName = string.Empty;
                NewIBAN = string.Empty;
                NewBankName = string.Empty;
                IsAddFormVisible = false;

                await LoadAsync();
            }
            catch (ArgumentException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception)
            {
                ErrorMessage = "An unexpected error occurred while saving the beneficiary.";
            }
        }

        private void DeleteBeneficiary(Beneficiary beneficiary)
        {
            if (beneficiary == null)
            {
                return;
            }

            beneficiaryService.Delete(beneficiary.Id);
            Beneficiaries.Remove(beneficiary);
        }

        private void ShowAddForm()
        {
            NewName = string.Empty;
            NewIBAN = string.Empty;
            NewBankName = string.Empty;
            ErrorMessage = string.Empty;
            IsAddFormVisible = true;
        }

        private void UseForTransfer(Beneficiary beneficiary)
        {
            if (beneficiary == null)
            {
                return;
            }

            TransferDto transferDto = beneficiaryService.BuildTransferDtoFrom(
                beneficiary,
                PlaceholderSourceAccountId,
                currentUserId);

            NavigationService.NavigateTo<TransferPage>(transferDto);
        }
    }
}
