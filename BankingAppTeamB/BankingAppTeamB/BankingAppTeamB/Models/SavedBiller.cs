using System;
using BankingAppTeamB.Models;
namespace BankingAppTeamB.Models
{
    public class SavedBiller
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BillerId { get; set; }
        public string? Nickname { get; set; }
        public string? DefaultReference { get; set; }
        public DateTime CreatedAt { get; set; }

        public Biller? Biller { get; set; }
    }
}