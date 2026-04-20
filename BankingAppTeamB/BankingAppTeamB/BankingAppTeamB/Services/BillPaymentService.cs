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

        /// <summary>Returns the service fee for a bill payment: €0.50 for amounts ≤ €100, otherwise €1.00.</summary>
        public decimal CalculateFee(decimal amount)
        {
            return amount <= SmallPaymentThreshold ? SmallPaymentFee : StandardPaymentFee;
        }

        /// <summary>Returns all active billers; if <paramref name="category"/> is provided, filters to that category only.</summary>
        public List<Biller> GetBillerDirectory(string? category)
        {
            if (category == null)
            {
                return billPaymentRepository.GetAllBillers(isActive: true);
            }

            return billPaymentRepository.SearchBillers(string.Empty, category, isActive: true);
        }

        /// <summary>Searches active billers by name containing <paramref name="query"/> across all categories.</summary>
        public List<Biller> SearchBillers(string query)
        {
            return billPaymentRepository.SearchBillers(query, null, isActive: true);
        }

        /// <summary>Searches active billers by name and optionally narrows results to <paramref name="category"/>.</summary>
        public List<Biller> SearchBillers(string query, string? category)
        {
            return billPaymentRepository.SearchBillers(query ?? string.Empty, category, isActive: true);
        }

        /// <summary>Returns the saved billers (with biller details) for the given user.</summary>
        public List<SavedBiller> GetSavedBillers(int userId)
        {
            return billPaymentRepository.GetSavedBillers(userId);
        }

        /// <summary>Persists a biller as a favourite for <paramref name="userId"/> so it can be selected quickly on future payments.</summary>
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

        /// <summary>Removes a previously saved biller by its <paramref name="savedBillerId"/>.</summary>
        public void RemoveSavedBiller(int savedBillerId)
        {
            billPaymentRepository.DeleteSavedBiller(savedBillerId);
        }

        /// <summary>Returns <see langword="true"/> when <paramref name="amount"/> meets or exceeds the €1 000 threshold that mandates two-factor authentication.</summary>
        public bool Requires2FA(decimal amount)
        {
            return amount >= TwoFaAmountThreshold;
        }

        /// <summary>Generates a unique receipt number in the format <c>RCP-yyyyMMdd-XXXXXX</c> where the suffix is the first 6 hex characters of a new GUID.</summary>
        private string GenerateReceiptNumber()
        {
            const int receiptSuffixLength = ReceiptUniqueSuffixLength;
            string uniqueSuffix = Guid.NewGuid().ToString("N")[..receiptSuffixLength].ToUpper();
            return $"RCP-{DateTime.UtcNow:yyyyMMdd}-{uniqueSuffix}";
        }

        /// <summary>Looks up the biller by <paramref name="billerId"/> and throws <see cref="InvalidOperationException"/> if it does not exist.</summary>
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

        /// <summary>Runs the full payment pipeline (validate → authorise → debit → log) for the bill described by <paramref name="dto"/>, then persists and returns the <see cref="BillPayment"/> record.</summary>
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