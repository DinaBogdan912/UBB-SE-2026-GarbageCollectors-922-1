using System;
using System.Collections.Generic;
using System.Linq;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Services
{
    public class AccountService : IAccountService
    {
        private const int MinimumAmount = 0;
        private readonly IUserSessionService userSessionService;

        public AccountService(IUserSessionService userSessionService)
        {
            this.userSessionService = userSessionService;
        }

        /// <summary>Takes money out of an account — throws an error if the account doesn't exist or you don't have enough.</summary>
        public void DebitAccount(int accountId, decimal amount)
        {
            if (amount <= MinimumAmount)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), $"Amount must be > {MinimumAmount}.");
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

        /// <summary>Puts money into an account — throws an error if the account doesn't exist.</summary>
        public void CreditAccount(int accountId, decimal amount)
        {
            if (amount <= MinimumAmount)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), $"Amount must be > {MinimumAmount}.");
            }

            List<Account> accounts = userSessionService.GetAccounts();
            Account? account = accounts.SingleOrDefault(account => account.Id == accountId);

            if (account == null)
            {
                throw new ArgumentException("Account not found.", nameof(accountId));
            }

            account.Balance += amount;
        }

        /// <summary>Checks if an account ID actually exists in the user's account list.</summary>
        public bool IsAccountValid(int accountId)
        {
            List<Account> accounts = userSessionService.GetAccounts();
            return accounts.Any(account => account.Id == accountId);
        }

        /// <summary>Returns how much money is in an account, or zero if the account doesn't exist.</summary>
        public decimal GetBalance(int accountId)
        {
            List<Account> accounts = userSessionService.GetAccounts();
            Account? account = accounts.SingleOrDefault(account => account.Id == accountId);
            return account?.Balance ?? MinimumAmount;
        }
    }
}
