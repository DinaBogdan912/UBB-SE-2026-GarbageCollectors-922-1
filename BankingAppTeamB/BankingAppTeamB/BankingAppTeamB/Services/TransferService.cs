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

        public Transfer ExecuteTransfer(TransferDto dto)
        {
            if (!ValidateIBAN(dto.RecipientIBAN))
            {
                throw new InvalidOperationException("Recipient IBAN is invalid.");
            }

            var context = new PipelineContext
            {
                UserId = dto.UserId,
                SourceAccountId = dto.SourceAccountId,
                Amount = dto.Amount,
                Currency = dto.Currency,
                Type = "Transfer",
                Fee = 0,
                CounterpartyName = dto.RecipientName,
                RelatedEntityType = "Transfer",
                RelatedEntityId = 0
            };

            var transaction = transactionPipelineService.RunPipeline(context, dto.TwoFAToken);

            var transfer = new Transfer
            {
                UserId = dto.UserId,
                SourceAccountId = dto.SourceAccountId,
                TransactionId = transaction.Id,
                RecipientName = dto.RecipientName,
                RecipientIBAN = dto.RecipientIBAN,
                RecipientBankName = GetBankNameFromIBAN(dto.RecipientIBAN),
                Amount = dto.Amount,
                Currency = dto.Currency,
                Fee = 0,
                Reference = dto.Reference,
                Status = TransferStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            transferRepository.Add(transfer);

            var beneficiaries = beneficiaryRepository.GetByUserId(dto.UserId);
            var match = beneficiaries.Find(beneficiary => beneficiary.IBAN == dto.RecipientIBAN);
            if (match != null)
            {
                match.TransferCount++;
                match.TotalAmountSent += dto.Amount;
                match.LastTransferDate = DateTime.UtcNow;
                beneficiaryRepository.Update(match);
            }

            return transfer;
        }

        public bool ValidateIBAN(string iban)
        {
            return IbanValidator.Validate(iban);
        }

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
                _ => "International Bank"
            };
        }

        public FxPreview GetFxPreview(string sourceCurrency, string targetCurrency, decimal amount)
        {
            if (sourceCurrency.Equals(targetCurrency, StringComparison.OrdinalIgnoreCase))
            {
                return new FxPreview { Rate = 1, ConvertedAmount = amount };
            }

            if (exchangeService == null)
            {
                return new FxPreview { Rate = 1, ConvertedAmount = amount };
            }

            var rates = exchangeService.GetLiveRates();
            string pair = $"{sourceCurrency.ToUpper()}/{targetCurrency.ToUpper()}";

            if (!rates.ContainsKey(pair))
            {
                return new FxPreview { Rate = 1, ConvertedAmount = amount };
            }

            decimal rate = rates[pair];
            return new FxPreview
            {
                Rate = rate,
                ConvertedAmount = Math.Round(amount * rate, 2)
            };
        }

        public List<Transfer> GetHistory(int userId)
        {
            return transferRepository.GetByUserId(userId);
        }

        public bool Requires2FA(decimal amount)
        {
            return amount >= TwoFaAmountThreshold;
        }
    }
}
