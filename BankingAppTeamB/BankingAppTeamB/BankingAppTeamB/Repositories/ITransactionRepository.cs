using System.Collections.Generic;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Repositories
{
    public interface ITransactionRepository
    {
        void Add(Transaction transaction);
        Transaction GetById(int transactionId);
        List<Transaction> GetByUserId(int userId);
    }
}
