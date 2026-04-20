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

        /// <summary>Returns the next execution date calculated by advancing <paramref name="from"/> by the interval corresponding to <paramref name="frequency"/>.</summary>
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
        /// <summary>Creates a new active recurring payment from the DTO, computes the first execution date, persists it, and returns the saved entity.</summary>
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

        /// <summary>Returns all recurring payments registered for <paramref name="userId"/>.</summary>
        public List<RecurringPayment> GetByUser(int userId)
        {
            return recurringPaymentRepository.GetByUserId(userId);
        }

        /// <summary>Fetches the recurring payment by <paramref name="id"/> and throws <see cref="InvalidOperationException"/> if it does not exist.</summary>
        private RecurringPayment GetRequiredRecurringPayment(int id)
        {
            try
            {
                return recurringPaymentRepository.GetById(id)
                    ?? throw new InvalidOperationException($"Recurring payment with ID {id} does not exist.");
            }
            catch (KeyNotFoundException ex)
            {
                throw new InvalidOperationException($"Recurring payment with ID {id} does not exist.", ex);
            }
        }

        /// <summary>Sets the status of the recurring payment with <paramref name="id"/> to <see cref="PaymentStatus.Paused"/> and persists the change.</summary>
        public void Pause(int id)
        {
            var payment = GetRequiredRecurringPayment(id);

            payment.Status = PaymentStatus.Paused;
            recurringPaymentRepository.Update(payment);
        }

        /// <summary>Reactivates a paused recurring payment by setting its status to <see cref="PaymentStatus.Active"/> and persisting the change.</summary>
        public void Resume(int recurringPaymentId)
        {
            var payment = GetRequiredRecurringPayment(recurringPaymentId);

            payment.Status = PaymentStatus.Active;
            recurringPaymentRepository.Update(payment);
        }

        /// <summary>Sets the status of the recurring payment to <see cref="PaymentStatus.Cancelled"/> and persists the change.</summary>
        public void Cancel(int recurringPaymentId)
        {
            var payment = GetRequiredRecurringPayment(recurringPaymentId);

            payment.Status = PaymentStatus.Cancelled;
            recurringPaymentRepository.Update(payment);
        }

        /// <summary>Executes all active recurring payments whose <c>NextExecutionDate</c> is in the past, advances their next run date, and marks any payment that fails as <see cref="PaymentStatus.Failed"/>.</summary>
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

        /// <summary>Returns active recurring payments whose next execution date falls within the next 24 hours, logging a debug notification for each.</summary>
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