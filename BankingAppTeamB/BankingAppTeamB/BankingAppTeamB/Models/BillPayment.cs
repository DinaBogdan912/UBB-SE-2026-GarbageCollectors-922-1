using System;
using BankingAppTeamB.Models;
namespace BankingAppTeamB.Models
{
    public class BillPayment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SourceAccountId { get; set; }
        public int BillerId { get; set; }
        public int? TransactionId { get; set; }  
        public string BillerReference { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public string ReceiptNumber { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public Biller? Biller { get; set; }
    }
}