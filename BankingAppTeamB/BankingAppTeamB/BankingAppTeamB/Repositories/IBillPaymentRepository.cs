using BankingAppTeamB.Models;
using System.Collections.Generic;

namespace BankingAppTeamB.Repositories
{
    public interface IBillPaymentRepository
    {
        BillPayment Add(BillPayment billPayment);
        List<BillPayment> GetByUserId(int userId);
        List<Biller> GetAllBillers();
        List<Biller> SearchBillers(string query, string? category);
        Biller? GetBillerById(int id);
        List<SavedBiller> GetSavedBillers(int userId);
        void SaveBiller(SavedBiller savedBiller);
        void DeleteSavedBiller(int id);
    }
}