using System;
using System.Collections.Generic;
using BankingAppTeamB.Models;
using BankingAppTeamB.Services;
using FluentAssertions;
using Xunit;

namespace BankingAppTeamB.Tests.Services;

public class NotificationServiceTests
{
    private const int DefaultId = 1;
    private const int DefaultUserId = 42;
    private const decimal DefaultAmount = 150.50m;
    private const decimal TargetRate = 1.1m;
    private const decimal ReachedRate = 1.1m;
    private const string DefaultCurrency = "EUR";
    private const string BaseCurrency = "EUR";
    private const string TargetCurrency = "USD";
    private const string RecipientName = "John Doe";
    private const string RecipientIban = "RO123456789123456";

    [Fact]
    public void NotifyTransferCompleted_WhenCalled_DoesNotThrow()
    {
        // Arrange
        var notificationService = new NotificationService();
        var transfer = new Transfer
        {
            Id = DefaultId,
            UserId = DefaultUserId,
            Amount = DefaultAmount,
            Currency = DefaultCurrency,
            RecipientName = RecipientName,
            RecipientIBAN = RecipientIban,
            Status = TransferStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        Action notifyAction = () => notificationService.NotifyTransferCompleted(transfer);

        // Assert
        notifyAction.Should().NotThrow();
    }

    [Fact]
    public void NotifyBeneficiaryStatsUpdated_WhenCalled_DoesNotThrow()
    {
        // Arrange
        var notificationService = new NotificationService();
        var beneficiary = new Beneficiary
        {
            Name = RecipientName,
            IBAN = RecipientIban,
            TotalAmountSent = DefaultAmount,
            TransferCount = 1
        };

        // Act
        Action notifyAction = () => notificationService.NotifyBeneficiaryStatsUpdated(beneficiary, DefaultAmount);

        // Assert
        notifyAction.Should().NotThrow();
    }

    [Fact]
    public void NotifyRateAlertTriggered_WhenCalled_DoesNotThrow()
    {
        // Arrange
        var notificationService = new NotificationService();
        var rateAlert = new RateAlert(DefaultUserId, BaseCurrency, TargetCurrency, TargetRate, false);

        // Act
        Action notifyAction = () => notificationService.NotifyRateAlertTriggered(rateAlert, ReachedRate);

        // Assert
        notifyAction.Should().NotThrow();
    }

    [Fact]
    public void NotifyRecurringPaymentDue_WhenCalled_DoesNotThrow()
    {
        // Arrange
        var notificationService = new NotificationService();
        var recurringPayment = new RecurringPayment
        {
            Amount = DefaultAmount,
            NextExecutionDate = DateTime.UtcNow,
            Status = PaymentStatus.Active
        };

        // Act
        Action notifyAction = () => notificationService.NotifyRecurringPaymentDue(recurringPayment);

        // Assert
        notifyAction.Should().NotThrow();
    }

    [Fact]
    public void CheckAndNotifyDuePayments_WhenPaymentIsDue_DoesNotThrow()
    {
        // Arrange
        var notificationService = new NotificationService();
        var warningWindow = TimeSpan.FromDays(1);
        var duePayment = new RecurringPayment
        {
            Id = DefaultId,
            NextExecutionDate = DateTime.Now.AddHours(12)
        };
        var payments = new List<RecurringPayment> { duePayment };

        // Act
        Action checkAction = () => notificationService.CheckAndNotifyDuePayments(payments, warningWindow);

        // Assert
        checkAction.Should().NotThrow();
    }

    [Fact]
    public void CheckAndNotifyRateAlerts_WhenAlertIsTriggered_DoesNotThrow()
    {
        // Arrange
        var notificationService = new NotificationService();
        var rateAlert = new RateAlert(DefaultUserId, BaseCurrency, TargetCurrency, TargetRate, false)
        {
            IsTriggered = false
        };
        var alerts = new List<RateAlert> { rateAlert };
        var liveRates = new Dictionary<string, decimal>
        {
            { $"{BaseCurrency}/{TargetCurrency}", ReachedRate }
        };

        // Act
        Action checkAction = () => notificationService.CheckAndNotifyRateAlerts(alerts, liveRates);

        // Assert
        checkAction.Should().NotThrow();
    }
}
