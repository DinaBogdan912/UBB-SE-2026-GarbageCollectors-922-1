using System;
using System.Collections.Generic;

using BankingAppTeamB.Models;

namespace BankingAppTeamB.Mocks
{
    public static class UserSession
    {
        private const int DefaultCurrentUserId = 1;

        public static int CurrentUserId = DefaultCurrentUserId;
        public static string CurrentUserName = "Ion Popescu";

        public static List<Account> GetAccounts() =>
        [
            new Account { Id = 1, IBAN = "RO49AAAA1B31007593840000", Currency = "EUR", Balance = 5000.00m, AccountName = "Main EUR Account", Status = "Active" },
            new Account { Id = 2, IBAN = "RO49AAAA1B31007593840001", Currency = "USD", Balance = 1200.00m, AccountName = "USD Account", Status = "Active" },
            new Account { Id = 3, IBAN = "RO49AAAA1B31007593840002", Currency = "RON", Balance = 8500.00m, AccountName = "RON Account", Status = "Active" },
            new Account { Id = 4, IBAN = "RO49AAAA1B31007593840003", Currency = "EUR", Balance = 300.00m, AccountName = "Savings EUR Account", Status = "Active" },
        ];
    }
}
