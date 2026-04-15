using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using System.Collections.Generic;

namespace BankingAppTeamB.Services
{
    public interface IBillPaymentService
    {
        decimal CalculateFee(decimal amount);
        List<Biller> GetBillerDirectory(string? category);
        List<SavedBiller> GetSavedBillers(int userId);
        BillPayment PayBill(BillPaymentDto dto);
        void RemoveSavedBiller(int id);
        bool Requires2FA(decimal amount);
        void SaveBiller(int userId, int billerId, string? nickname, string? defaultRef);
        List<Biller> SearchBillers(string query);
        List<Biller> SearchBillers(string query, string? category);
    }
}