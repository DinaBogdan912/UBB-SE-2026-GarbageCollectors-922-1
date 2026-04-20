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
        var actualResult = service.GetLiveRates();

        // Assert
        actualResult.Should().NotBeNull();
        actualResult.Should().ContainKey($"{BaseCurrency}/{TargetCurrency}");
        actualResult.Should().ContainKey($"{TargetCurrency}/{BaseCurrency}");
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
        var actualResult = service.GetRate(BaseCurrency, TargetCurrency);

        // Assert
        actualResult.Should().Be(SeedExchangeRate);
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
        var actualResult = service.GetRate(TargetCurrency, BaseCurrency);

        // Assert
        actualResult.Should().Be(expectedInverseRate);
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
        Action attemptGetUnsupportedRateOperation = () => service.GetRate("JPY", "CAD");

        // Assert
        attemptGetUnsupportedRateOperation.Should().Throw<Exception>().WithMessage("Rate not found for pair JPY/CAD");
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
        var actualResult = service.LockRate(DefaultUserId, BaseCurrency, TargetCurrency);

        // Assert
        actualResult.Should().NotBeNull();
        actualResult.UserId.Should().Be(DefaultUserId);
        actualResult.CurrencyPair.Should().Be($"{BaseCurrency}/{TargetCurrency}");
        actualResult.Rate.Should().Be(SeedExchangeRate);
        actualResult.LockedAt.Should().BeCloseTo(DateTime.Now, expectedToleranceForLockTime);

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
        var actualResult = service.IsRateLockValid(DefaultUserId);

        // Assert
        actualResult.Should().BeFalse();
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
        var actualResult = service.CalculateCommission(SmallExchangeAmount);

        // Assert
        actualResult.Should().Be(MinimumCommissionAmount);
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
        var actualResult = service.CalculateCommission(LargeExchangeAmount);

        // Assert
        actualResult.Should().Be(ExpectedLargeCommission);
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
        var actualResult = service.CalculateTargetAmount(LargeExchangeAmount, SeedExchangeRate);

        // Assert
        actualResult.Should().Be(expectedTargetAmount);
    }

    [Fact]
    public void ExecuteExchange_WhenLockIsInvalid_ThrowsException()
    {
        // Arrange
        var mockExchangeRepository = new Mock<IExchangeRepository>();
        var mockPipelineService = new Mock<ITransactionPipelineService>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new ExchangeService(mockExchangeRepository.Object, mockPipelineService.Object, mockAccountService.Object);
        var exchangeRequest = new ExchangeDto
        {
            UserId = DefaultUserId,
            SourceCurrency = BaseCurrency,
            TargetCurrency = TargetCurrency
        };

        // Act
        Action executeExchangeOperation = () => service.ExecuteExchange(exchangeRequest);

        // Assert
        executeExchangeOperation.Should().Throw<Exception>().WithMessage("No valid rate lock found or the 3-second window has expired.");
        mockPipelineService.Verify(mockServiceDependency => mockServiceDependency.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>()), Times.Never);
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

        var exchangeRequest = new ExchangeDto
        {
            UserId = DefaultUserId,
            SourceAccountId = DefaultSourceAccountId,
            TargetAccountId = DefaultTargetAccountId,
            SourceCurrency = BaseCurrency,
            TargetCurrency = TargetCurrency,
            SourceAmount = LargeExchangeAmount
        };

        var transaction = new Transaction { Id = 123 };
        mockPipelineService.Setup(mockServiceDependency => mockServiceDependency.RunPipeline(It.IsAny<PipelineContext>(), null)).Returns(transaction);

        var expectedToleranceForCreationTime = TimeSpan.FromSeconds(2);

        // Act
        var actualResult = service.ExecuteExchange(exchangeRequest);

        // Assert
        actualResult.Should().NotBeNull();
        actualResult.UserId.Should().Be(DefaultUserId);
        actualResult.SourceAccountId.Should().Be(DefaultSourceAccountId);
        actualResult.TargetAccountId.Should().Be(DefaultTargetAccountId);
        actualResult.TransactionId.Should().Be(transaction.Id);
        actualResult.SourceCurrency.Should().Be(BaseCurrency);
        actualResult.TargetCurrency.Should().Be(TargetCurrency);
        actualResult.SourceAmount.Should().Be(LargeExchangeAmount);
        actualResult.ExchangeRate.Should().Be(SeedExchangeRate);
        actualResult.Commission.Should().Be(ExpectedLargeCommission);
        actualResult.Status.Should().Be(TransferStatus.Completed);
        actualResult.CreatedAt.Should().BeCloseTo(DateTime.Now, expectedToleranceForCreationTime);

        mockPipelineService.Verify(mockServiceDependency => mockServiceDependency.RunPipeline(It.IsAny<PipelineContext>(), null), Times.Once);
        mockAccountService.Verify(mockServiceDependency => mockServiceDependency.CreditAccount(DefaultTargetAccountId, actualResult.TargetAmount), Times.Once);
        mockExchangeRepository.Verify(mockRepositoryInstance => mockRepositoryInstance.Add(It.IsAny<ExchangeTransaction>()), Times.Once);

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
        mockExchangeRepository.Setup(mockRepositoryInstance => mockRepositoryInstance.GetAlertsByUser(DefaultUserId, false)).Returns(expectedAlerts);

        // Act
        var actualResult = service.GetUserAlerts(DefaultUserId);

        // Assert
        actualResult.Should().BeEquivalentTo(expectedAlerts);
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
        Action createAlertOperation = () => service.CreateAlert(DefaultUserId, string.Empty, TargetCurrency, SeedExchangeRate, false);

        // Assert
        createAlertOperation.Should().Throw<ArgumentException>().WithMessage("Source currency cannot be null or empty.");
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
        Action createAlertOperation = () => service.CreateAlert(DefaultUserId, BaseCurrency, string.Empty, SeedExchangeRate, false);

        // Assert
        createAlertOperation.Should().Throw<ArgumentException>().WithMessage("Target currency cannot be null or empty.");
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
        Action createAlertOperation = () => service.CreateAlert(DefaultUserId, BaseCurrency, BaseCurrency, SeedExchangeRate, false);

        // Assert
        createAlertOperation.Should().Throw<ArgumentException>().WithMessage("Source currency cannot be the same as target currency.");
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
        Action createAlertOperation = () => service.CreateAlert(DefaultUserId, BaseCurrency, TargetCurrency, 0m, false);

        // Assert
        createAlertOperation.Should().Throw<ArgumentException>().WithMessage("Rate cannot be zero or negative.");
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
        mockExchangeRepository.Setup(mockRepositoryInstance => mockRepositoryInstance.AddAlert(It.IsAny<RateAlert>())).Returns(expectedAlert);

        // Act
        var actualResult = service.CreateAlert(DefaultUserId, BaseCurrency, TargetCurrency, SeedExchangeRate, false);

        // Assert
        actualResult.Should().Be(expectedAlert);
        mockExchangeRepository.Verify(mockRepositoryInstance => mockRepositoryInstance.AddAlert(It.Is<RateAlert>(rateAlertEntry =>
            rateAlertEntry.UserId == DefaultUserId &&
            rateAlertEntry.BaseCurrency == BaseCurrency &&
            rateAlertEntry.TargetCurrency == TargetCurrency &&
            rateAlertEntry.TargetRate == SeedExchangeRate &&
            rateAlertEntry.IsBuyAlert == false)), Times.Once);
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
        mockExchangeRepository.Verify(mockRepositoryInstance => mockRepositoryInstance.DeleteAlert(ValidAlertId), Times.Once);
    }
}
