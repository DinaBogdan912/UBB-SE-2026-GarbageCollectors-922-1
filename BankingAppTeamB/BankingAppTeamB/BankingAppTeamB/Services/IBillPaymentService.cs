using System.Collections.Generic;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;

namespace BankingAppTeamB.Services
{
    public interface IBillPaymentService
    {
        decimal CalculateFee(decimal amount);
        List<Biller> GetBillerDirectory(string? category);
        List<SavedBiller> GetSavedBillers(int userId);
        BillPayment PayBill(BillPaymentDto billPaymentDto);
        void RemoveSavedBiller(int savedBillerId);
        bool Requires2FA(decimal amount);
        void SaveBiller(int userId, int billerId, string? nickname, string? defaultReference);
        List<Biller> SearchBillers(string query);
        List<Biller> SearchBillers(string query, string? category);
    }
}