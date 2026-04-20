using System;
using System.Collections.Generic;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Repositories;
using BankingAppTeamB.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace BankingAppTeamB.Tests.Services;

public class RecurringPaymentServiceTests
{
    private const int ValidRecurringPaymentId = 1;
    private const int InvalidRecurringPaymentId = 999;
    private const int DefaultUserId = 42;
    private const int DefaultBillerId = 10;
    private const int DefaultSourceAccountId = 100;
    private const decimal DefaultPaymentAmount = 150.50m;
    
    private const int OneDayIncrement = 1;
    private const int OneWeekIncrementInDays = 7;
    private const int TwoWeeksIncrementInDays = 14;
    private const int OneMonthIncrement = 1;
    private const int ThreeMonthsIncrement = 3;
    private const int OneYearIncrement = 1;

    private readonly DateTime _baseReferenceDate = new DateTime(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ComputeNextRunDate_WhenFrequencyIsDaily_ReturnsNextDay()
    {
        // Arrange
        var mockRecurringPaymentRepository = new Mock<IRecurringPaymentRepository>();
        var mockBillPaymentService = new Mock<IBillPaymentService>();
        var recurringPaymentService = new RecurringPaymentService(mockRecurringPaymentRepository.Object, mockBillPaymentService.Object);
        var expectedNextRunDate = _baseReferenceDate.AddDays(OneDayIncrement);

        // Act
        var actualNextRunDate = recurringPaymentService.ComputeNextRunDate(RecurringFrequency.Daily, _baseReferenceDate);

        // Assert
        actualNextRunDate.Should().Be(expectedNextRunDate);
    }

    [Fact]
    public void ComputeNextRunDate_WhenFrequencyIsWeekly_ReturnsNextWeek()
    {
        // Arrange
        var mockRecurringPaymentRepository = new Mock<IRecurringPaymentRepository>();
        var mockBillPaymentService = new Mock<IBillPaymentService>();
        var recurringPaymentService = new RecurringPaymentService(mockRecurringPaymentRepository.Object, mockBillPaymentService.Object);
        var expectedNextRunDate = _baseReferenceDate.AddDays(OneWeekIncrementInDays);

        // Act
        var actualNextRunDate = recurringPaymentService.ComputeNextRunDate(RecurringFrequency.Weekly, _baseReferenceDate);

        // Assert
        actualNextRunDate.Should().Be(expectedNextRunDate);
    }

    [Fact]
    public void ComputeNextRunDate_WhenFrequencyIsBiWeekly_ReturnsTwoWeeksLater()
    {
        // Arrange
        var mockRecurringPaymentRepository = new Mock<IRecurringPaymentRepository>();
        var mockBillPaymentService = new Mock<IBillPaymentService>();
        var recurringPaymentService = new RecurringPaymentService(mockRecurringPaymentRepository.Object, mockBillPaymentService.Object);
        var expectedNextRunDate = _baseReferenceDate.AddDays(TwoWeeksIncrementInDays);

        // Act
        var actualNextRunDate = recurringPaymentService.ComputeNextRunDate(RecurringFrequency.BiWeekly, _baseReferenceDate);

        // Assert
        actualNextRunDate.Should().Be(expectedNextRunDate);
    }

    [Fact]
    public void ComputeNextRunDate_WhenFrequencyIsMonthly_ReturnsNextMonth()
    {
        // Arrange
        var mockRecurringPaymentRepository = new Mock<IRecurringPaymentRepository>();
        var mockBillPaymentService = new Mock<IBillPaymentService>();
        var recurringPaymentService = new RecurringPaymentService(mockRecurringPaymentRepository.Object, mockBillPaymentService.Object);
        var expectedNextRunDate = _baseReferenceDate.AddMonths(OneMonthIncrement);

        // Act
        var actualNextRunDate = recurringPaymentService.ComputeNextRunDate(RecurringFrequency.Monthly, _baseReferenceDate);

        // Assert
        actualNextRunDate.Should().Be(expectedNextRunDate);
    }

    [Fact]
    public void ComputeNextRunDate_WhenFrequencyIsQuarterly_ReturnsThreeMonthsLater()
    {
        // Arrange
        var mockRecurringPaymentRepository = new Mock<IRecurringPaymentRepository>();
        var mockBillPaymentService = new Mock<IBillPaymentService>();
        var recurringPaymentService = new RecurringPaymentService(mockRecurringPaymentRepository.Object, mockBillPaymentService.Object);
        var expectedNextRunDate = _baseReferenceDate.AddMonths(ThreeMonthsIncrement);

        // Act
        var actualNextRunDate = recurringPaymentService.ComputeNextRunDate(RecurringFrequency.Quarterly, _baseReferenceDate);

        // Assert
        actualNextRunDate.Should().Be(expectedNextRunDate);
    }

    [Fact]
    public void ComputeNextRunDate_WhenFrequencyIsYearly_ReturnsNextYear()
    {
        // Arrange
        var mockRecurringPaymentRepository = new Mock<IRecurringPaymentRepository>();
        var mockBillPaymentService = new Mock<IBillPaymentService>();
        var recurringPaymentService = new RecurringPaymentService(mockRecurringPaymentRepository.Object, mockBillPaymentService.Object);
        var expectedNextRunDate = _baseReferenceDate.AddYears(OneYearIncrement);

        // Act
        var actualNextRunDate = recurringPaymentService.ComputeNextRunDate(RecurringFrequency.Yearly, _baseReferenceDate);

        // Assert
        actualNextRunDate.Should().Be(expectedNextRunDate);
    }

    [Fact]
    public void ComputeNextRunDate_WhenFrequencyIsUnknown_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var mockRecurringPaymentRepository = new Mock<IRecurringPaymentRepository>();
        var mockBillPaymentService = new Mock<IBillPaymentService>();
        var recurringPaymentService = new RecurringPaymentService(mockRecurringPaymentRepository.Object, mockBillPaymentService.Object);
        var invalidFrequency = (RecurringFrequency)999;

        // Act
        Action computeNextRunDateAction = () => recurringPaymentService.ComputeNextRunDate(invalidFrequency, _baseReferenceDate);

        // Assert
        computeNextRunDateAction.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WhenCalledWithValidData_SavesAndReturnsActiveRecurringPayment()
    {
        // Arrange
        var mockRecurringPaymentRepository = new Mock<IRecurringPaymentRepository>();
        var mockBillPaymentService = new Mock<IBillPaymentService>();
        var recurringPaymentService = new RecurringPaymentService(mockRecurringPaymentRepository.Object, mockBillPaymentService.Object);

        var recurringPaymentDataTransferObject = new RecurringPaymentDto
        {
            UserId = DefaultUserId,
            BillerId = DefaultBillerId,
            SourceAccountId = DefaultSourceAccountId,
            Amount = DefaultPaymentAmount,
            IsPayInFull = false,
            Frequency = RecurringFrequency.Monthly,
            StartDate = _baseReferenceDate,
            EndDate = null
        };

        var expectedNextRunDate = _baseReferenceDate.AddMonths(OneMonthIncrement);
        var expectedToleranceForCreationTime = TimeSpan.FromSeconds(2);

        mockRecurringPaymentRepository
            .Setup(repository => repository.Add(It.IsAny<RecurringPayment>()))
            .Returns((RecurringPayment payment) => payment);

        // Act
        var createdRecurringPayment = recurringPaymentService.Create(recurringPaymentDataTransferObject);

        // Assert
        createdRecurringPayment.Should().NotBeNull();
        createdRecurringPayment.UserId.Should().Be(DefaultUserId);
        createdRecurringPayment.BillerId.Should().Be(DefaultBillerId);
        createdRecurringPayment.SourceAccountId.Should().Be(DefaultSourceAccountId);
        createdRecurringPayment.Amount.Should().Be(DefaultPaymentAmount);
        createdRecurringPayment.IsPayInFull.Should().BeFalse();
        createdRecurringPayment.Frequency.Should().Be(RecurringFrequency.Monthly);
        createdRecurringPayment.StartDate.Should().Be(_baseReferenceDate);
        createdRecurringPayment.EndDate.Should().BeNull();
        createdRecurringPayment.NextExecutionDate.Should().Be(expectedNextRunDate);
        createdRecurringPayment.Status.Should().Be(PaymentStatus.Active);
        createdRecurringPayment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, expectedToleranceForCreationTime);

        mockRecurringPaymentRepository.Verify(repository => repository.Add(It.IsAny<RecurringPayment>()), Times.Once);
    }

    [Fact]
    public void Pause_WhenPaymentExists_UpdatesStatusToPaused()
    {
        // Arrange
        var mockRecurringPaymentRepository = new Mock<IRecurringPaymentRepository>();
        var mockBillPaymentService = new Mock<IBillPaymentService>();
        var recurringPaymentService = new RecurringPaymentService(mockRecurringPaymentRepository.Object, mockBillPaymentService.Object);

        var existingRecurringPayment = new RecurringPayment
        {
            Id = ValidRecurringPaymentId,
            Status = PaymentStatus.Active
        };

        mockRecurringPaymentRepository
            .Setup(repository => repository.GetById(ValidRecurringPaymentId))
            .Returns(existingRecurringPayment);

        // Act
        recurringPaymentService.Pause(ValidRecurringPaymentId);

        // Assert
        existingRecurringPayment.Status.Should().Be(PaymentStatus.Paused);
        mockRecurringPaymentRepository.Verify(repository => repository.Update(existingRecurringPayment), Times.Once);
    }

    [Fact]
    public void Pause_WhenPaymentDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockRecurringPaymentRepository = new Mock<IRecurringPaymentRepository>();
        var mockBillPaymentService = new Mock<IBillPaymentService>();
        var recurringPaymentService = new RecurringPaymentService(mockRecurringPaymentRepository.Object, mockBillPaymentService.Object);

        mockRecurringPaymentRepository
            .Setup(repository => repository.GetById(InvalidRecurringPaymentId))
            .Returns((RecurringPayment)null!);

        // Act
        Action pausePaymentAction = () => recurringPaymentService.Pause(InvalidRecurringPaymentId);

        // Assert
        pausePaymentAction.Should().Throw<InvalidOperationException>()
            .WithMessage($"Recurring payment with ID {InvalidRecurringPaymentId} does not exist.");
        
        mockRecurringPaymentRepository.Verify(repository => repository.Update(It.IsAny<RecurringPayment>()), Times.Never);
    }

    [Fact]
    public void Resume_WhenPaymentExists_UpdatesStatusToActive()
    {
        // Arrange
        var mockRecurringPaymentRepository = new Mock<IRecurringPaymentRepository>();
        var mockBillPaymentService = new Mock<IBillPaymentService>();
        var recurringPaymentService = new RecurringPaymentService(mockRecurringPaymentRepository.Object, mockBillPaymentService.Object);

        var pausedRecurringPayment = new RecurringPayment
        {
            Id = ValidRecurringPaymentId,
            Status = PaymentStatus.Paused
        };

        mockRecurringPaymentRepository
            .Setup(repository => repository.GetById(ValidRecurringPaymentId))
            .Returns(pausedRecurringPayment);

        // Act
        recurringPaymentService.Resume(ValidRecurringPaymentId);

        // Assert
        pausedRecurringPayment.Status.Should().Be(PaymentStatus.Active);
        mockRecurringPaymentRepository.Verify(repository => repository.Update(pausedRecurringPayment), Times.Once);
    }

    [Fact]
    public void Resume_WhenPaymentDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockRecurringPaymentRepository = new Mock<IRecurringPaymentRepository>();
        var mockBillPaymentService = new Mock<IBillPaymentService>();
        var recurringPaymentService = new RecurringPaymentService(mockRecurringPaymentRepository.Object, mockBillPaymentService.Object);

        mockRecurringPaymentRepository
            .Setup(repository => repository.GetById(InvalidRecurringPaymentId))
            .Returns((RecurringPayment)null!);

        // Act
        Action resumePaymentAction = () => recurringPaymentService.Resume(InvalidRecurringPaymentId);

        // Assert
        resumePaymentAction.Should().Throw<InvalidOperationException>()
            .WithMessage($"Recurring payment with ID {InvalidRecurringPaymentId} does not exist.");
        
        mockRecurringPaymentRepository.Verify(repository => repository.Update(It.IsAny<RecurringPayment>()), Times.Never);
    }

    [Fact]
    public void Cancel_WhenPaymentExists_UpdatesStatusToCancelled()
    {
        // Arrange
        var mockRecurringPaymentRepository = new Mock<IRecurringPaymentRepository>();
        var mockBillPaymentService = new Mock<IBillPaymentService>();
        var recurringPaymentService = new RecurringPaymentService(mockRecurringPaymentRepository.Object, mockBillPaymentService.Object);

        var activeRecurringPayment = new RecurringPayment
        {
            Id = ValidRecurringPaymentId,
            Status = PaymentStatus.Active
        };

        mockRecurringPaymentRepository
            .Setup(repository => repository.GetById(ValidRecurringPaymentId))
            .Returns(activeRecurringPayment);

        // Act
        recurringPaymentService.Cancel(ValidRecurringPaymentId);

        // Assert
        activeRecurringPayment.Status.Should().Be(PaymentStatus.Cancelled);
        mockRecurringPaymentRepository.Verify(repository => repository.Update(activeRecurringPayment), Times.Once);
    }

    [Fact]
    public void Cancel_WhenPaymentDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockRecurringPaymentRepository = new Mock<IRecurringPaymentRepository>();
        var mockBillPaymentService = new Mock<IBillPaymentService>();
        var recurringPaymentService = new RecurringPaymentService(mockRecurringPaymentRepository.Object, mockBillPaymentService.Object);

        mockRecurringPaymentRepository
            .Setup(repository => repository.GetById(InvalidRecurringPaymentId))
            .Returns((RecurringPayment)null!);

        // Act
        Action cancelPaymentAction = () => recurringPaymentService.Cancel(InvalidRecurringPaymentId);

        // Assert
        cancelPaymentAction.Should().Throw<InvalidOperationException>()
            .WithMessage($"Recurring payment with ID {InvalidRecurringPaymentId} does not exist.");
        
        mockRecurringPaymentRepository.Verify(repository => repository.Update(It.IsAny<RecurringPayment>()), Times.Never);
    }

    [Fact]
    public void ProcessDuePayments_WhenActiveDuePaymentsExist_PaysBillsAndUpdatesNextExecutionDate()
    {
        // Arrange
        var mockRecurringPaymentRepository = new Mock<IRecurringPaymentRepository>();
        var mockBillPaymentService = new Mock<IBillPaymentService>();
        var recurringPaymentService = new RecurringPaymentService(mockRecurringPaymentRepository.Object, mockBillPaymentService.Object);

        var pastExecutionDate = _baseReferenceDate.AddDays(-1);
        var expectedNextExecutionDate = pastExecutionDate.AddMonths(OneMonthIncrement);
        
        var dueRecurringPayment = new RecurringPayment
        {
            Id = ValidRecurringPaymentId,
            UserId = DefaultUserId,
            SourceAccountId = DefaultSourceAccountId,
            BillerId = DefaultBillerId,
            Amount = DefaultPaymentAmount,
            IsPayInFull = false,
            Frequency = RecurringFrequency.Monthly,
            NextExecutionDate = pastExecutionDate,
            Status = PaymentStatus.Active
        };

        var duePaymentsList = new List<RecurringPayment> { dueRecurringPayment };

        mockRecurringPaymentRepository
            .Setup(repository => repository.GetDueBefore(It.IsAny<DateTime>()))
            .Returns(duePaymentsList);

        // Act
        recurringPaymentService.ProcessDuePayments();

        // Assert
        mockBillPaymentService.Verify(service => service.PayBill(It.Is<BillPaymentDto>(billPaymentDataTransferObject => 
            billPaymentDataTransferObject.UserId == DefaultUserId &&
            billPaymentDataTransferObject.SourceAccountId == DefaultSourceAccountId &&
            billPaymentDataTransferObject.BillerId == DefaultBillerId &&
            billPaymentDataTransferObject.Amount == DefaultPaymentAmount &&
            billPaymentDataTransferObject.IsPayInFull == false &&
            billPaymentDataTransferObject.BillerReference == string.Empty)), Times.Once);

        dueRecurringPayment.NextExecutionDate.Should().Be(expectedNextExecutionDate);
        mockRecurringPaymentRepository.Verify(repository => repository.Update(dueRecurringPayment), Times.Once);
    }

    [Fact]
    public void ProcessDuePayments_WhenBillPaymentFails_SetsStatusToFailed()
    {
        // Arrange
        var mockRecurringPaymentRepository = new Mock<IRecurringPaymentRepository>();
        var mockBillPaymentService = new Mock<IBillPaymentService>();
        var recurringPaymentService = new RecurringPaymentService(mockRecurringPaymentRepository.Object, mockBillPaymentService.Object);

        var pastExecutionDate = _baseReferenceDate.AddDays(-1);
        
        var dueRecurringPayment = new RecurringPayment
        {
            Id = ValidRecurringPaymentId,
            Frequency = RecurringFrequency.Monthly,
            NextExecutionDate = pastExecutionDate,
            Status = PaymentStatus.Active
        };

        var duePaymentsList = new List<RecurringPayment> { dueRecurringPayment };

        mockRecurringPaymentRepository
            .Setup(repository => repository.GetDueBefore(It.IsAny<DateTime>()))
            .Returns(duePaymentsList);

        mockBillPaymentService
            .Setup(service => service.PayBill(It.IsAny<BillPaymentDto>()))
            .Throws(new InvalidOperationException("Insufficient funds"));

        // Act
        recurringPaymentService.ProcessDuePayments();

        // Assert
        dueRecurringPayment.Status.Should().Be(PaymentStatus.Failed);
        dueRecurringPayment.NextExecutionDate.Should().Be(pastExecutionDate); // Ensures NextExecutionDate was not updated
        mockRecurringPaymentRepository.Verify(repository => repository.Update(dueRecurringPayment), Times.Once);
    }
}
