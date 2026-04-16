using System;
using System.Linq;
using BankingAppTeamB.Mocks;

namespace BankingAppTeamB.Services
{
    public class AccountService : IAccountService
    {
        private const decimal StubAccountBalance = 50m;
        public void DebitAccount(int accountId, decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be > 0.");
            }

            var accounts = UserSession.GetAccounts();
            var account = accounts.SingleOrDefault(account => account.Id == accountId);

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

            var accounts = UserSession.GetAccounts();
            var account = accounts.SingleOrDefault(account => account.Id == accountId);

            if (account == null)
            {
                throw new ArgumentException("Account not found.", nameof(accountId));
            }

            account.Balance += amount;
        }

        public AccountService()
        {
        }

        public bool IsAccountValid(int id)
        {
            return true;
        }

        public decimal GetBalance(int id)
        {
            return StubAccountBalance;
        }
    }
}