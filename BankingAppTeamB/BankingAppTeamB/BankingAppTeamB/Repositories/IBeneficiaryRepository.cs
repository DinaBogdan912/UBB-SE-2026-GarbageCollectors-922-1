using System.Collections.Generic;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Repositories
{
    public interface IBeneficiaryRepository
    {
        void Add(Beneficiary beneficiary);
        Beneficiary GetById(int beneficiaryId);
        List<Beneficiary> GetByUserId(int userId);
        void Update(Beneficiary beneficiary);
        void Delete(int beneficiaryId);
        bool ExistsByIBAN(int userId, string iban);
    }
}
