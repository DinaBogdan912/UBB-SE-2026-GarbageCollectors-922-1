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

        /// <summary>Figures out the next date this payment should run based on how often it repeats.</summary>
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
        /// <summary>Builds a new recurring payment from the form data, sets it active, and saves it.</summary>
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

        /// <summary>Gets all recurring payments that belong to a user.</summary>
        public List<RecurringPayment> GetByUser(int userId)
        {
            return recurringPaymentRepository.GetByUserId(userId);
        }

        /// <summary>Tries to load a recurring payment by ID and throws a clear error if it is missing.</summary>
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

        /// <summary>Marks a recurring payment as paused and saves that change.</summary>
        public void Pause(int id)
        {
            var payment = GetRequiredRecurringPayment(id);

            payment.Status = PaymentStatus.Paused;
            recurringPaymentRepository.Update(payment);
        }

        /// <summary>Turns a paused recurring payment back on and saves it.</summary>
        public void Resume(int recurringPaymentId)
        {
            var payment = GetRequiredRecurringPayment(recurringPaymentId);

            payment.Status = PaymentStatus.Active;
            recurringPaymentRepository.Update(payment);
        }

        /// <summary>Marks a recurring payment as cancelled so it stops running.</summary>
        public void Cancel(int recurringPaymentId)
        {
            var payment = GetRequiredRecurringPayment(recurringPaymentId);

            payment.Status = PaymentStatus.Cancelled;
            recurringPaymentRepository.Update(payment);
        }

        /// <summary>Runs every active payment that is due now, then moves its next run date or marks it failed.</summary>
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

        /// <summary>Gets active payments due in the next 24 hours and logs a reminder for each one.</summary>
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