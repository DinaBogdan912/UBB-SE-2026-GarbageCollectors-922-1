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
        var result = service.CalculateFee(SmallPaymentAmount);

        // Assert
        result.Should().Be(SmallPaymentFee);
    }

    [Fact]
    public void CalculateFee_WhenAmountIsLarge_ReturnsStandardPaymentFee()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);

        // Act
        var result = service.CalculateFee(LargePaymentAmount);

        // Assert
        result.Should().Be(StandardPaymentFee);
    }

    [Fact]
    public void GetBillerDirectory_WhenCategoryIsNull_ReturnsAllActiveBillers()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);
        var expectedBillers = new List<Biller> { new Biller { Id = DefaultBillerId, Name = BillerName } };
        mockRepository.Setup(r => r.GetAllBillers(true)).Returns(expectedBillers);

        // Act
        var result = service.GetBillerDirectory(null);

        // Assert
        result.Should().BeEquivalentTo(expectedBillers);
    }

    [Fact]
    public void GetBillerDirectory_WhenCategoryIsProvided_ReturnsFilteredBillers()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);
        var expectedBillers = new List<Biller> { new Biller { Id = DefaultBillerId, Name = BillerName, Category = BillerCategory } };
        mockRepository.Setup(r => r.SearchBillers(string.Empty, BillerCategory, true)).Returns(expectedBillers);

        // Act
        var result = service.GetBillerDirectory(BillerCategory);

        // Assert
        result.Should().BeEquivalentTo(expectedBillers);
    }

    [Fact]
    public void SearchBillers_WithQueryOnly_ReturnsFilteredBillers()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);
        var expectedBillers = new List<Biller> { new Biller { Id = DefaultBillerId, Name = BillerName } };
        mockRepository.Setup(r => r.SearchBillers(SearchQuery, null, true)).Returns(expectedBillers);

        // Act
        var result = service.SearchBillers(SearchQuery);

        // Assert
        result.Should().BeEquivalentTo(expectedBillers);
    }

    [Fact]
    public void SearchBillers_WithQueryAndCategory_ReturnsFilteredBillers()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);
        var expectedBillers = new List<Biller> { new Biller { Id = DefaultBillerId, Name = BillerName, Category = BillerCategory } };
        mockRepository.Setup(r => r.SearchBillers(SearchQuery, BillerCategory, true)).Returns(expectedBillers);

        // Act
        var result = service.SearchBillers(SearchQuery, BillerCategory);

        // Assert
        result.Should().BeEquivalentTo(expectedBillers);
    }

    [Fact]
    public void SearchBillers_WithNullQueryAndCategory_UsesEmptyStringForQuery()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);
        var expectedBillers = new List<Biller> { new Biller { Id = DefaultBillerId, Name = BillerName, Category = BillerCategory } };
        mockRepository.Setup(r => r.SearchBillers(string.Empty, BillerCategory, true)).Returns(expectedBillers);

        // Act
        var result = service.SearchBillers(null, BillerCategory);

        // Assert
        result.Should().BeEquivalentTo(expectedBillers);
    }

    [Fact]
    public void GetSavedBillers_WhenCalled_ReturnsUserSavedBillers()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);
        var expectedSavedBillers = new List<SavedBiller> { new SavedBiller { Id = 1, UserId = DefaultUserId, BillerId = DefaultBillerId } };
        mockRepository.Setup(r => r.GetSavedBillers(DefaultUserId)).Returns(expectedSavedBillers);

        // Act
        var result = service.GetSavedBillers(DefaultUserId);

        // Assert
        result.Should().BeEquivalentTo(expectedSavedBillers);
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
        mockRepository.Verify(r => r.SaveBiller(It.Is<SavedBiller>(sb => sb.UserId == DefaultUserId &&
            sb.BillerId == DefaultBillerId &&
            sb.Nickname == BillerNickname &&
            sb.DefaultReference == BillerReference &&
            (DateTime.UtcNow - sb.CreatedAt) < expectedToleranceForCreationTime)), Times.Once);
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
        mockRepository.Verify(r => r.DeleteSavedBiller(DefaultBillerId), Times.Once);
    }

    [Fact]
    public void Requires2FA_WhenAmountIsBelowThreshold_ReturnsFalse()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);

        // Act
        var result = service.Requires2FA(LargePaymentAmount);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Requires2FA_WhenAmountIsAtOrAboveThreshold_ReturnsTrue()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);

        // Act
        var result = service.Requires2FA(TwoFaThresholdAmount);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void PayBill_WhenBillerDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);
        var dto = new BillPaymentDto { BillerId = DefaultBillerId };
        mockRepository.Setup(r => r.GetBillerById(DefaultBillerId)).Returns((Biller)null);

        // Act
        Action payBillAction = () => service.PayBill(dto);

        // Assert
        payBillAction.Should().Throw<InvalidOperationException>().WithMessage($"Biller with ID {DefaultBillerId} does not exist.");
        mockPipelineService.Verify(s => s.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void PayBill_WhenValid_ExecutesPipelineAndSavesBillPayment()
    {
        // Arrange
        var mockRepository = new Mock<IBillPaymentRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new BillPaymentService(mockRepository.Object, mockPipelineService.Object);
        var dto = new BillPaymentDto
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
        mockRepository.Setup(r => r.GetBillerById(DefaultBillerId)).Returns(biller);
        mockPipelineService.Setup(s => s.RunPipeline(It.IsAny<PipelineContext>(), ValidTwoFaToken)).Returns(transaction);
        var expectedBillPayment = new BillPayment
        {
            Id = 456,
            BillerReference = BillerReference,
            ReceiptNumber = "some-receipt"
        };
        mockRepository.Setup(r => r.Add(It.IsAny<BillPayment>())).Returns(expectedBillPayment);

        // Act
        var result = service.PayBill(dto);

        // Assert
        result.Should().Be(expectedBillPayment);
        mockRepository.Verify(r => r.Add(It.Is<BillPayment>(bp =>
            bp.UserId == DefaultUserId &&
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
