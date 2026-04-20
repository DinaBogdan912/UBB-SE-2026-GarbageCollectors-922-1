using System.Collections.Generic;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;

namespace BankingAppTeamB.Services
{
    public interface ITransferService
    {
        Transfer ExecuteTransfer(TransferDto transferDto);
        string GetBankNameFromIBAN(string iban);
        FxPreview GetFxPreview(string sourceCurrency, string targetCurrency, decimal amount);
        List<Transfer> GetHistory(int userId);
        bool Requires2FA(decimal amount);
        bool ValidateIBAN(string iban);
    }
}