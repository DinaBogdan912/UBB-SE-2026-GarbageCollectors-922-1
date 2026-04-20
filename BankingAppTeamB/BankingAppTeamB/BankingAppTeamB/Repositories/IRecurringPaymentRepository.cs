using System;
using System.Collections.Generic;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Repositories
{
    public interface IRecurringPaymentRepository
    {
        RecurringPayment Add(RecurringPayment recurringPayment);
        RecurringPayment? GetById(int recurringPaymentId);
        List<RecurringPayment> GetByUserId(int userId);
        List<RecurringPayment> GetDueBefore(DateTime dueBeforeDateTime);
        void Update(RecurringPayment recurringPayment);
        void Delete(int recurringPaymentId);
    }
}