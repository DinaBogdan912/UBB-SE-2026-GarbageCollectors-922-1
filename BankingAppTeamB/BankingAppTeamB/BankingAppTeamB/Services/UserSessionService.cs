using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Services
{
    // This is a mock implementation of IUserSessionService for demonstration purposes.
    public class UserSessionService : IUserSessionService
    {
        public int CurrentUserId { get; private set; } = 1;
        public string CurrentUserName { get; private set; } = "Ion Popescu";
        public List<Account> GetAccounts() =>
        [
            new Account { Id = 1, IBAN = "RO49AAAA1B31007593840000", Currency = "EUR", Balance = 5000.00m, AccountName = "Main EUR Account", Status = "Active" },
            new Account { Id = 2, IBAN = "RO49AAAA1B31007593840001", Currency = "USD", Balance = 1200.00m, AccountName = "USD Account", Status = "Active" },
            new Account { Id = 3, IBAN = "RO49AAAA1B31007593840002", Currency = "RON", Balance = 8500.00m, AccountName = "RON Account", Status = "Active" },
            new Account { Id = 4, IBAN = "RO49AAAA1B31007593840003", Currency = "EUR", Balance = 300.00m, AccountName = "Savings EUR Account", Status = "Active" }
        ];
    }
}
