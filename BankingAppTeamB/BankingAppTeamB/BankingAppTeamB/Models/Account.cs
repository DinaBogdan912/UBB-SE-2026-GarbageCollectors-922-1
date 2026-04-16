using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAppTeamB.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string IBAN { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
