using System.Collections.Generic;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Repositories
{
    public interface ITransferRepository
    {
        void Add(Transfer t);
        Transfer GetById(int id);
        List<Transfer> GetByUserId(int uid);
        void UpdateStatus(int id, string s);
    }
}
