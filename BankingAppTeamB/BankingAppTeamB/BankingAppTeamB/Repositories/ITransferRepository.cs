using System.Collections.Generic;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Repositories
{
    public interface ITransferRepository
    {
        void Add(Transfer transfer);
        Transfer GetById(int id);
        List<Transfer> GetByUserId(int userId);
        void UpdateStatus(int id, string newStatus);
    }
}
