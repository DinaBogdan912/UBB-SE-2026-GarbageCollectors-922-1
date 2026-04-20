using System;
using System.Collections.Generic;
using System.Reflection;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Repositories;
using BankingAppTeamB.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace BankingAppTeamB.Tests.Services
{
    public class ExchangeServiceTests
    {
        [Fact]
        public void GetLiveRates_FirstCall_ReturnsSeedAndInverseRates()
        {
            var (service, _, _, _) = CreateService();

            var rates = service.GetLiveRates();

            rates.Should().HaveCount(10);
        }

        [Fact]
        public void GetLiveRates_SecondCallWithinCacheDuration_ReturnsSameReference()
        {
            var (service, _, _, _) = CreateService();
            var firstRates = service.GetLiveRates();

            var secondRates = service.GetLiveRates();

            secondRates.Should().BeSameAs(firstRates);
        }

        [Fact]
        public void GetRate_DirectPairExists_ReturnsDirectRate()
        {
            var (service, _, _, _) = CreateService();

            var rate = service.GetRate("EUR", "USD");

            rate.Should().Be(1.15m);
        }

        [Fact]
        public void GetRate_OnlyInversePairExists_ReturnsInverseRate()
        {
            var (service, _, _, _) = CreateService();
            SetPrivateField(service, "cachedRates", new Dictionary<string, decimal> { { "USD/EUR", 0.87m } });
            SetPrivateField(service, "ratesLastFetched", DateTime.Now);

            var rate = service.GetRate("EUR", "USD");

            rate.Should().Be(1.15m);
        }

        [Fact]
        public void GetRate_PairMissing_ThrowsException()
        {
            var (service, _, _, _) = CreateService();
            SetPrivateField(service, "cachedRates", new Dictionary<string, decimal>());
            SetPrivateField(service, "ratesLastFetched", DateTime.Now);

            Action act = () => service.GetRate("AAA", "BBB");

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void IsRateLockValid_LockMissing_ReturnsFalse()
        {
            var (service, _, _, _) = CreateService();

            var isValid = service.IsRateLockValid(999);

            isValid.Should().BeFalse();
        }

        [Fact]
        public void IsRateLockValid_LockExpired_ReturnsFalse()
        {
            var (service, _, _, _) = CreateService();
            var lockedRates = GetPrivateField<Dictionary<int, LockedRate>>(service, "lockedRates");
            lockedRates[55] = new LockedRate
            {
                UserId = 55,
                CurrencyPair = "EUR/USD",
                Rate = 1.15m,
                LockedAt = DateTime.Now.AddSeconds(-31)
            };

            var isValid = service.IsRateLockValid(55);

            isValid.Should().BeFalse();
        }

        [Fact]
        public void CalculateCommission_AmountBelowThreshold_ReturnsMinimumCommission()
        {
            var (service, _, _, _) = CreateService();

            var commission = service.CalculateCommission(50m);

            commission.Should().Be(0.50m);
        }

        [Fact]
        public void CalculateCommission_AmountAboveThreshold_ReturnsPercentageCommission()
        {
            var (service, _, _, _) = CreateService();

            var commission = service.CalculateCommission(1000m);

            commission.Should().Be(5m);
        }

        [Fact]
        public void CalculateTargetAmount_ReturnsConvertedMinusCommission()
        {
            var (service, _, _, _) = CreateService();

            var targetAmount = service.CalculateTargetAmount(100m, 1.15m);

            targetAmount.Should().Be(114.5m);
        }

        [Fact]
        public void ExecuteExchange_NoValidLock_ThrowsException()
        {
            var (service, _, _, _) = CreateService();
            var dto = BuildExchangeDto();

            Action act = () => service.ExecuteExchange(dto);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void ExecuteExchange_WithValidLock_ReturnsCompletedStatus()
        {
            var (service, repositoryMock, pipelineServiceMock, accountServiceMock) = CreateService();
            service.LockRate(1, "EUR", "USD");

            pipelineServiceMock
                .Setup(pipelineService => pipelineService.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string?>()))
                .Returns(new Transaction { Id = 42 });

            repositoryMock
                .Setup(repository => repository.Add(It.IsAny<ExchangeTransaction>()))
                .Returns((ExchangeTransaction transaction) => transaction);

            accountServiceMock
                .Setup(accountService => accountService.CreditAccount(It.IsAny<int>(), It.IsAny<decimal>()));

            var result = service.ExecuteExchange(BuildExchangeDto());

            result.Status.Should().Be(TransferStatus.Completed);
        }

        [Fact]
        public void ExecuteExchange_WithValidLock_CallsPipelineOnce()
        {
            var (service, repositoryMock, pipelineServiceMock, accountServiceMock) = CreateService();
            service.LockRate(1, "EUR", "USD");

            pipelineServiceMock
                .Setup(pipelineService => pipelineService.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string?>()))
                .Returns(new Transaction { Id = 42 });

            repositoryMock
                .Setup(repository => repository.Add(It.IsAny<ExchangeTransaction>()))
                .Returns((ExchangeTransaction transaction) => transaction);

            accountServiceMock
                .Setup(accountService => accountService.CreditAccount(It.IsAny<int>(), It.IsAny<decimal>()));

            service.ExecuteExchange(BuildExchangeDto());

            pipelineServiceMock.Verify(pipelineService => pipelineService.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string?>()), Times.Once);
        }

        [Fact]
        public void ExecuteExchange_WithValidLock_CallsCreditAccountOnce()
        {
            var (service, repositoryMock, pipelineServiceMock, accountServiceMock) = CreateService();
            service.LockRate(1, "EUR", "USD");

            pipelineServiceMock
                .Setup(pipelineService => pipelineService.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string?>()))
                .Returns(new Transaction { Id = 42 });

            repositoryMock
                .Setup(repository => repository.Add(It.IsAny<ExchangeTransaction>()))
                .Returns((ExchangeTransaction transaction) => transaction);

            accountServiceMock
                .Setup(accountService => accountService.CreditAccount(It.IsAny<int>(), It.IsAny<decimal>()));

            service.ExecuteExchange(BuildExchangeDto());

            accountServiceMock.Verify(accountService => accountService.CreditAccount(It.IsAny<int>(), It.IsAny<decimal>()), Times.Once);
        }

        [Fact]
        public void ExecuteExchange_WithValidLock_PersistsExchangeTransactionOnce()
        {
            var (service, repositoryMock, pipelineServiceMock, accountServiceMock) = CreateService();
            service.LockRate(1, "EUR", "USD");

            pipelineServiceMock
                .Setup(pipelineService => pipelineService.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string?>()))
                .Returns(new Transaction { Id = 42 });

            repositoryMock
                .Setup(repository => repository.Add(It.IsAny<ExchangeTransaction>()))
                .Returns((ExchangeTransaction transaction) => transaction);

            accountServiceMock
                .Setup(accountService => accountService.CreditAccount(It.IsAny<int>(), It.IsAny<decimal>()));

            service.ExecuteExchange(BuildExchangeDto());

            repositoryMock.Verify(repository => repository.Add(It.IsAny<ExchangeTransaction>()), Times.Once);
        }

        [Fact]
        public void ExecuteExchange_WithValidLock_RemovesUserLockAfterExecution()
        {
            var (service, repositoryMock, pipelineServiceMock, accountServiceMock) = CreateService();
            service.LockRate(1, "EUR", "USD");

            pipelineServiceMock
                .Setup(pipelineService => pipelineService.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string?>()))
                .Returns(new Transaction { Id = 42 });

            repositoryMock
                .Setup(repository => repository.Add(It.IsAny<ExchangeTransaction>()))
                .Returns((ExchangeTransaction transaction) => transaction);

            accountServiceMock
                .Setup(accountService => accountService.CreditAccount(It.IsAny<int>(), It.IsAny<decimal>()));

            service.ExecuteExchange(BuildExchangeDto());

            service.IsRateLockValid(1).Should().BeFalse();
        }

        [Fact]
        public void ClearLocks_WhenLockExists_RemovesLock()
        {
            var (service, _, _, _) = CreateService();
            service.LockRate(2, "EUR", "USD");

            service.ClearLocks(2);

            service.IsRateLockValid(2).Should().BeFalse();
        }

        [Fact]
        public void GetUserAlerts_ReturnsRepositoryValue()
        {
            var (service, repositoryMock, _, _) = CreateService();
            var expected = new List<RateAlert> { new RateAlert() };
            repositoryMock.Setup(repository => repository.GetAlertsByUser(7, false)).Returns(expected);

            var result = service.GetUserAlerts(7);

            result.Should().BeSameAs(expected);
        }

        [Fact]
        public void CreateAlert_SourceEmpty_ThrowsArgumentException()
        {
            var (service, _, _, _) = CreateService();

            Action act = () => service.CreateAlert(1, string.Empty, "USD", 1.2m, true);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateAlert_TargetEmpty_ThrowsArgumentException()
        {
            var (service, _, _, _) = CreateService();

            Action act = () => service.CreateAlert(1, "EUR", string.Empty, 1.2m, true);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateAlert_SameCurrencies_ThrowsArgumentException()
        {
            var (service, _, _, _) = CreateService();

            Action act = () => service.CreateAlert(1, "EUR", "EUR", 1.2m, true);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateAlert_ZeroRate_ThrowsArgumentException()
        {
            var (service, _, _, _) = CreateService();

            Action act = () => service.CreateAlert(1, "EUR", "USD", 0m, true);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void CreateAlert_ValidInput_ReturnsAddedAlert()
        {
            var (service, repositoryMock, _, _) = CreateService();
            repositoryMock
                .Setup(repository => repository.AddAlert(It.IsAny<RateAlert>()))
                .Returns((RateAlert alert) =>
                {
                    alert.Id = 99;
                    return alert;
                });

            var result = service.CreateAlert(1, "EUR", "USD", 1.25m, true);

            result.Id.Should().Be(99);
        }

        [Fact]
        public void DeleteAlert_CallsRepositoryDeleteOnce()
        {
            var (service, repositoryMock, _, _) = CreateService();
            repositoryMock.Setup(repository => repository.DeleteAlert(123));

            service.DeleteAlert(123);

            repositoryMock.Verify(repository => repository.DeleteAlert(123), Times.Once);
        }

        [Fact]
        public void CheckRateAlerts_BuyAlert_SetsTriggeredWhenCurrentRateIsLowerOrEqual()
        {
            var (service, repositoryMock, _, _) = CreateService();
            var alert = new RateAlert(1, "EUR", "USD", 1.20m, true);
            repositoryMock.Setup(repository => repository.GetAllAlerts(false)).Returns(new List<RateAlert> { alert });

            service.CheckRateAlerts();

            alert.IsTriggered.Should().BeTrue();
        }

        [Fact]
        public void CheckRateAlerts_SellAlert_SetsTriggeredWhenCurrentRateIsHigherOrEqual()
        {
            var (service, repositoryMock, _, _) = CreateService();
            var alert = new RateAlert(1, "EUR", "USD", 1.10m, false);
            repositoryMock.Setup(repository => repository.GetAllAlerts(false)).Returns(new List<RateAlert> { alert });

            service.CheckRateAlerts();

            alert.IsTriggered.Should().BeTrue();
        }

        private static ExchangeDto BuildExchangeDto()
        {
            return new ExchangeDto
            {
                UserId = 1,
                SourceAccountId = 100,
                TargetAccountId = 200,
                SourceCurrency = "EUR",
                TargetCurrency = "USD",
                SourceAmount = 100m
            };
        }

        private static (ExchangeService Service, Mock<IExchangeRepository> Repository, Mock<ITransactionPipelineService> PipelineService, Mock<IAccountService> AccountService) CreateService()
        {
            var repositoryMock = new Mock<IExchangeRepository>(MockBehavior.Strict);
            var pipelineServiceMock = new Mock<ITransactionPipelineService>(MockBehavior.Strict);
            var accountServiceMock = new Mock<IAccountService>(MockBehavior.Strict);

            var service = new ExchangeService(repositoryMock.Object, pipelineServiceMock.Object, accountServiceMock.Object);
            return (service, repositoryMock, pipelineServiceMock, accountServiceMock);
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field!.SetValue(instance, value);
        }

        private static T GetPrivateField<T>(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return (T)field!.GetValue(instance)!;
        }
    }
}
