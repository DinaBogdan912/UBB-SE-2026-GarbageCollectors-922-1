using System.Collections.Generic;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Repositories
{
    public interface ITransferRepository
    {
        void Add(Transfer transfer);
        Transfer GetById(int transferId);
        List<Transfer> GetByUserId(int userId);
        void UpdateStatus(int transferId, string transferStatus);
    }
}
