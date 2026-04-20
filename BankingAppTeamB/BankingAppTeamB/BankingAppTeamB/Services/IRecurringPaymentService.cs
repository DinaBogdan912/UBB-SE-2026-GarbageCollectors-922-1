using System;
using System.Collections.Generic;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;

namespace BankingAppTeamB.Services
{
    public interface IRecurringPaymentService
    {
        void Cancel(int recurringPaymentId);
        DateTime ComputeNextRunDate(RecurringFrequency frequency, DateTime from);
        RecurringPayment Create(RecurringPaymentDto recurringPaymentDto);
        List<RecurringPayment> GetByUser(int userId);
        List<RecurringPayment> GetDueSoon();
        void Pause(int recurringPaymentId);
        void ProcessDuePayments();
        void Resume(int recurringPaymentId);
    }
}