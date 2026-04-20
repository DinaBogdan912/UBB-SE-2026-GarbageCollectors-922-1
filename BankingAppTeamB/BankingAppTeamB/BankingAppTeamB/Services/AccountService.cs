using System;
using System.Collections.Generic;
using System.Linq;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUserSessionService userSessionService;

        public AccountService(IUserSessionService userSessionService)
        {
            this.userSessionService = userSessionService;
        }

        public void DebitAccount(int accountId, decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be > 0.");
            }

            List<Account> accounts = userSessionService.GetAccounts();
            Account? account = accounts.SingleOrDefault(account => account.Id == accountId);

            if (account == null)
            {
                throw new ArgumentException("Account not found.", nameof(accountId));
            }

            if (account.Balance < amount)
            {
                throw new InvalidOperationException("Insufficient funds.");
            }

            account.Balance -= amount;
        }

        public void CreditAccount(int accountId, decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be > 0.");
            }

            List<Account> accounts = userSessionService.GetAccounts();
            Account? account = accounts.SingleOrDefault(account => account.Id == accountId);

            if (account == null)
            {
                throw new ArgumentException("Account not found.", nameof(accountId));
            }

            account.Balance += amount;
        }

        public bool IsAccountValid(int accountId)
        {
            List<Account> accounts = userSessionService.GetAccounts();
            return accounts.Any(account => account.Id == accountId);
        }

        public decimal GetBalance(int accountId)
        {
            List<Account> accounts = userSessionService.GetAccounts();
            Account? account = accounts.SingleOrDefault(account => account.Id == accountId);
            return account?.Balance ?? 0m;
        }
    }
}
