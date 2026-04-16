using System.Collections.Generic;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Repositories
{
    public interface ITransactionRepository
    {
        void Add(Transaction t);
        Transaction GetById(int id);
        List<Transaction> GetByUserId(int uid);
    }
}
