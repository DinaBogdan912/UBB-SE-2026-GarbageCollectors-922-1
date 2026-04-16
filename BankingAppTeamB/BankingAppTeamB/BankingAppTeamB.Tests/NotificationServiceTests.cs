using BankingAppTeamB.Models;
using BankingAppTeamB.Services;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace BankingAppTeamB.Tests.Services
{
    public class NotificationServiceTests
    {
        private static NotificationService CreateSut() => new();

        [Fact]
        public void NotifyTransferCompleted_DoesNotThrow()
        {
            var sut = CreateSut();
            var transfer = new Transfer
            {
                Id = 1,
                Amount = 150m,
                Currency = "RON",
                RecipientName = "Ana",
                RecipientIBAN = "RO49AAAA1B31007593840000",
                Status = TransferStatus.Completed,
                CreatedAt = DateTime.Now
            };

            Action act = () => sut.NotifyTransferCompleted(transfer);

            act.Should().NotThrow();
        }

        [Fact]
        public void NotifyBeneficiaryStatsUpdated_DoesNotThrow()
        {
            var sut = CreateSut();
            var beneficiary = new Beneficiary
            {
                Name = "Ana",
                IBAN = "RO49AAAA1B31007593840000",
                TotalAmountSent = 300m,
                TransferCount = 2
            };

            Action act = () => sut.NotifyBeneficiaryStatsUpdated(beneficiary, 150m);

            act.Should().NotThrow();
        }

        [Fact]
        public void NotifyRateAlertTriggered_DoesNotThrow()
        {
            var sut = CreateSut();
            var alert = new RateAlert
            {
                BaseCurrency = "USD",
                TargetCurrency = "RON",
                TargetRate = 4.5m
            };

            Action act = () => sut.NotifyRateAlertTriggered(alert, 4.6m);

            act.Should().NotThrow();
        }

        [Fact]
        public void NotifyRecurringPaymentDue_DoesNotThrow()
        {
            var sut = CreateSut();
            var payment = new RecurringPayment
            {
                Id = 1,
                Amount = 99m,
                NextExecutionDate = DateTime.Now.AddDays(1),
                Status = PaymentStatus.Completed
            };

            Action act = () => sut.NotifyRecurringPaymentDue(payment);

            act.Should().NotThrow();
        }

        [Fact]
        public void CheckAndNotifyDuePayments_DoesNotThrow_WithEmptyList()
        {
            var sut = CreateSut();

            Action act = () => sut.CheckAndNotifyDuePayments(new List<RecurringPayment>(), TimeSpan.FromDays(1));

            act.Should().NotThrow();
        }

        [Fact]
        public void CheckAndNotifyDuePayments_DoesNotThrow_WhenPaymentWithinWarningWindow()
        {
            var sut = CreateSut();
            var payment = new RecurringPayment
            {
                Id = 1,
                Amount = 50m,
                NextExecutionDate = DateTime.Now.AddHours(12),
                Status = PaymentStatus.Completed
            };

            Action act = () => sut.CheckAndNotifyDuePayments(
                new List<RecurringPayment> { payment },
                TimeSpan.FromDays(1));

            act.Should().NotThrow();
        }

        [Fact]
        public void CheckAndNotifyDuePayments_DoesNotThrow_WhenPaymentBeyondWarningWindow()
        {
            var sut = CreateSut();
            var payment = new RecurringPayment
            {
                Id = 2,
                Amount = 75m,
                NextExecutionDate = DateTime.Now.AddDays(10),
                Status = PaymentStatus.Completed
            };

            Action act = () => sut.CheckAndNotifyDuePayments(
                new List<RecurringPayment> { payment },
                TimeSpan.FromDays(1));

            act.Should().NotThrow();
        }

        [Fact]
        public void CheckAndNotifyDuePayments_DoesNotThrow_WhenPaymentIsOverdue()
        {
            var sut = CreateSut();
            var payment = new RecurringPayment
            {
                Id = 3,
                Amount = 200m,
                NextExecutionDate = DateTime.Now.AddDays(-3),
                Status = PaymentStatus.Completed
            };

            Action act = () => sut.CheckAndNotifyDuePayments(
                new List<RecurringPayment> { payment },
                TimeSpan.FromDays(1));

            act.Should().NotThrow();
        }

        [Fact]
        public void CheckAndNotifyDuePayments_DoesNotThrow_WithMultiplePayments()
        {
            var sut = CreateSut();
            var payments = new List<RecurringPayment>
            {
                new() { Id = 1, Amount = 50m,  NextExecutionDate = DateTime.Now.AddHours(6) },
                new() { Id = 2, Amount = 75m,  NextExecutionDate = DateTime.Now.AddDays(5) },
                new() { Id = 3, Amount = 100m, NextExecutionDate = DateTime.Now.AddDays(-1) }
            };

            Action act = () => sut.CheckAndNotifyDuePayments(payments, TimeSpan.FromDays(1));

            act.Should().NotThrow();
        }

        [Fact]
        public void CheckAndNotifyRateAlerts_DoesNotThrow_WithEmptyList()
        {
            var sut = CreateSut();

            Action act = () => sut.CheckAndNotifyRateAlerts(
                new List<RateAlert>(),
                new Dictionary<string, decimal> { ["USD/RON"] = 4.5m });

            act.Should().NotThrow();
        }

        [Fact]
        public void CheckAndNotifyRateAlerts_DoesNotThrow_WhenPairNotInLiveRates()
        {
            var sut = CreateSut();
            var alert = new RateAlert { BaseCurrency = "USD", TargetCurrency = "RON", TargetRate = 4.5m };

            Action act = () => sut.CheckAndNotifyRateAlerts(
                new List<RateAlert> { alert },
                new Dictionary<string, decimal> { ["EUR/RON"] = 5.0m });

            act.Should().NotThrow();
        }

        [Fact]
        public void CheckAndNotifyRateAlerts_DoesNotThrow_WhenRateBelowTarget()
        {
            var sut = CreateSut();
            var alert = new RateAlert { BaseCurrency = "USD", TargetCurrency = "RON", TargetRate = 5.0m };

            Action act = () => sut.CheckAndNotifyRateAlerts(
                new List<RateAlert> { alert },
                new Dictionary<string, decimal> { ["USD/RON"] = 4.5m });

            act.Should().NotThrow();
        }

        [Fact]
        public void CheckAndNotifyRateAlerts_DoesNotThrow_WhenRateMeetsTarget_AndNotTriggered()
        {
            var sut = CreateSut();
            var alert = new RateAlert { BaseCurrency = "USD", TargetCurrency = "RON", TargetRate = 4.5m, IsTriggered = false };

            Action act = () => sut.CheckAndNotifyRateAlerts(
                new List<RateAlert> { alert },
                new Dictionary<string, decimal> { ["USD/RON"] = 4.5m });

            act.Should().NotThrow();
        }

        [Fact]
        public void CheckAndNotifyRateAlerts_DoesNotThrow_WhenRateAboveTarget_AndNotTriggered()
        {
            var sut = CreateSut();
            var alert = new RateAlert { BaseCurrency = "EUR", TargetCurrency = "RON", TargetRate = 4.9m, IsTriggered = false };

            Action act = () => sut.CheckAndNotifyRateAlerts(
                new List<RateAlert> { alert },
                new Dictionary<string, decimal> { ["EUR/RON"] = 5.1m });

            act.Should().NotThrow();
        }

        [Fact]
        public void CheckAndNotifyRateAlerts_DoesNotThrow_WhenRateAboveTarget_ButAlreadyTriggered()
        {
            var sut = CreateSut();
            var alert = new RateAlert { BaseCurrency = "EUR", TargetCurrency = "RON", TargetRate = 4.9m, IsTriggered = true };

            Action act = () => sut.CheckAndNotifyRateAlerts(
                new List<RateAlert> { alert },
                new Dictionary<string, decimal> { ["EUR/RON"] = 5.1m });

            act.Should().NotThrow();
        }

        [Fact]
        public void CheckAndNotifyRateAlerts_DoesNotThrow_WithMixedAlerts()
        {
            var sut = CreateSut();
            var alerts = new List<RateAlert>
            {
                new() { BaseCurrency = "USD", TargetCurrency = "RON", TargetRate = 4.5m, IsTriggered = false },
                new() { BaseCurrency = "EUR", TargetCurrency = "RON", TargetRate = 5.0m, IsTriggered = true },
                new() { BaseCurrency = "GBP", TargetCurrency = "RON", TargetRate = 6.0m, IsTriggered = false }
            };
            var rates = new Dictionary<string, decimal>
            {
                ["USD/RON"] = 4.6m,
                ["EUR/RON"] = 5.2m
            };

            Action act = () => sut.CheckAndNotifyRateAlerts(alerts, rates);

            act.Should().NotThrow();
        }
    }
}
