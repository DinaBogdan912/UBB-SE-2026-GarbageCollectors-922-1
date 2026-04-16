using System.Collections.Generic;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Repositories
{
    public interface IBeneficiaryRepository
    {
        void Add(Beneficiary b);
        Beneficiary GetById(int id);
        List<Beneficiary> GetByUserId(int uid);
        void Update(Beneficiary b);
        void Delete(int id);
        bool ExistsByIBAN(int uid, string iban);
    }
}
