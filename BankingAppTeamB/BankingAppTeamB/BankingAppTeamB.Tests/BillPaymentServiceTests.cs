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
        var mockBillPaymentRepository = new Mock<IBillPaymentRepository>();
        var mockTransactionPipelineService = new Mock<ITransactionPipelineService>();
        var testedService = new BillPaymentService(mockBillPaymentRepository.Object, mockTransactionPipelineService.Object);

        // Act
        var result = billPaymentService.CalculateFee(SmallPaymentAmount);

        // Assert
        result.Should().Be(SmallPaymentFee);
    }

    [Fact]
    public void CalculateFee_WhenAmountIsLarge_ReturnsStandardPaymentFee()
    {
        // Arrange
        var mockBillPaymentRepository = new Mock<IBillPaymentRepository>();
        var mockTransactionPipelineService = new Mock<ITransactionPipelineService>();
        var testedService = new BillPaymentService(mockBillPaymentRepository.Object, mockTransactionPipelineService.Object);

        // Act
        var result = billPaymentService.CalculateFee(LargePaymentAmount);

        // Assert
        result.Should().Be(StandardPaymentFee);
    }

    [Fact]
    public void GetBillerDirectory_WhenCategoryIsNull_ReturnsAllActiveBillers()
    {
        // Arrange
        var mockBillPaymentRepository = new Mock<IBillPaymentRepository>();
        var mockTransactionPipelineService = new Mock<ITransactionPipelineService>();
        var testedService = new BillPaymentService(mockBillPaymentRepository.Object, mockTransactionPipelineService.Object);
        var expectedBillers = new List<Biller> { new Biller { Id = DefaultBillerId, Name = BillerName } };
        mockBillPaymentRepository.Setup(repository => repository.GetAllBillers(true)).Returns(expectedBillers);

        // Act
        var result = billPaymentService.GetBillerDirectory(null);

        // Assert
        result.Should().BeEquivalentTo(expectedBillers);
    }

    [Fact]
    public void GetBillerDirectory_WhenCategoryIsProvided_ReturnsFilteredBillers()
    {
        // Arrange
        var mockBillPaymentRepository = new Mock<IBillPaymentRepository>();
        var mockTransactionPipelineService = new Mock<ITransactionPipelineService>();
        var testedService = new BillPaymentService(mockBillPaymentRepository.Object, mockTransactionPipelineService.Object);
        var expectedBillers = new List<Biller> { new Biller { Id = DefaultBillerId, Name = BillerName, Category = BillerCategory } };
        mockBillPaymentRepository.Setup(repository => repository.SearchBillers(string.Empty, BillerCategory, true)).Returns(expectedBillers);

        // Act
        var result = billPaymentService.GetBillerDirectory(BillerCategory);

        // Assert
        result.Should().BeEquivalentTo(expectedBillers);
    }

    [Fact]
    public void SearchBillers_WithQueryOnly_ReturnsFilteredBillers()
    {
        // Arrange
        var mockBillPaymentRepository = new Mock<IBillPaymentRepository>();
        var mockTransactionPipelineService = new Mock<ITransactionPipelineService>();
        var testedService = new BillPaymentService(mockBillPaymentRepository.Object, mockTransactionPipelineService.Object);
        var expectedBillers = new List<Biller> { new Biller { Id = DefaultBillerId, Name = BillerName } };
        mockBillPaymentRepository.Setup(repository => repository.SearchBillers(SearchQuery, null, true)).Returns(expectedBillers);

        // Act
        var result = billPaymentService.SearchBillers(SearchQuery);

        // Assert
        result.Should().BeEquivalentTo(expectedBillers);
    }

    [Fact]
    public void SearchBillers_WithQueryAndCategory_ReturnsFilteredBillers()
    {
        // Arrange
        var mockBillPaymentRepository = new Mock<IBillPaymentRepository>();
        var mockTransactionPipelineService = new Mock<ITransactionPipelineService>();
        var testedService = new BillPaymentService(mockBillPaymentRepository.Object, mockTransactionPipelineService.Object);
        var expectedBillers = new List<Biller> { new Biller { Id = DefaultBillerId, Name = BillerName, Category = BillerCategory } };
        mockBillPaymentRepository.Setup(repository => repository.SearchBillers(SearchQuery, BillerCategory, true)).Returns(expectedBillers);

        // Act
        var result = billPaymentService.SearchBillers(SearchQuery, BillerCategory);

        // Assert
        result.Should().BeEquivalentTo(expectedBillers);
    }

    [Fact]
    public void SearchBillers_WithNullQueryAndCategory_UsesEmptyStringForQuery()
    {
        // Arrange
        var mockBillPaymentRepository = new Mock<IBillPaymentRepository>();
        var mockTransactionPipelineService = new Mock<ITransactionPipelineService>();
        var testedService = new BillPaymentService(mockBillPaymentRepository.Object, mockTransactionPipelineService.Object);
        var expectedBillers = new List<Biller> { new Biller { Id = DefaultBillerId, Name = BillerName, Category = BillerCategory } };
        mockBillPaymentRepository.Setup(repository => repository.SearchBillers(string.Empty, BillerCategory, true)).Returns(expectedBillers);

        // Act
        var result = billPaymentService.SearchBillers(null, BillerCategory);

        // Assert
        result.Should().BeEquivalentTo(expectedBillers);
    }

    [Fact]
    public void GetSavedBillers_WhenCalled_ReturnsUserSavedBillers()
    {
        // Arrange
        var mockBillPaymentRepository = new Mock<IBillPaymentRepository>();
        var mockTransactionPipelineService = new Mock<ITransactionPipelineService>();
        var testedService = new BillPaymentService(mockBillPaymentRepository.Object, mockTransactionPipelineService.Object);
        var expectedSavedBillers = new List<SavedBiller> { new SavedBiller { Id = 1, UserId = DefaultUserId, BillerId = DefaultBillerId } };
        mockBillPaymentRepository.Setup(repository => repository.GetSavedBillers(DefaultUserId)).Returns(expectedSavedBillers);

        // Act
        var result = billPaymentService.GetSavedBillers(DefaultUserId);

        // Assert
        result.Should().BeEquivalentTo(expectedSavedBillers);
    }

    [Fact]
    public void SaveBiller_WhenCalled_SavesBillerToRepository()
    {
        // Arrange
        var mockBillPaymentRepository = new Mock<IBillPaymentRepository>();
        var mockTransactionPipelineService = new Mock<ITransactionPipelineService>();
        var testedService = new BillPaymentService(mockBillPaymentRepository.Object, mockTransactionPipelineService.Object);
        var expectedToleranceForCreationTime = TimeSpan.FromSeconds(2);

        // Act
        billPaymentService.SaveBiller(DefaultUserId, DefaultBillerId, BillerNickname, BillerReference);

        // Assert
        mockBillPaymentRepository.Verify(repository => repository.SaveBiller(It.Is<SavedBiller>(savedBiller => savedBiller.UserId == DefaultUserId &&
            sb.BillerId == DefaultBillerId &&
            sb.Nickname == BillerNickname &&
            sb.DefaultReference == BillerReference &&
            (DateTime.UtcNow - sb.CreatedAt) < expectedToleranceForCreationTime)), Times.Once);
    }

    [Fact]
    public void RemoveSavedBiller_WhenCalled_DeletesBillerFromRepository()
    {
        // Arrange
        var mockBillPaymentRepository = new Mock<IBillPaymentRepository>();
        var mockTransactionPipelineService = new Mock<ITransactionPipelineService>();
        var testedService = new BillPaymentService(mockBillPaymentRepository.Object, mockTransactionPipelineService.Object);

        // Act
        billPaymentService.RemoveSavedBiller(DefaultBillerId);

        // Assert
        mockBillPaymentRepository.Verify(repository => repository.DeleteSavedBiller(DefaultBillerId), Times.Once);
    }

    [Fact]
    public void Requires2FA_WhenAmountIsBelowThreshold_ReturnsFalse()
    {
        // Arrange
        var mockBillPaymentRepository = new Mock<IBillPaymentRepository>();
        var mockTransactionPipelineService = new Mock<ITransactionPipelineService>();
        var testedService = new BillPaymentService(mockBillPaymentRepository.Object, mockTransactionPipelineService.Object);

        // Act
        var result = billPaymentService.Requires2FA(LargePaymentAmount);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Requires2FA_WhenAmountIsAtOrAboveThreshold_ReturnsTrue()
    {
        // Arrange
        var mockBillPaymentRepository = new Mock<IBillPaymentRepository>();
        var mockTransactionPipelineService = new Mock<ITransactionPipelineService>();
        var testedService = new BillPaymentService(mockBillPaymentRepository.Object, mockTransactionPipelineService.Object);

        // Act
        var result = billPaymentService.Requires2FA(TwoFaThresholdAmount);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PayBill_WhenBillerDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockBillPaymentRepository = new Mock<IBillPaymentRepository>();
        var mockTransactionPipelineService = new Mock<ITransactionPipelineService>();
        var testedService = new BillPaymentService(mockBillPaymentRepository.Object, mockTransactionPipelineService.Object);
        var dataTransferObject = new BillPaymentDto { BillerId = DefaultBillerId };
        mockBillPaymentRepository.Setup(repository => repository.GetBillerById(DefaultBillerId)).Returns((Biller)null);

        // Act
        Action payBillAction = () => billPaymentService.PayBill(dataTransferObject);

        // Assert
        payBillAction.Should().Throw<InvalidOperationException>().WithMessage($"Biller with ID {DefaultBillerId} does not exist.");
        mockTransactionPipelineService.Verify(service => billPaymentService.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void PayBill_WhenValid_ExecutesPipelineAndSavesBillPayment()
    {
        // Arrange
        var mockBillPaymentRepository = new Mock<IBillPaymentRepository>();
        var mockTransactionPipelineService = new Mock<ITransactionPipelineService>();
        var testedService = new BillPaymentService(mockBillPaymentRepository.Object, mockTransactionPipelineService.Object);
        var dataTransferObject = new BillPaymentDto
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
        mockBillPaymentRepository.Setup(repository => repository.GetBillerById(DefaultBillerId)).Returns(biller);
        mockTransactionPipelineService.Setup(service => billPaymentService.RunPipeline(It.IsAny<PipelineContext>(), ValidTwoFaToken)).Returns(transaction);
        var expectedBillPayment = new BillPayment
        {
            Id = 456,
            BillerReference = BillerReference,
            ReceiptNumber = "some-receipt"
        };
        mockBillPaymentRepository.Setup(repository => repository.Add(It.IsAny<BillPayment>())).Returns(expectedBillPayment);

        // Act
        var result = billPaymentService.PayBill(dataTransferObject);

        // Assert
        result.Should().Be(expectedBillPayment);
        mockBillPaymentRepository.Verify(repository => repository.Add(It.Is<BillPayment>(billPayment => billPayment.UserId == DefaultUserId &&
            bp.SourceAccountId == DefaultSourceAccountId &&
            bp.BillerId == DefaultBillerId &&
            bp.TransactionId == transaction.Id &&
            bp.BillerReference == BillerReference &&
            bp.Amount == LargePaymentAmount &&
            bp.Fee == StandardPaymentFee &&
            bp.Status == PaymentStatus.Completed &&
            bp.ReceiptNumber.StartsWith("RCP-"))), Times.Once);
    }
}
