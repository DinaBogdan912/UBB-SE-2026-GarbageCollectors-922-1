using System;

namespace BankingAppTeamB.Models
{
    public class ExchangeTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SourceAccountId { get; set; }
        public int TargetAccountId { get; set; }
        public int? TransactionId { get; set; }
        public required string SourceCurrency { get; set; }
        public required string TargetCurrency { get; set; }
        public decimal SourceAmount { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal Commission { get; set; }
        public DateTime RateLockedAt { get; set; }
        public TransferStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public int GetId()
        {
            return Id;
        }
        public TransferStatus GetStatus()
        {
            return Status;
        }
        public void SetStatus(TransferStatus transferStatus)
        {
            Status = transferStatus;
        }
        public decimal GetExchangeRate()
        {
            return ExchangeRate;
        }
        public decimal GetTargetAmount()
        {
            return TargetAmount;
        }
    }
}
