using System;
using System.Collections.Generic;
using System.Diagnostics;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Services
{
    public class NotificationService : INotificationService
    {
        /// <summary>Logs a notification message summarising the completed transfer details.</summary>
        public void NotifyTransferCompleted(Transfer transfer)
        {
            string message = $"[NOTIFICATION] Transfer completed: " +
                             $"€{transfer.Amount} {transfer.Currency} " +
                             $"to {transfer.RecipientName} ({transfer.RecipientIBAN}). " +
                             $"Status: {transfer.Status}. " +
                             $"Ref: {transfer.Id} at {transfer.CreatedAt:yyyy-MM-dd HH:mm:ss}";

            Log(message);
        }

        /// <summary>Logs a notification message showing the updated cumulative transfer stats for <paramref name="beneficiary"/>.</summary>
        public void NotifyBeneficiaryStatsUpdated(Beneficiary beneficiary, decimal amountSent)
        {
            string message = $"[NOTIFICATION] Beneficiary stats updated: " +
                             $"{beneficiary.Name} ({beneficiary.IBAN}). " +
                             $"Amount sent: €{amountSent}. " +
                             $"Total sent: €{beneficiary.TotalAmountSent}. " +
                             $"Transfer count: {beneficiary.TransferCount}.";

            Log(message);
        }

        /// <summary>Logs a notification message indicating that a rate alert has fired with the current live rate vs. the target.</summary>
        public void NotifyRateAlertTriggered(RateAlert alert, decimal currentRate)
        {
            string message = $"[NOTIFICATION] Rate alert triggered: " +
                             $"{alert.BaseCurrency}/{alert.TargetCurrency} " +
                             $"has reached {currentRate} " +
                             $"(your target was {alert.TargetRate}).";

            Log(message);
        }

        /// <summary>Logs a notification message warning that a recurring payment is due soon.</summary>
        public void NotifyRecurringPaymentDue(RecurringPayment payment)
        {
            string message = $"[NOTIFICATION] Recurring payment due soon: " +
                             $"€{payment.Amount} scheduled for " +
                             $"{payment.NextExecutionDate:yyyy-MM-dd}. " +
                             $"Status: {payment.Status}.";

            Log(message);
        }

        /// <summary>Iterates <paramref name="payments"/> and calls <see cref="NotifyRecurringPaymentDue"/> for any whose next execution falls within <paramref name="warningWindow"/> from now.</summary>
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

        /// <summary>Checks each alert in <paramref name="alerts"/> against <paramref name="liveRates"/> and calls <see cref="NotifyRateAlertTriggered"/> when the live rate meets or exceeds the target and the alert is not yet triggered.</summary>
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

        /// <summary>Outputs <paramref name="message"/> to both <see cref="System.Diagnostics.Debug"/> and the console.</summary>
        private void Log(string message)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }
    }
}
