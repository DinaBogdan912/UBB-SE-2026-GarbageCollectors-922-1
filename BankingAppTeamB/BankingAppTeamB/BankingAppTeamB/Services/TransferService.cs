using System;
using System.Collections.Generic;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Repositories;

namespace BankingAppTeamB.Services
{
    public class TransferService : ITransferService
    {
        private const int IbanCountryCodeLength = 2;
        private const decimal TwoFaAmountThreshold = 1000m;
        private const string SwitchDefault = null;
        private const int DecimalRoundingPlaces = 2;
        private const int DefaultExchangeRate = 1;
        private const int NoFee = 0;
        private const int NoRelatedEntityId = 0;
        private readonly ITransferRepository transferRepository;
        private readonly IBeneficiaryRepository beneficiaryRepository;
        private readonly ITransactionPipelineService transactionPipelineService;
        private readonly IExchangeService? exchangeService;

        public TransferService(
            ITransferRepository transferRepository,
            IBeneficiaryRepository beneficiaryRepository,
            ITransactionPipelineService transactionPipelineService,
            IExchangeService? exchangeService = null)
        {
            this.transferRepository = transferRepository;
            this.beneficiaryRepository = beneficiaryRepository;
            this.transactionPipelineService = transactionPipelineService;
            this.exchangeService = exchangeService;
        }

        /// <summary>Validates the recipient IBAN, runs the transaction pipeline (debit source account), persists the transfer record, and updates beneficiary statistics if the recipient is a saved beneficiary.</summary>
        public Transfer ExecuteTransfer(TransferDto transferDto)
        {
            if (!ValidateIBAN(transferDto.RecipientIBAN))
            {
                throw new InvalidOperationException("Recipient IBAN is invalid.");
            }

            var pipelineContext = new PipelineContext
            {
                UserId = transferDto.UserId,
                SourceAccountId = transferDto.SourceAccountId,
                Amount = transferDto.Amount,
                Currency = transferDto.Currency,
                Type = "Transfer",
                Fee = NoFee,
                CounterpartyName = transferDto.RecipientName,
                RelatedEntityType = "Transfer",
                RelatedEntityId = NoRelatedEntityId
            };

            var transaction = transactionPipelineService.RunPipeline(pipelineContext, transferDto.TwoFAToken);

            var transfer = new Transfer
            {
                UserId = transferDto.UserId,
                SourceAccountId = transferDto.SourceAccountId,
                TransactionId = transaction.Id,
                RecipientName = transferDto.RecipientName,
                RecipientIBAN = transferDto.RecipientIBAN,
                RecipientBankName = GetBankNameFromIBAN(transferDto.RecipientIBAN),
                Amount = transferDto.Amount,
                Currency = transferDto.Currency,
                Fee = NoFee,
                Reference = transferDto.Reference,
                Status = TransferStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            transferRepository.Add(transfer);

            var beneficiaries = beneficiaryRepository.GetByUserId(transferDto.UserId);
            var match = beneficiaries.Find(beneficiary => beneficiary.IBAN == transferDto.RecipientIBAN);
            if (match != null)
            {
                match.TransferCount++;
                match.TotalAmountSent += transferDto.Amount;
                match.LastTransferDate = DateTime.UtcNow;
                beneficiaryRepository.Update(match);
            }

            return transfer;
        }

        /// <summary>Returns true when iban passes the structural IBAN validation rules.</summary>
        public bool ValidateIBAN(string iban)
        {
            return IbanValidator.Validate(iban);
        }

        /// <summary>Infers a human-readable bank name from the two-letter country code at the start of iban.</summary>
        public string GetBankNameFromIBAN(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban) || iban.Length < IbanCountryCodeLength)
            {
                return "Unknown Bank";
            }

            string countryCode = iban.Substring(0, IbanCountryCodeLength).ToUpper();
            return countryCode switch
            {
                "RO" => "Romanian Bank",
                "DE" => "German Bank",
                "GB" => "UK Bank",
                "FR" => "French Bank",
                "US" => "US Bank",
                SwitchDefault => "International Bank"
            };
        }

        /// <summary>Returns a preview of the converted amount and rate for a cross-currency transfer; returns a 1:1 identity preview when currencies match or no exchange service is wired.</summary>
        public FxPreview GetFxPreview(string sourceCurrency, string targetCurrency, decimal amount)
        {
            if (sourceCurrency.Equals(targetCurrency, StringComparison.OrdinalIgnoreCase))
            {
                return new FxPreview { ExchangeRate = DefaultExchangeRate, ConvertedAmount = amount };
            }

            if (exchangeService == null)
            {
                return new FxPreview { ExchangeRate = DefaultExchangeRate, ConvertedAmount = amount };
            }

            var rates = exchangeService.GetLiveRates();
            string pair = $"{sourceCurrency.ToUpper()}/{targetCurrency.ToUpper()}";

            if (!rates.ContainsKey(pair))
            {
                return new FxPreview { ExchangeRate = DefaultExchangeRate, ConvertedAmount = amount };
            }

            decimal rate = rates[pair];
            return new FxPreview
            {
                ExchangeRate = rate,
                ConvertedAmount = Math.Round(amount * rate, DecimalRoundingPlaces)
            };
        }

        /// <summary>Returns all transfer records for userId, ordered by creation date descending.</summary>
        public List<Transfer> GetHistory(int userId)
        {
            return transferRepository.GetByUserId(userId);
        }

        /// <summary>Returns true when amount meets or exceeds the 1 000 threshold that mandates two-factor authentication.</summary>
        public bool Requires2FA(decimal amount)
        {
            return amount >= TwoFaAmountThreshold;
        }
    }
}
