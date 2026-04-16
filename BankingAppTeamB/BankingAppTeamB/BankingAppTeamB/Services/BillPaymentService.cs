using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Repositories;
using System;
using System.Collections.Generic;

namespace BankingAppTeamB.Services
{
    public class BillPaymentService : IBillPaymentService
    {


        private const int SmallPaymentThreshold = 100;
        private const float SmallPaymentFee = 0.50f;
        private const float StandardPaymentFee = 1.00f;
        private const int TwoFaAmountThreshold = 1000;

        private readonly IBillPaymentRepository billPaymentRepository;
        private readonly ITransactionPipelineService transactionPipelineService;

        public BillPaymentService(IBillPaymentRepository billPaymentRepository, ITransactionPipelineService transactionPipelineService)
        {
            this.billPaymentRepository = billPaymentRepository;
            this.transactionPipelineService = transactionPipelineService;
        }

        public decimal CalculateFee(decimal amount)
        {
            var fee = amount <= SmallPaymentThreshold ? SmallPaymentFee : StandardPaymentFee;
            return Convert.ToDecimal(fee);
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
            return (float)amount >= TwoFaAmountThreshold;
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

            string receiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";

            var billPayment = new BillPayment
            {
                UserId = dto.UserId,
                SourceAccountId = dto.SourceAccountId,
                BillerId = dto.BillerId,
                TransactionId = transaction.Id,
                BillerReference = dto.BillerReference,
                Amount = dto.Amount,
                Fee = fee,
                ReceiptNumber = receiptNumber,
                Status = PaymentStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            return billPaymentRepository.Add(billPayment);
        }
    }
}