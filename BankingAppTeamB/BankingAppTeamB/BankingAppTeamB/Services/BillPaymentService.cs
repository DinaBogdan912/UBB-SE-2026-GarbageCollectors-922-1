using System;
using System.Collections.Generic;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Repositories;

namespace BankingAppTeamB.Services
{
    public class BillPaymentService : IBillPaymentService
    {
        private const decimal SmallPaymentThreshold = 100m;
        private const decimal SmallPaymentFee = 0.50m;
        private const decimal StandardPaymentFee = 1.00m;
        private const decimal TwoFaAmountThreshold = 1000m;
        private const int ReceiptUniqueSuffixLength = 6;
        private const int NoRelatedEntityId = 0;

        private readonly IBillPaymentRepository billPaymentRepository;
        private readonly ITransactionPipelineService transactionPipelineService;

        public BillPaymentService(IBillPaymentRepository billPaymentRepository, ITransactionPipelineService transactionPipelineService)
        {
            this.billPaymentRepository = billPaymentRepository;
            this.transactionPipelineService = transactionPipelineService;
        }

        /// <summary>Works out the fee - up to 100 costs 0.50, above that costs 1.00.</summary>
        public decimal CalculateFee(decimal amount)
        {
            return amount <= SmallPaymentThreshold ? SmallPaymentFee : StandardPaymentFee;
        }

        /// <summary>Gets active billers, and if you pass a category it only returns that group.</summary>
        public List<Biller> GetBillerDirectory(string? category)
        {
            if (category == null)
            {
                return billPaymentRepository.GetAllBillers(isActive: true);
            }

            return billPaymentRepository.SearchBillers(string.Empty, category, isActive: true);
        }

        /// <summary>Searches for active billers by name.</summary>
        public List<Biller> SearchBillers(string query)
        {
            return billPaymentRepository.SearchBillers(query, null, isActive: true);
        }

        /// <summary>Searches active billers by name and can narrow it to one category.</summary>
        public List<Biller> SearchBillers(string query, string? category)
        {
            return billPaymentRepository.SearchBillers(query ?? string.Empty, category, isActive: true);
        }

        /// <summary>Gets the billers a user saved for quick payments.</summary>
        public List<SavedBiller> GetSavedBillers(int userId)
        {
            return billPaymentRepository.GetSavedBillers(userId);
        }

        /// <summary>Saves a biller to the user's favorites list.</summary>
        public void SaveBiller(int userId, int billerId, string? nickname, string? defaultReference)
        {
            var savedBiller = new SavedBiller
            {
                UserId = userId,
                BillerId = billerId,
                Nickname = nickname,
                DefaultReference = defaultReference,
                CreatedAt = DateTime.UtcNow
            };

            billPaymentRepository.SaveBiller(savedBiller);
        }

        /// <summary>Removes a saved biller from favorites.</summary>
        public void RemoveSavedBiller(int savedBillerId)
        {
            billPaymentRepository.DeleteSavedBiller(savedBillerId);
        }

        /// <summary>Returns true when the amount is 1000 or more, so 2FA is needed.</summary>
        public bool Requires2FA(decimal amount)
        {
            return amount >= TwoFaAmountThreshold;
        }

        /// <summary>Creates a unique receipt number like RCP-20260421-ABC123.</summary>
        private string GenerateReceiptNumber()
        {
            const int receiptSuffixLength = ReceiptUniqueSuffixLength;
            string uniqueSuffix = Guid.NewGuid().ToString("N")[..receiptSuffixLength].ToUpper();
            return $"RCP-{DateTime.UtcNow:yyyyMMdd}-{uniqueSuffix}";
        }

        /// <summary>Loads a biller by ID and throws a clear error if it does not exist.</summary>
        private Biller GetRequiredBiller(int billerId)
        {
            try
            {
                return billPaymentRepository.GetBillerById(billerId)
                    ?? throw new InvalidOperationException($"Biller with ID {billerId} does not exist.");
            }
            catch (KeyNotFoundException ex)
            {
                throw new InvalidOperationException($"Biller with ID {billerId} does not exist.", ex);
            }
        }

        /// <summary>Handles the full bill payment flow - validates biller, calculates fee, runs pipeline, and saves payment.</summary>
        public BillPayment PayBill(BillPaymentDto dto)
        {
            var biller = GetRequiredBiller(dto.BillerId);

            decimal fee = CalculateFee(dto.Amount);

            var context = new PipelineContext
            {
                UserId = dto.UserId,
                SourceAccountId = dto.SourceAccountId,
                Amount = dto.Amount,
                Currency = "RON",
                Type = "BillPayment",
                Fee = fee,
                CounterpartyName = biller.Name,
                RelatedEntityType = "BillPayment",
                RelatedEntityId = NoRelatedEntityId
            };

            var transaction = transactionPipelineService.RunPipeline(context, dto.TwoFAToken);

            var billPayment = new BillPayment
            {
                UserId = dto.UserId,
                SourceAccountId = dto.SourceAccountId,
                BillerId = dto.BillerId,
                TransactionId = transaction.Id,
                BillerReference = dto.BillerReference,
                Amount = dto.Amount,
                Fee = fee,
                ReceiptNumber = GenerateReceiptNumber(),
                Status = PaymentStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            return billPaymentRepository.Add(billPayment);
        }
    }
}