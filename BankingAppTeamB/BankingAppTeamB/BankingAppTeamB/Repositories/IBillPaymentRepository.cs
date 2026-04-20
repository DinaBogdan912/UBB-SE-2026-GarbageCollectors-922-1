using System.Collections.Generic;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Repositories
{
    public interface IBillPaymentRepository
    {
        BillPayment Add(BillPayment billPayment);
        List<BillPayment> GetByUserId(int userId);
        List<Biller> GetAllBillers(bool? isActive = null);
        List<Biller> SearchBillers(string searchQuery, string? category, bool? isActive = null);
        Biller? GetBillerById(int billerId);
        List<SavedBiller> GetSavedBillers(int userId);
        void SaveBiller(SavedBiller savedBiller);
        void DeleteSavedBiller(int savedBillerId);
    }
}