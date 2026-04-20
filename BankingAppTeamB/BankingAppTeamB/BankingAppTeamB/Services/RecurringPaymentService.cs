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
            return frequency switch
            {
                RecurringFrequency.Daily => from.AddDays(DailyIntervalInDays),
                RecurringFrequency.Weekly => from.AddDays(WeeklyIntervalInDays),
                RecurringFrequency.BiWeekly => from.AddDays(BiweeklyIntervalInDays),
                RecurringFrequency.Monthly => from.AddMonths(MonthlyIntervalInMonths),
                RecurringFrequency.Quarterly => from.AddMonths(QuarterlyIntervalInMonths),
                RecurringFrequency.Yearly => from.AddYears(YearlyIntervalInYears),
                _ => throw new ArgumentOutOfRangeException(nameof(frequency), $"Unknown frequency: {frequency}")
            };
        }

        public RecurringPayment Create(RecurringPaymentDto dto)
        {
            var recurringPayment = new RecurringPayment
            {
                UserId = dto.UserId,
                BillerId = dto.BillerId,
                SourceAccountId = dto.SourceAccountId,
                Amount = dto.Amount,
                IsPayInFull = dto.IsPayInFull,
                Frequency = dto.Frequency,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                NextExecutionDate = ComputeNextRunDate(dto.Frequency, dto.StartDate),
                Status = PaymentStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            return recurringPaymentRepository.Add(recurringPayment);
        }

        public List<RecurringPayment> GetByUser(int userId)
        {
            return recurringPaymentRepository.GetByUserId(userId);
        }

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

        public void Pause(int id)
        {
            var payment = GetRequiredRecurringPayment(id);

            payment.Status = PaymentStatus.Paused;
            recurringPaymentRepository.Update(payment);
        }

        public void Resume(int id)
        {
            var payment = GetRequiredRecurringPayment(id);

            payment.Status = PaymentStatus.Active;
            recurringPaymentRepository.Update(payment);
        }

        public void Cancel(int id)
        {
            var payment = GetRequiredRecurringPayment(id);

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
                    var dto = new BillPaymentDto
                    {
                        UserId = payment.UserId,
                        SourceAccountId = payment.SourceAccountId,
                        BillerId = payment.BillerId,
                        BillerReference = string.Empty,
                        Amount = payment.Amount,
                        IsPayInFull = payment.IsPayInFull
                    };

                    billPaymentService.PayBill(dto);

                    payment.NextExecutionDate = ComputeNextRunDate(payment.Frequency, payment.NextExecutionDate);
                    recurringPaymentRepository.Update(payment);
                }
                catch (Exception ex)
                {
                    payment.Status = PaymentStatus.Failed;
                    recurringPaymentRepository.Update(payment);
                    Debug.WriteLine($"[RecurringPaymentService] Payment ID {payment.Id} failed: {ex.Message}");
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