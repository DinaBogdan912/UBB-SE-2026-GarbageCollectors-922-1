using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Repositories;
using System;
using System.Collections.Generic;

namespace BankingAppTeamB.Services
{
    public class BillPaymentService : IBillPaymentService
    {


        private const decimal SmallPaymentThreshold = 100m;
        private const decimal SmallPaymentFee = 0.50m;
        private const decimal StandardPaymentFee = 1.00m;
        private const decimal TwoFaAmountThreshold = 1000m;

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
                return billPaymentRepository.GetAllBillers();

            return billPaymentRepository.SearchBillers(string.Empty, category);
        }

        public List<Biller> SearchBillers(string query)
        {
            return billPaymentRepository.SearchBillers(query, null);
        }

        public List<Biller> SearchBillers(string query, string? category)
        {
            return billPaymentRepository.SearchBillers(query ?? string.Empty, category);
        }

        public List<SavedBiller> GetSavedBillers(int userId)
        {
            return billPaymentRepository.GetSavedBillers(userId);
        }

        public void SaveBiller(int userId, int billerId, string? nickname, string? defaultRef)
        {
            var savedBiller = new SavedBiller
            {
                UserId = userId,
                BillerId = billerId,
                Nickname = nickname,
                DefaultReference = defaultRef,
                CreatedAt = DateTime.UtcNow
            };

            billPaymentRepository.SaveBiller(savedBiller);
        }

        public void RemoveSavedBiller(int id)
        {
            billPaymentRepository.DeleteSavedBiller(id);
        }

        public bool Requires2FA(decimal amount)
        {
            return amount >= TwoFaAmountThreshold;
        }


        private String GenerateReceiptId()
        {
            const int receiptSuffixLength = 6;
            string uniqueSuffix = Guid.NewGuid().ToString("N")[..receiptSuffixLength].ToUpper();
            return $"RCP-{DateTime.UtcNow:yyyyMMdd}-{uniqueSuffix}";
        }
        public BillPayment PayBill(BillPaymentDto dto)
        {
            var biller = billPaymentRepository.GetBillerById(dto.BillerId);
            if (biller == null)
                throw new InvalidOperationException($"Biller with ID {dto.BillerId} does not exist.");

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
                RelatedEntityId = 0
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
                ReceiptNumber = GenerateReceiptId(),
                Status = PaymentStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            return billPaymentRepository.Add(billPayment);
        }
    }
}