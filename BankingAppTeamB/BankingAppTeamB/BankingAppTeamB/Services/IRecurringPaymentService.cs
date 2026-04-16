using System;
using System.Collections.Generic;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;

namespace BankingAppTeamB.Services
{
    public interface IRecurringPaymentService
    {
        void Cancel(int id);
        DateTime ComputeNextRunDate(RecurringFrequency frequency, DateTime from);
        RecurringPayment Create(RecurringPaymentDto dto);
        List<RecurringPayment> GetByUser(int userId);
        List<RecurringPayment> GetDueSoon();
        void Pause(int id);
        void ProcessDuePayments();
        void Resume(int id);
    }
}