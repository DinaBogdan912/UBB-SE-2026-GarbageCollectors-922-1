using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Repositories;

namespace BankingAppTeamB.Services
{
    public class RecurringPaymentService : IRecurringPaymentService
    {
        private const int DailyIntervalInDays = 1;
        private const int WeeklyIntervalInDays = 7;
        private const int BiweeklyIntervalInDays = 14;
        private const int MonthlyIntervalInMonths = 1;
        private const int QuarterlyIntervalInMonths = 3;
        private const int YearlyIntervalInYears = 1;
        private const int DueSoonWarningHours = 24;

        private readonly IRecurringPaymentRepository recurringPaymentRepository;
        private readonly IBillPaymentService billPaymentService;

        public RecurringPaymentService(IRecurringPaymentRepository recurringPaymentRepository, IBillPaymentService billPaymentService)
        {
            this.recurringPaymentRepository = recurringPaymentRepository;
            this.billPaymentService = billPaymentService;
        }

        public DateTime ComputeNextRunDate(RecurringFrequency frequency, DateTime from)
        {
            if (!Enum.IsDefined(typeof(RecurringFrequency), frequency))
            {
                 throw new ArgumentOutOfRangeException(nameof(frequency), $"Unknown frequency: {frequency}");
            }
            return frequency switch
            {
                RecurringFrequency.Daily => from.AddDays(DailyIntervalInDays),
                RecurringFrequency.Weekly => from.AddDays(WeeklyIntervalInDays),
                RecurringFrequency.BiWeekly => from.AddDays(BiweeklyIntervalInDays),
                RecurringFrequency.Monthly => from.AddMonths(MonthlyIntervalInMonths),
                RecurringFrequency.Quarterly => from.AddMonths(QuarterlyIntervalInMonths),
                RecurringFrequency.Yearly => from.AddYears(YearlyIntervalInYears)
            };
        }
        public RecurringPayment Create(RecurringPaymentDto recurringPaymentDto)
        {
            var recurringPayment = new RecurringPayment
            {
                UserId = recurringPaymentDto.UserId,
                BillerId = recurringPaymentDto.BillerId,
                SourceAccountId = recurringPaymentDto.SourceAccountId,
                Amount = recurringPaymentDto.Amount,
                IsPayInFull = recurringPaymentDto.IsPayInFull,
                Frequency = recurringPaymentDto.Frequency,
                StartDate = recurringPaymentDto.StartDate,
                EndDate = recurringPaymentDto.EndDate,
                NextExecutionDate = ComputeNextRunDate(recurringPaymentDto.Frequency, recurringPaymentDto.StartDate),
                Status = PaymentStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            return recurringPaymentRepository.Add(recurringPayment);
        }

        public List<RecurringPayment> GetByUser(int userId)
        {
            return recurringPaymentRepository.GetByUserId(userId);
        }

        public void Pause(int recurringPaymentId)
        {
            var payment = recurringPaymentRepository.GetById(recurringPaymentId);
            if (payment == null)
            {
                throw new InvalidOperationException($"Recurring payment with ID {recurringPaymentId} does not exist.");
            }

            payment.Status = PaymentStatus.Paused;
            recurringPaymentRepository.Update(payment);
        }

        public void Resume(int recurringPaymentId)
        {
            var payment = recurringPaymentRepository.GetById(recurringPaymentId);
            if (payment == null)
            {
                throw new InvalidOperationException($"Recurring payment with ID {recurringPaymentId} does not exist.");
            }

            payment.Status = PaymentStatus.Active;
            recurringPaymentRepository.Update(payment);
        }

        public void Cancel(int recurringPaymentId)
        {
            var payment = recurringPaymentRepository.GetById(recurringPaymentId);
            if (payment == null)
            {
                throw new InvalidOperationException($"Recurring payment with ID {recurringPaymentId} does not exist.");
            }

            payment.Status = PaymentStatus.Cancelled;
            recurringPaymentRepository.Update(payment);
        }

        public void ProcessDuePayments()
        {
            var duePayments = recurringPaymentRepository.GetDueBefore(DateTime.UtcNow)
                .Where(payment => payment.Status == PaymentStatus.Active)
                .ToList();

            foreach (var payment in duePayments)
            {
                try
                {
                    var billPaymentDto = new BillPaymentDto
                    {
                        UserId = payment.UserId,
                        SourceAccountId = payment.SourceAccountId,
                        BillerId = payment.BillerId,
                        BillerReference = string.Empty,
                        Amount = payment.Amount,
                        IsPayInFull = payment.IsPayInFull
                    };

                    billPaymentService.PayBill(billPaymentDto);

                    payment.NextExecutionDate = ComputeNextRunDate(payment.Frequency, payment.NextExecutionDate);
                    recurringPaymentRepository.Update(payment);
                }
                catch (Exception paymentException)
                {
                    payment.Status = PaymentStatus.Failed;
                    recurringPaymentRepository.Update(payment);
                    Debug.WriteLine($"[RecurringPaymentService] Payment ID {payment.Id} failed: {paymentException.Message}");
                }
            }
        }

        public List<RecurringPayment> GetDueSoon()
        {
            var dueSoon = recurringPaymentRepository.GetDueBefore(DateTime.UtcNow.AddHours(DueSoonWarningHours))
                .Where(payment => payment.Status == PaymentStatus.Active)
                .ToList();

            foreach (var payment in dueSoon)
            {
                Debug.WriteLine($"[RecurringPaymentService] NOTIFICATION STUB - Payment ID {payment.Id} for UserId {payment.UserId} is due on {payment.NextExecutionDate:yyyy-MM-dd HH:mm}");
            }

            return dueSoon;
        }
    }
}