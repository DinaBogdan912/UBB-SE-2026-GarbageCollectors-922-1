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

public class ExchangeServiceTests
{
    private const int DefaultUserId = 1;
    private const int DefaultSourceAccountId = 100;
    private const int DefaultTargetAccountId = 101;
    private const int ValidAlertId = 10;
    private const decimal MinimumCommissionAmount = 0.50m;
    private const decimal SmallExchangeAmount = 50m;
    private const decimal LargeExchangeAmount = 1000m;
    private const decimal ExpectedLargeCommission = 5m;
    private const string BaseCurrency = "EUR";
    private const string TargetCurrency = "USD";
    private const decimal SeedExchangeRate = 1.15m;
    
    [Fact]
    public void GetLiveRates_WhenCalled_ReturnsRatesDictionary()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);

        // Act
        var result = service.GetLiveRates();

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey($"{BaseCurrency}/{TargetCurrency}");
        result.Should().ContainKey($"{TargetCurrency}/{BaseCurrency}");
    }

    [Fact]
    public void GetRate_WhenDirectPairExists_ReturnsRate()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);

        // Act
        var result = service.GetRate(BaseCurrency, TargetCurrency);

        // Assert
        result.Should().Be(SeedExchangeRate);
    }

    [Fact]
    public void GetRate_WhenInversePairExists_ReturnsInverseRate()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);
        var expectedInverseRate = Math.Round(1 / SeedExchangeRate, 2);

        // Act
        var result = service.GetRate(TargetCurrency, BaseCurrency);

        // Assert
        result.Should().Be(expectedInverseRate);
    }

    [Fact]
    public void GetRate_WhenPairDoesNotExist_ThrowsException()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);

        // Act
        Action getRateAction = () => service.GetRate("JPY", "CAD");

        // Assert
        getRateAction.Should().Throw<Exception>().WithMessage("Rate not found for pair JPY/CAD");
    }

    [Fact]
    public void LockRate_WhenCalled_ReturnsLockedRateAndStoresLock()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);
        var expectedToleranceForLockTime = TimeSpan.FromSeconds(2);

        // Act
        var result = service.LockRate(DefaultUserId, BaseCurrency, TargetCurrency);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(DefaultUserId);
        result.CurrencyPair.Should().Be($"{BaseCurrency}/{TargetCurrency}");
        result.Rate.Should().Be(SeedExchangeRate);
        result.LockedAt.Should().BeCloseTo(DateTime.Now, expectedToleranceForLockTime);
        
        service.IsRateLockValid(DefaultUserId).Should().BeTrue();
    }

    [Fact]
    public void IsRateLockValid_WhenNoLockExists_ReturnsFalse()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);

        // Act
        var result = service.IsRateLockValid(DefaultUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CalculateCommission_WhenAmountIsSmall_ReturnsMinimumCommission()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);

        // Act
        var result = service.CalculateCommission(SmallExchangeAmount);

        // Assert
        result.Should().Be(MinimumCommissionAmount);
    }

    [Fact]
    public void CalculateCommission_WhenAmountIsLarge_ReturnsPercentageCommission()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);

        // Act
        var result = service.CalculateCommission(LargeExchangeAmount);

        // Assert
        result.Should().Be(ExpectedLargeCommission);
    }

    [Fact]
    public void CalculateTargetAmount_WhenCalled_ReturnsTargetAmountMinusCommission()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);
        var expectedTargetAmount = (LargeExchangeAmount * SeedExchangeRate) - ExpectedLargeCommission;

        // Act
        var result = service.CalculateTargetAmount(LargeExchangeAmount, SeedExchangeRate);

        // Assert
        result.Should().Be(expectedTargetAmount);
    }

    [Fact]
    public void ExecuteExchange_WhenLockIsInvalid_ThrowsException()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);
        var dto = new ExchangeDto { UserId = DefaultUserId };

        // Act
        Action executeAction = () => service.ExecuteExchange(dto);

        // Assert
        executeAction.Should().Throw<Exception>().WithMessage("No valid rate lock found or the 3-second window has expired.");
        mockPipelineService.Verify(s => s.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ExecuteExchange_WhenValid_ExecutesPipelineSavesTransactionAndCreditsAccount()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);
        
        service.LockRate(DefaultUserId, BaseCurrency, TargetCurrency);
        
        var dto = new ExchangeDto
        {
            UserId = DefaultUserId,
            SourceAccountId = DefaultSourceAccountId,
            TargetAccountId = DefaultTargetAccountId,
            SourceCurrency = BaseCurrency,
            TargetCurrency = TargetCurrency,
            SourceAmount = LargeExchangeAmount
        };
        
        var transaction = new Transaction { Id = 123 };
        mockPipelineService.Setup(s => s.RunPipeline(It.IsAny<PipelineContext>(), null)).Returns(transaction);
        
        var expectedToleranceForCreationTime = TimeSpan.FromSeconds(2);

        // Act
        var result = service.ExecuteExchange(dto);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(DefaultUserId);
        result.SourceAccountId.Should().Be(DefaultSourceAccountId);
        result.TargetAccountId.Should().Be(DefaultTargetAccountId);
        result.TransactionId.Should().Be(transaction.Id);
        result.SourceCurrency.Should().Be(BaseCurrency);
        result.TargetCurrency.Should().Be(TargetCurrency);
        result.SourceAmount.Should().Be(LargeExchangeAmount);
        result.ExchangeRate.Should().Be(SeedExchangeRate);
        result.Commission.Should().Be(ExpectedLargeCommission);
        result.Status.Should().Be(TransferStatus.Completed);
        result.CreatedAt.Should().BeCloseTo(DateTime.Now, expectedToleranceForCreationTime);
        
        mockPipelineService.Verify(s => s.RunPipeline(It.IsAny<PipelineContext>(), null), Times.Once);
        mockAccountService.Verify(s => s.CreditAccount(DefaultTargetAccountId, result.TargetAmount), Times.Once);
        mockExchangeRepository.Verify(r => r.Add(It.IsAny<ExchangeTransaction>()), Times.Once);
        
        // Lock should be cleared after execution
        service.IsRateLockValid(DefaultUserId).Should().BeFalse();
    }

    [Fact]
    public void ClearLocks_WhenCalled_RemovesLockForUser()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);
        
        service.LockRate(DefaultUserId, BaseCurrency, TargetCurrency);

        // Act
        service.ClearLocks(DefaultUserId);

        // Assert
        service.IsRateLockValid(DefaultUserId).Should().BeFalse();
    }

    [Fact]
    public void GetUserAlerts_WhenCalled_ReturnsUntriggeredAlerts()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);
        var expectedAlerts = new List<RateAlert> { new RateAlert(DefaultUserId, BaseCurrency, TargetCurrency, SeedExchangeRate, false) };
        mockExchangeRepository.Setup(r => r.GetAlertsByUser(DefaultUserId, false)).Returns(expectedAlerts);

        // Act
        var result = service.GetUserAlerts(DefaultUserId);

        // Assert
        result.Should().BeEquivalentTo(expectedAlerts);
    }

    [Fact]
    public void CreateAlert_WhenSourceCurrencyIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);

        // Act
        Action createAction = () => service.CreateAlert(DefaultUserId, "", TargetCurrency, SeedExchangeRate, false);

        // Assert
        createAction.Should().Throw<ArgumentException>().WithMessage("Source currency cannot be null or empty.");
    }

    [Fact]
    public void CreateAlert_WhenTargetCurrencyIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);

        // Act
        Action createAction = () => service.CreateAlert(DefaultUserId, BaseCurrency, "", SeedExchangeRate, false);

        // Assert
        createAction.Should().Throw<ArgumentException>().WithMessage("Target currency cannot be null or empty.");
    }

    [Fact]
    public void CreateAlert_WhenCurrenciesAreSame_ThrowsArgumentException()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);

        // Act
        Action createAction = () => service.CreateAlert(DefaultUserId, BaseCurrency, BaseCurrency, SeedExchangeRate, false);

        // Assert
        createAction.Should().Throw<ArgumentException>().WithMessage("Source currency cannot be the same as target currency.");
    }

    [Fact]
    public void CreateAlert_WhenRateIsZeroOrNegative_ThrowsArgumentException()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);

        // Act
        Action createAction = () => service.CreateAlert(DefaultUserId, BaseCurrency, TargetCurrency, 0m, false);

        // Assert
        createAction.Should().Throw<ArgumentException>().WithMessage("Rate cannot be zero or negative.");
    }

    [Fact]
    public void CreateAlert_WhenValid_AddsAlertToRepository()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);
        var expectedAlert = new RateAlert(DefaultUserId, BaseCurrency, TargetCurrency, SeedExchangeRate, false);
        mockExchangeRepository.Setup(r => r.AddAlert(It.IsAny<RateAlert>())).Returns(expectedAlert);

        // Act
        var result = service.CreateAlert(DefaultUserId, BaseCurrency, TargetCurrency, SeedExchangeRate, false);

        // Assert
        result.Should().Be(expectedAlert);
        mockExchangeRepository.Verify(r => r.AddAlert(It.Is<RateAlert>(a => 
            a.UserId == DefaultUserId &&
            a.BaseCurrency == BaseCurrency &&
            a.TargetCurrency == TargetCurrency &&
            a.TargetRate == SeedExchangeRate &&
            a.IsBuyAlert == false
        )), Times.Once);
    }

    [Fact]
    public void DeleteAlert_WhenCalled_DeletesAlertFromRepository()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);

        // Act
        service.DeleteAlert(ValidAlertId);

        // Assert
        mockExchangeRepository.Verify(r => r.DeleteAlert(ValidAlertId), Times.Once);
    }
}
