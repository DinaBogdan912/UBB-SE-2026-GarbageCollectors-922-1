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

        /// <summary>Deducts <paramref name="amount"/> from the in-memory balance of the account identified by <paramref name="accountId"/>; throws if the account is not found or funds are insufficient.</summary>
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

        /// <summary>Adds <paramref name="amount"/> to the in-memory balance of the account identified by <paramref name="accountId"/>; throws if the account is not found.</summary>
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

        /// <summary>Returns <see langword="true"/> when an account with <paramref name="accountId"/> exists in the current session's account list.</summary>
        public bool IsAccountValid(int accountId)
        {
            List<Account> accounts = userSessionService.GetAccounts();
            return accounts.Any(account => account.Id == accountId);
        }

        /// <summary>Returns the current in-memory balance for <paramref name="accountId"/>, or zero if the account is not found.</summary>
        public decimal GetBalance(int accountId)
        {
            List<Account> accounts = userSessionService.GetAccounts();
            Account? account = accounts.SingleOrDefault(account => account.Id == accountId);
            return account?.Balance ?? MinimumAmount;
        }
    }
}
