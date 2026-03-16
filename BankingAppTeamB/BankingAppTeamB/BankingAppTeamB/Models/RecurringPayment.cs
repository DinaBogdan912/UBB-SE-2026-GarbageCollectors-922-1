using System;
using BankingAppTeamB.Models;
namespace BankingAppTeamB.Models
{
    public class RecurringPayment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BillerId { get; set; }
        public int SourceAccountId { get; set; }
        public decimal Amount { get; set; }
        public bool IsPayInFull { get; set; }
        public RecurringFrequency Frequency { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime NextExecutionDate { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}