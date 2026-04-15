using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using System.Collections.Generic;

namespace BankingAppTeamB.Services
{
    public interface ITransferService
    {
        Transfer ExecuteTransfer(TransferDto dto);
        string GetBankNameFromIBAN(string iban);
        FxPreview GetFxPreview(string src, string tgt, decimal amt);
        List<Transfer> GetHistory(int userId);
        bool Requires2FA(decimal amount);
        bool ValidateIBAN(string iban);
    }
}