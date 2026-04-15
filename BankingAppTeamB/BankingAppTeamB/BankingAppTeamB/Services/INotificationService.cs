using BankingAppTeamB.Models;
using System;
using System.Collections.Generic;

namespace BankingAppTeamB.Services
{
    public interface INotificationService
    {
        void CheckAndNotifyDuePayments(List<RecurringPayment> payments, TimeSpan warningWindow);
        void CheckAndNotifyRateAlerts(List<RateAlert> alerts, Dictionary<string, decimal> liveRates);
        void NotifyBeneficiaryStatsUpdated(Beneficiary beneficiary, decimal amountSent);
        void NotifyRateAlertTriggered(RateAlert alert, decimal currentRate);
        void NotifyRecurringPaymentDue(RecurringPayment payment);
        void NotifyTransferCompleted(Transfer transfer);
    }
}