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

        /// <summary>Returns the database identifier of this exchange transaction.</summary>
        public int GetId()
        {
            return Id;
        }

        /// <summary>Returns the current processing status of this transaction.</summary>
        public TransferStatus GetStatus()
        {
            return Status;
        }

        /// <summary>Updates the processing status of this transaction.</summary>
        public void SetStatus(TransferStatus transferStatus)
        {
            Status = transferStatus;
        }

        /// <summary>Returns the exchange rate applied when this transaction was executed.</summary>
        public decimal GetExchangeRate()
        {
            return ExchangeRate;
        }

        /// <summary>Returns the amount credited to the target account after commission deduction.</summary>
        public decimal GetTargetAmount()
        {
            return TargetAmount;
        }
    }
}
