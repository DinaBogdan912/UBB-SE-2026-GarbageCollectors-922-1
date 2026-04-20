using System;
using System.Collections.Generic;
using System.Diagnostics;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Services
{
    public class NotificationService : INotificationService
    {
        /// <summary>Logs a message saying a transfer finished successfully.</summary>
        public void NotifyTransferCompleted(Transfer transfer)
        {
            string message = $"[NOTIFICATION] Transfer completed: " +
                             $"€{transfer.Amount} {transfer.Currency} " +
                             $"to {transfer.RecipientName} ({transfer.RecipientIBAN}). " +
                             $"Status: {transfer.Status}. " +
                             $"Ref: {transfer.Id} at {transfer.CreatedAt:yyyy-MM-dd HH:mm:ss}";

            Log(message);
        }

        /// <summary>Logs updated info about how much has been sent to a beneficiary overall.</summary>
        public void NotifyBeneficiaryStatsUpdated(Beneficiary beneficiary, decimal amountSent)
        {
            string message = $"[NOTIFICATION] Beneficiary stats updated: " +
                             $"{beneficiary.Name} ({beneficiary.IBAN}). " +
                             $"Amount sent: €{amountSent}. " +
                             $"Total sent: €{beneficiary.TotalAmountSent}. " +
                             $"Transfer count: {beneficiary.TransferCount}.";

            Log(message);
        }

        /// <summary>Logs a message when a rate alert fires, showing the current rate vs. what you were waiting for.</summary>
        public void NotifyRateAlertTriggered(RateAlert alert, decimal currentRate)
        {
            string message = $"[NOTIFICATION] Rate alert triggered: " +
                             $"{alert.BaseCurrency}/{alert.TargetCurrency} " +
                             $"has reached {currentRate} " +
                             $"(your target was {alert.TargetRate}).";

            Log(message);
        }

        /// <summary>Logs a warning that a recurring payment is coming up soon.</summary>
        public void NotifyRecurringPaymentDue(RecurringPayment payment)
        {
            string message = $"[NOTIFICATION] Recurring payment due soon: " +
                             $"€{payment.Amount} scheduled for " +
                             $"{payment.NextExecutionDate:yyyy-MM-dd}. " +
                             $"Status: {payment.Status}.";

            Log(message);
        }

        /// <summary>Goes through a list of payments and sends a notification for any that are due within the warning window.</summary>
        public void CheckAndNotifyDuePayments(List<RecurringPayment> payments, TimeSpan warningWindow)
        {
            var now = DateTime.Now;
            foreach (var payment in payments)
            {
                if (payment.NextExecutionDate <= now + warningWindow)
                {
                    Debug.WriteLine($"[SCHEDULER] Due payment detected for payment Id={payment.Id}, " +
                                    $"due {payment.NextExecutionDate:yyyy-MM-dd}");
                    NotifyRecurringPaymentDue(payment);
                }
            }
        }

        /// <summary>Goes through alerts and fires a notification for any where the live rate hit or passed the target.</summary>
        public void CheckAndNotifyRateAlerts(List<RateAlert> alerts, Dictionary<string, decimal> liveRates)
        {
            foreach (var alert in alerts)
            {
                string pair = $"{alert.BaseCurrency}/{alert.TargetCurrency}";
                if (!liveRates.ContainsKey(pair))
                {
                    continue;
                }

                decimal currentRate = liveRates[pair];
                if (currentRate >= alert.TargetRate && !alert.IsTriggered)
                {
                    Debug.WriteLine($"[SCHEDULER] Rate alert triggered for pair {pair}: " +
                                    $"current={currentRate}, target={alert.TargetRate}");
                    NotifyRateAlertTriggered(alert, currentRate);
                }
            }
        }

        /// <summary>Writes a message to both the debug output and the console.</summary>
        private void Log(string message)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }
    }
}
