using System.Collections.Generic;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Repositories
{
    public interface ITransferRepository
    {
        void Add(Transfer transfer);
<<<<<<< FilipB-refactor-tests-repo-model
        Transfer GetById(int transferId);
        List<Transfer> GetByUserId(int userId);
        void UpdateStatus(int transferId, string transferStatus);
=======
        Transfer GetById(int id);
        List<Transfer> GetByUserId(int userId);
        void UpdateStatus(int id, string newStatus);
>>>>>>> main
    }
}
