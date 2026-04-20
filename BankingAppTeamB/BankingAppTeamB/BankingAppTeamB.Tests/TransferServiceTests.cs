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

public class TransferServiceTests
{
    private const int DefaultUserId = 1;
    private const int DefaultSourceAccountId = 100;
    private const decimal ValidAmount = 500m;
    private const decimal AmountRequiringTwoFa = 1500m;
    private const string ValidCurrency = "EUR";
    private const string RecipientName = "John Doe";
    private const string ValidIbanRomanian = "RO12XXXX000000000000000";
    private const string InvalidIban = "INVALID123";
    private const string DefaultReference = "Invoice payment";
    private const string ValidTwoFaToken = "123456";

    [Fact]
    public void ExecuteTransfer_WhenIbanIsInvalid_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockTransferRepository = new Mock<ITransferRepository>();
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new TransferService(mockTransferRepository.Object, mockBeneficiaryRepository.Object, mockPipelineService.Object);
        var transferDto = new TransferDto { RecipientIBAN = InvalidIban };

        // Act
        Action executeAction = () => service.ExecuteTransfer(transferDto);

        // Assert
        executeAction.Should().Throw<InvalidOperationException>().WithMessage("Recipient IBAN is invalid.");
        mockPipelineService.Verify(s => s.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ExecuteTransfer_WhenValid_ExecutesPipelineAndSavesTransfer()
    {
        // Arrange
        var mockTransferRepository = new Mock<ITransferRepository>();
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new TransferService(mockTransferRepository.Object, mockBeneficiaryRepository.Object, mockPipelineService.Object);
        
        var transferDto = new TransferDto 
        { 
            UserId = DefaultUserId,
            SourceAccountId = DefaultSourceAccountId,
            Amount = ValidAmount,
            Currency = ValidCurrency,
            RecipientName = RecipientName,
            RecipientIBAN = ValidIbanRomanian,
            Reference = DefaultReference,
            TwoFAToken = ValidTwoFaToken
        };

        var expectedTransactionId = 123;
        var transaction = new Transaction { Id = expectedTransactionId };

        mockPipelineService.Setup(s => s.RunPipeline(It.IsAny<PipelineContext>(), ValidTwoFaToken)).Returns(transaction);
        mockBeneficiaryRepository.Setup(r => r.GetByUserId(DefaultUserId)).Returns(new List<Beneficiary>());

        var expectedToleranceForCreationTime = TimeSpan.FromSeconds(2);

        // Act
        var result = service.ExecuteTransfer(transferDto);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(DefaultUserId);
        result.SourceAccountId.Should().Be(DefaultSourceAccountId);
        result.TransactionId.Should().Be(expectedTransactionId);
        result.RecipientName.Should().Be(RecipientName);
        result.RecipientIBAN.Should().Be(ValidIbanRomanian);
        result.RecipientBankName.Should().Be("Romanian Bank");
        result.Amount.Should().Be(ValidAmount);
        result.Currency.Should().Be(ValidCurrency);
        result.Fee.Should().Be(0);
        result.Reference.Should().Be(DefaultReference);
        result.Status.Should().Be(TransferStatus.Completed);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, expectedToleranceForCreationTime);

        mockTransferRepository.Verify(r => r.Add(It.IsAny<Transfer>()), Times.Once);
    }

    [Fact]
    public void ExecuteTransfer_WhenBeneficiaryExists_UpdatesBeneficiaryStats()
    {
        // Arrange
        var mockTransferRepository = new Mock<ITransferRepository>();
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new TransferService(mockTransferRepository.Object, mockBeneficiaryRepository.Object, mockPipelineService.Object);
        
        var transferDto = new TransferDto 
        { 
            UserId = DefaultUserId,
            RecipientIBAN = ValidIbanRomanian,
            Amount = ValidAmount
        };

        mockPipelineService.Setup(s => s.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>())).Returns(new Transaction());
        
        var existingBeneficiary = new Beneficiary 
        { 
            IBAN = ValidIbanRomanian, 
            TransferCount = 1, 
            TotalAmountSent = 100m 
        };
        
        mockBeneficiaryRepository.Setup(r => r.GetByUserId(DefaultUserId)).Returns(new List<Beneficiary> { existingBeneficiary });

        // Act
        service.ExecuteTransfer(transferDto);

        // Assert
        existingBeneficiary.TransferCount.Should().Be(2);
        existingBeneficiary.TotalAmountSent.Should().Be(100m + ValidAmount);
        mockBeneficiaryRepository.Verify(r => r.Update(existingBeneficiary), Times.Once);
    }

    [Fact]
    public void ValidateIBAN_WhenIbanIsValid_ReturnsTrue()
    {
        // Arrange
        var mockTransferRepository = new Mock<ITransferRepository>();
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new TransferService(mockTransferRepository.Object, mockBeneficiaryRepository.Object, mockPipelineService.Object);

        // Act
        var result = service.ValidateIBAN(ValidIbanRomanian);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateIBAN_WhenIbanIsInvalid_ReturnsFalse()
    {
        // Arrange
        var mockTransferRepository = new Mock<ITransferRepository>();
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new TransferService(mockTransferRepository.Object, mockBeneficiaryRepository.Object, mockPipelineService.Object);

        // Act
        var result = service.ValidateIBAN(InvalidIban);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetBankNameFromIBAN_WhenIbanIsEmpty_ReturnsUnknownBank()
    {
        // Arrange
        var mockTransferRepository = new Mock<ITransferRepository>();
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new TransferService(mockTransferRepository.Object, mockBeneficiaryRepository.Object, mockPipelineService.Object);

        // Act
        var result = service.GetBankNameFromIBAN("");

        // Assert
        result.Should().Be("Unknown Bank");
    }

    [Fact]
    public void GetBankNameFromIBAN_WhenIbanStartsRO_ReturnsRomanianBank()
    {
        // Arrange
        var mockTransferRepository = new Mock<ITransferRepository>();
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new TransferService(mockTransferRepository.Object, mockBeneficiaryRepository.Object, mockPipelineService.Object);

        // Act
        var result = service.GetBankNameFromIBAN(ValidIbanRomanian);

        // Assert
        result.Should().Be("Romanian Bank");
    }

    [Fact]
    public void GetFxPreview_WhenSourceAndTargetCurrencyAreSame_ReturnsRateOne()
    {
        // Arrange
        var mockTransferRepository = new Mock<ITransferRepository>();
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockExchangeService = new Mock<IExchangeService>();
        var service = new TransferService(mockTransferRepository.Object, mockBeneficiaryRepository.Object, mockPipelineService.Object, mockExchangeService.Object);

        // Act
        var result = service.GetFxPreview("EUR", "EUR", ValidAmount);

        // Assert
        result.Rate.Should().Be(1m);
        result.ConvertedAmount.Should().Be(ValidAmount);
        mockExchangeService.Verify(s => s.GetLiveRates(), Times.Never);
    }

    [Fact]
    public void GetFxPreview_WhenExchangeServiceIsNull_ReturnsRateOne()
    {
        // Arrange
        var mockTransferRepository = new Mock<ITransferRepository>();
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new TransferService(mockTransferRepository.Object, mockBeneficiaryRepository.Object, mockPipelineService.Object); // null exchange service

        // Act
        var result = service.GetFxPreview("EUR", "USD", ValidAmount);

        // Assert
        result.Rate.Should().Be(1m);
        result.ConvertedAmount.Should().Be(ValidAmount);
    }

    [Fact]
    public void GetFxPreview_WhenPairNotFound_ReturnsRateOne()
    {
        // Arrange
        var mockTransferRepository = new Mock<ITransferRepository>();
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockExchangeService = new Mock<IExchangeService>();
        mockExchangeService.Setup(s => s.GetLiveRates()).Returns(new Dictionary<string, decimal>());
        var service = new TransferService(mockTransferRepository.Object, mockBeneficiaryRepository.Object, mockPipelineService.Object, mockExchangeService.Object);

        // Act
        var result = service.GetFxPreview("EUR", "USD", ValidAmount);

        // Assert
        result.Rate.Should().Be(1m);
        result.ConvertedAmount.Should().Be(ValidAmount);
    }

    [Fact]
    public void GetFxPreview_WhenPairExists_ReturnsCorrectConversion()
    {
        // Arrange
        var mockTransferRepository = new Mock<ITransferRepository>();
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockExchangeService = new Mock<IExchangeService>();
        var liveRates = new Dictionary<string, decimal> { { "EUR/USD", 1.2m } };
        mockExchangeService.Setup(s => s.GetLiveRates()).Returns(liveRates);
        var service = new TransferService(mockTransferRepository.Object, mockBeneficiaryRepository.Object, mockPipelineService.Object, mockExchangeService.Object);
        var amount = 100m;
        var expectedConvertedAmount = 120m;

        // Act
        var result = service.GetFxPreview("EUR", "USD", amount);

        // Assert
        result.Rate.Should().Be(1.2m);
        result.ConvertedAmount.Should().Be(expectedConvertedAmount);
    }

    [Fact]
    public void GetHistory_WhenCalled_ReturnsHistoryFromRepository()
    {
        // Arrange
        var mockTransferRepository = new Mock<ITransferRepository>();
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new TransferService(mockTransferRepository.Object, mockBeneficiaryRepository.Object, mockPipelineService.Object);
        var expectedTransfers = new List<Transfer> { new Transfer { UserId = DefaultUserId } };
        mockTransferRepository.Setup(r => r.GetByUserId(DefaultUserId)).Returns(expectedTransfers);

        // Act
        var result = service.GetHistory(DefaultUserId);

        // Assert
        result.Should().BeEquivalentTo(expectedTransfers);
    }

    [Fact]
    public void Requires2FA_WhenAmountIsBelowThreshold_ReturnsFalse()
    {
        // Arrange
        var mockTransferRepository = new Mock<ITransferRepository>();
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new TransferService(mockTransferRepository.Object, mockBeneficiaryRepository.Object, mockPipelineService.Object);

        // Act
        var result = service.Requires2FA(ValidAmount);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Requires2FA_WhenAmountIsAtOrAboveThreshold_ReturnsTrue()
    {
        // Arrange
        var mockTransferRepository = new Mock<ITransferRepository>();
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var service = new TransferService(mockTransferRepository.Object, mockBeneficiaryRepository.Object, mockPipelineService.Object);

        // Act
        var result = service.Requires2FA(AmountRequiringTwoFa);

        // Assert
        result.Should().BeTrue();
    }
}
