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

        public decimal CalculateFee(decimal amount)
        {
            return amount <= SmallPaymentThreshold ? SmallPaymentFee : StandardPaymentFee;
        }

        public List<Biller> GetBillerDirectory(string? category)
        {
            if (category == null)
            {
                return billPaymentRepository.GetAllBillers(isActive: true);
            }

            return billPaymentRepository.SearchBillers(string.Empty, category, isActive: true);
        }

        public List<Biller> SearchBillers(string query)
        {
            return billPaymentRepository.SearchBillers(query, null, isActive: true);
        }

        public List<Biller> SearchBillers(string query, string? category)
        {
            return billPaymentRepository.SearchBillers(query ?? string.Empty, category, isActive: true);
        }

        public List<SavedBiller> GetSavedBillers(int userId)
        {
            return billPaymentRepository.GetSavedBillers(userId);
        }

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

        public void RemoveSavedBiller(int savedBillerId)
        {
            billPaymentRepository.DeleteSavedBiller(savedBillerId);
        }

        public bool Requires2FA(decimal amount)
        {
            return amount >= TwoFaAmountThreshold;
        }

        private string GenerateReceiptNumber()
        {
            const int receiptSuffixLength = ReceiptUniqueSuffixLength;
            string uniqueSuffix = Guid.NewGuid().ToString("N")[..receiptSuffixLength].ToUpper();
            return $"RCP-{DateTime.UtcNow:yyyyMMdd}-{uniqueSuffix}";
        }
        public BillPayment PayBill(BillPaymentDto billPaymentDto)
        {
            var biller = billPaymentRepository.GetBillerById(billPaymentDto.BillerId);
            if (biller == null)
            {
                throw new InvalidOperationException($"Biller with ID {billPaymentDto.BillerId} does not exist.");
            }

            decimal fee = CalculateFee(billPaymentDto.Amount);

            var context = new PipelineContext
            {
                UserId = billPaymentDto.UserId,
                SourceAccountId = billPaymentDto.SourceAccountId,
                Amount = billPaymentDto.Amount,
                Currency = "RON",
                Type = "BillPayment",
                Fee = fee,
                CounterpartyName = biller.Name,
                RelatedEntityType = "BillPayment",
                RelatedEntityId = NoRelatedEntityId
            };

            var transaction = transactionPipelineService.RunPipeline(context, billPaymentDto.TwoFAToken);

            var billPayment = new BillPayment
            {
                UserId = billPaymentDto.UserId,
                SourceAccountId = billPaymentDto.SourceAccountId,
                BillerId = billPaymentDto.BillerId,
                TransactionId = transaction.Id,
                BillerReference = billPaymentDto.BillerReference,
                Amount = billPaymentDto.Amount,
                Fee = fee,
                ReceiptNumber = GenerateReceiptNumber(),
                Status = PaymentStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            return billPaymentRepository.Add(billPayment);
        }
    }
}