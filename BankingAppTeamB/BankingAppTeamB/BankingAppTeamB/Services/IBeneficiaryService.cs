using System.Collections.Generic;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;

namespace BankingAppTeamB.Services
{
    public interface IBeneficiaryService
    {
        Beneficiary Add(string name, string iban, int uid);
        TransferDto BuildTransferDtoFrom(Beneficiary beneficiary, int sourceAccountId, int userId);
        void Delete(int id);
        List<Beneficiary> GetByUser(int userId);
        void Update(Beneficiary beneficiary);
        bool ValidateIBAN(string iban);
    }
}