using System;
using System.Collections.Generic;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Repositories;
using BankingAppTeamB.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace BankingAppTeamB.Tests;

public class BillPaymentServiceTests
{
    private const int DefaultUserId = 1;
    private const int DefaultBillerId = 10;
    private const int DefaultSourceAccountId = 100;
    private const decimal SmallPaymentAmount = 50m;
    private const decimal LargePaymentAmount = 150m;
    private const decimal TwoFaThresholdAmount = 1500m;
    private const decimal SmallPaymentFee = 0.50m;
    private const decimal StandardPaymentFee = 1.00m;
    private const string BillerCategory = "Utilities";
    private const string BillerName = "Electric Company";
    private const string BillerReference = "INV-123";
    private const string BillerNickname = "Electricity";
    private const string SearchQuery = "Elec";
    private const string ValidTwoFaToken = "123456";

    [Fact]
    public void CalculateFee_WhenAmountIsSmall_ReturnsSmallPaymentFee()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);

        // Act
        var actualResult = service.CalculateFee(SmallPaymentAmount);

        // Assert
        actualResult.Should().Be(SmallPaymentFee);
    }

    [Fact]
    public void CalculateFee_WhenAmountIsLarge_ReturnsStandardPaymentFee()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);

        // Act
        var actualResult = service.CalculateFee(LargePaymentAmount);

        // Assert
        actualResult.Should().Be(StandardPaymentFee);
    }

    [Fact]
    public void GetBillerDirectory_WhenCategoryIsNull_ReturnsAllActiveBillers()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);
        var expectedBillers = new List<Biller> { new Biller { Id = DefaultBillerId, Name = BillerName } };
        mockRepository.Setup(mockRepositoryInstance => mockRepositoryInstance.GetAllBillers(true)).Returns(expectedBillers);

        // Act
        var actualResult = service.GetBillerDirectory(null);

        // Assert
        actualResult.Should().BeEquivalentTo(expectedBillers);
    }

    [Fact]
    public void GetBillerDirectory_WhenCategoryIsProvided_ReturnsFilteredBillers()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);
        var expectedBillers = new List<Biller> { new Biller { Id = DefaultBillerId, Name = BillerName, Category = BillerCategory } };
        mockRepository.Setup(mockRepositoryInstance => mockRepositoryInstance.SearchBillers(string.Empty, BillerCategory, true)).Returns(expectedBillers);

        // Act
        var actualResult = service.GetBillerDirectory(BillerCategory);

        // Assert
        actualResult.Should().BeEquivalentTo(expectedBillers);
    }

    [Fact]
    public void SearchBillers_WithQueryOnly_ReturnsFilteredBillers()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);
        var expectedBillers = new List<Biller> { new Biller { Id = DefaultBillerId, Name = BillerName } };
        mockRepository.Setup(mockRepositoryInstance => mockRepositoryInstance.SearchBillers(SearchQuery, null, true)).Returns(expectedBillers);

        // Act
        var actualResult = service.SearchBillers(SearchQuery);

        // Assert
        actualResult.Should().BeEquivalentTo(expectedBillers);
    }

    [Fact]
    public void SearchBillers_WithQueryAndCategory_ReturnsFilteredBillers()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);
        var expectedBillers = new List<Biller> { new Biller { Id = DefaultBillerId, Name = BillerName, Category = BillerCategory } };
        mockRepository.Setup(mockRepositoryInstance => mockRepositoryInstance.SearchBillers(SearchQuery, BillerCategory, true)).Returns(expectedBillers);

        // Act
        var actualResult = service.SearchBillers(SearchQuery, BillerCategory);

        // Assert
        actualResult.Should().BeEquivalentTo(expectedBillers);
    }

    [Fact]
    public void SearchBillers_WithNullQueryAndCategory_UsesEmptyStringForQuery()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);
        var expectedBillers = new List<Biller> { new Biller { Id = DefaultBillerId, Name = BillerName, Category = BillerCategory } };
        mockRepository.Setup(mockRepositoryInstance => mockRepositoryInstance.SearchBillers(string.Empty, BillerCategory, true)).Returns(expectedBillers);

        // Act
        var actualResult = service.SearchBillers(null, BillerCategory);

        // Assert
        actualResult.Should().BeEquivalentTo(expectedBillers);
    }

    [Fact]
    public void GetSavedBillers_WhenCalled_ReturnsUserSavedBillers()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);
        var expectedSavedBillers = new List<SavedBiller> { new SavedBiller { Id = 1, UserId = DefaultUserId, BillerId = DefaultBillerId } };
        mockRepository.Setup(mockRepositoryInstance => mockRepositoryInstance.GetSavedBillers(DefaultUserId)).Returns(expectedSavedBillers);

        // Act
        var actualResult = service.GetSavedBillers(DefaultUserId);

        // Assert
        actualResult.Should().BeEquivalentTo(expectedSavedBillers);
    }

    [Fact]
    public void SaveBiller_WhenCalled_SavesBillerToRepository()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);
        var expectedToleranceForCreationTime = TimeSpan.FromSeconds(2);

        // Act
        service.SaveBiller(DefaultUserId, DefaultBillerId, BillerNickname, BillerReference);

        // Assert
        mockRepository.Verify(mockRepositoryInstance => mockRepositoryInstance.SaveBiller(It.Is<SavedBiller>(savedBillerEntry => savedBillerEntry.UserId == DefaultUserId &&
            savedBillerEntry.BillerId == DefaultBillerId &&
            savedBillerEntry.Nickname == BillerNickname &&
            savedBillerEntry.DefaultReference == BillerReference &&
            (DateTime.UtcNow - savedBillerEntry.CreatedAt) < expectedToleranceForCreationTime)), Times.Once);
    }

    [Fact]
    public void RemoveSavedBiller_WhenCalled_DeletesBillerFromRepository()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);

        // Act
        service.RemoveSavedBiller(DefaultBillerId);

        // Assert
        mockRepository.Verify(mockRepositoryInstance => mockRepositoryInstance.DeleteSavedBiller(DefaultBillerId), Times.Once);
    }

    [Fact]
    public void Requires2FA_WhenAmountIsBelowThreshold_ReturnsFalse()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);

        // Act
        var actualResult = service.Requires2FA(LargePaymentAmount);

        // Assert
        actualResult.Should().BeFalse();
    }

    [Fact]
    public void Requires2FA_WhenAmountIsAtOrAboveThreshold_ReturnsTrue()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);

        // Act
        var actualResult = service.Requires2FA(TwoFaThresholdAmount);

        // Assert
        actualResult.Should().BeTrue();
    }

    [Fact]
    public void PayBill_WhenBillerDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);
        var billPaymentRequest = new BillPaymentDto { BillerId = DefaultBillerId };
        mockRepository.Setup(mockRepositoryInstance => mockRepositoryInstance.GetBillerById(DefaultBillerId)).Returns((Biller)null);

        // Act
        Action payBillOperation = () => service.PayBill(billPaymentRequest);

        // Assert
        payBillOperation.Should().Throw<InvalidOperationException>().WithMessage($"Biller with ID {DefaultBillerId} does not exist.");
        mockPipelineService.Verify(mockServiceDependency => mockServiceDependency.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void PayBill_WhenValid_ExecutesPipelineAndSavesBillPayment()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);
        var billPaymentRequest = new BillPaymentDto
        {
            UserId = DefaultUserId,
            SourceAccountId = DefaultSourceAccountId,
            BillerId = DefaultBillerId,
            Amount = LargePaymentAmount,
            BillerReference = BillerReference,
            TwoFAToken = ValidTwoFaToken
        };
        var biller = new Biller { Id = DefaultBillerId, Name = BillerName };
        var transaction = new Transaction { Id = 123 };
        mockRepository.Setup(mockRepositoryInstance => mockRepositoryInstance.GetBillerById(DefaultBillerId)).Returns(biller);
        mockPipelineService.Setup(mockServiceDependency => mockServiceDependency.RunPipeline(It.IsAny<PipelineContext>(), ValidTwoFaToken)).Returns(transaction);
        var expectedBillPayment = new BillPayment
        {
            Id = 456,
            BillerReference = BillerReference,
            ReceiptNumber = "some-receipt"
        };
        mockRepository.Setup(mockRepositoryInstance => mockRepositoryInstance.Add(It.IsAny<BillPayment>())).Returns(expectedBillPayment);

        // Act
        var actualResult = service.PayBill(billPaymentRequest);

        // Assert
        actualResult.Should().Be(expectedBillPayment);
        mockRepository.Verify(mockRepositoryInstance => mockRepositoryInstance.Add(It.Is<BillPayment>(billPaymentRecord =>
            billPaymentRecord.UserId == DefaultUserId &&
            billPaymentRecord.SourceAccountId == DefaultSourceAccountId &&
            billPaymentRecord.BillerId == DefaultBillerId &&
            billPaymentRecord.TransactionId == transaction.Id &&
            billPaymentRecord.BillerReference == BillerReference &&
            billPaymentRecord.Amount == LargePaymentAmount &&
            billPaymentRecord.Fee == StandardPaymentFee &&
            billPaymentRecord.Status == PaymentStatus.Completed &&
            billPaymentRecord.ReceiptNumber.StartsWith("RCP-"))), Times.Once);
    }
}
