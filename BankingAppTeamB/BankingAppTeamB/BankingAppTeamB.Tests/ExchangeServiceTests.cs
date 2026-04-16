using System;
using System.Collections.Generic;
using System.Reflection;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Repositories;
using BankingAppTeamB.Services;
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

            Assert.Equal(10, rates.Count);    }

        [Fact]
        public void GetLiveRates_SecondCallWithinCacheDuration_ReturnsSameReference()
        {
            var (service, _, _, _) = CreateService();
            var first = service.GetLiveRates();

            var second = service.GetLiveRates();

            Assert.Same(first, second);
        }

        [Fact]
        public void GetRate_DirectPairExists_ReturnsDirectRate()
        {
            var (service, _, _, _) = CreateService();

            var rate = service.GetRate("EUR", "USD");

            Assert.Equal(1.15m, rate);
        }

        [Fact]
        public void GetRate_OnlyInversePairExists_ReturnsInverseRate()
        {
            var (service, _, _, _) = CreateService();
            SetPrivateField(service, "cachedRates", new Dictionary<string, decimal> { { "USD/EUR", 0.87m } });
            SetPrivateField(service, "ratesLastFetched", DateTime.Now);

            var rate = service.GetRate("EUR", "USD");

            Assert.Equal(1.15m, rate);
        }

        [Fact]
        public void GetRate_PairMissing_ThrowsException()
        {
            var (service, _, _, _) = CreateService();
            SetPrivateField(service, "cachedRates", new Dictionary<string, decimal>());
            SetPrivateField(service, "ratesLastFetched", DateTime.Now);

            Assert.Throws<Exception>(() => service.GetRate("AAA", "BBB"));
        }

        [Fact]
        public void LockRate_ReturnsLockWithCorrectUserId()
        {
            var (service, _, _, _) = CreateService();

            var locked = service.LockRate(7, "EUR", "USD");

            Assert.Equal(7, locked.UserId);
        }

        [Fact]
        public void IsRateLockValid_LockMissing_ReturnsFalse()
        {
            var (service, _, _, _) = CreateService();

            var valid = service.IsRateLockValid(999);

            Assert.False(valid);
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

            var valid = service.IsRateLockValid(55);

            Assert.False(valid);
        }

        [Fact]
        public void CalculateCommission_AmountBelowThreshold_ReturnsMinimumCommission()
        {
            var (service, _, _, _) = CreateService();

            var commission = service.CalculateCommission(50m);

            Assert.Equal(0.50m, commission);
        }

        [Fact]
        public void CalculateCommission_AmountAboveThreshold_ReturnsPercentageCommission()
        {
            var (service, _, _, _) = CreateService();

            var commission = service.CalculateCommission(1000m);

            Assert.Equal(5m, commission);
        }

        [Fact]
        public void CalculateTargetAmount_ReturnsConvertedMinusCommission()
        {
            var (service, _, _, _) = CreateService();

            var target = service.CalculateTargetAmount(100m, 1.15m);

            Assert.Equal(114.5m, target);
        }

        [Fact]
        public void ExecuteExchange_NoValidLock_ThrowsException()
        {
            var (service, _, _, _) = CreateService();
            var dto = BuildDto();

            Assert.Throws<Exception>(() => service.ExecuteExchange(dto));
        }

        [Fact]
        public void ExecuteExchange_WithValidLock_ReturnsCompletedStatus()
        {
            var (service, repoMock, pipelineMock, accountMock) = CreateService();
            service.LockRate(1, "EUR", "USD");

            pipelineMock
                .Setup(x => x.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string?>()))
                .Returns(new Transaction { Id = 42 });

            repoMock
                .Setup(x => x.Add(It.IsAny<ExchangeTransaction>()))
                .Returns((ExchangeTransaction tx) => tx);

            accountMock
                .Setup(x => x.CreditAccount(It.IsAny<int>(), It.IsAny<decimal>()));

            var dto = BuildDto();
            var result = service.ExecuteExchange(dto);

            Assert.Equal(TransferStatus.Completed, result.Status);
        }

        [Fact]
        public void ExecuteExchange_WithValidLock_CallsPipelineOnce()
        {
            var (service, repoMock, pipelineMock, accountMock) = CreateService();
            service.LockRate(1, "EUR", "USD");

            pipelineMock
                .Setup(x => x.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string?>()))
                .Returns(new Transaction { Id = 42 });

            repoMock
                .Setup(x => x.Add(It.IsAny<ExchangeTransaction>()))
                .Returns((ExchangeTransaction tx) => tx);

            accountMock
                .Setup(x => x.CreditAccount(It.IsAny<int>(), It.IsAny<decimal>()));

            service.ExecuteExchange(BuildDto());

            pipelineMock.Verify(x => x.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string?>()), Times.Once);
        }

        [Fact]
        public void ExecuteExchange_WithValidLock_CallsCreditAccountOnce()
        {
            var (service, repoMock, pipelineMock, accountMock) = CreateService();
            service.LockRate(1, "EUR", "USD");

            pipelineMock
                .Setup(x => x.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string?>()))
                .Returns(new Transaction { Id = 42 });

            repoMock
                .Setup(x => x.Add(It.IsAny<ExchangeTransaction>()))
                .Returns((ExchangeTransaction tx) => tx);

            accountMock
                .Setup(x => x.CreditAccount(It.IsAny<int>(), It.IsAny<decimal>()));

            service.ExecuteExchange(BuildDto());

            accountMock.Verify(x => x.CreditAccount(It.IsAny<int>(), It.IsAny<decimal>()), Times.Once);
        }

        [Fact]
        public void ExecuteExchange_WithValidLock_PersistsExchangeTransactionOnce()
        {
            var (service, repoMock, pipelineMock, accountMock) = CreateService();
            service.LockRate(1, "EUR", "USD");

            pipelineMock
                .Setup(x => x.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string?>()))
                .Returns(new Transaction { Id = 42 });

            repoMock
                .Setup(x => x.Add(It.IsAny<ExchangeTransaction>()))
                .Returns((ExchangeTransaction tx) => tx);

            accountMock
                .Setup(x => x.CreditAccount(It.IsAny<int>(), It.IsAny<decimal>()));

            service.ExecuteExchange(BuildDto());

            repoMock.Verify(x => x.Add(It.IsAny<ExchangeTransaction>()), Times.Once);
        }

        [Fact]
        public void ExecuteExchange_WithValidLock_RemovesUserLockAfterExecution()
        {
            var (service, repoMock, pipelineMock, accountMock) = CreateService();
            service.LockRate(1, "EUR", "USD");

            pipelineMock
                .Setup(x => x.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string?>()))
                .Returns(new Transaction { Id = 42 });

            repoMock
                .Setup(x => x.Add(It.IsAny<ExchangeTransaction>()))
                .Returns((ExchangeTransaction tx) => tx);

            accountMock
                .Setup(x => x.CreditAccount(It.IsAny<int>(), It.IsAny<decimal>()));

            service.ExecuteExchange(BuildDto());
            var validAfter = service.IsRateLockValid(1);

            Assert.False(validAfter);
        }

        [Fact]
        public void ClearLocks_WhenLockExists_RemovesLock()
        {
            var (service, _, _, _) = CreateService();
            service.LockRate(2, "EUR", "USD");

            service.ClearLocks(2);

            Assert.False(service.IsRateLockValid(2));
        }

        [Fact]
        public void GetUserAlerts_ReturnsRepositoryValue()
        {
            var (service, repoMock, _, _) = CreateService();
            var expected = new List<RateAlert> { new RateAlert() };
            repoMock.Setup(x => x.GetAlertsByUser(7, false)).Returns(expected);

            var result = service.GetUserAlerts(7);

            Assert.Same(expected, result);
        }

        [Fact]
        public void CreateAlert_SourceEmpty_ThrowsArgumentException()
        {
            var (service, _, _, _) = CreateService();

            Assert.Throws<ArgumentException>(() => service.CreateAlert(1, string.Empty, "USD", 1.2m, true));
        }

        [Fact]
        public void CreateAlert_TargetEmpty_ThrowsArgumentException()
        {
            var (service, _, _, _) = CreateService();

            Assert.Throws<ArgumentException>(() => service.CreateAlert(1, "EUR", string.Empty, 1.2m, true));
        }

        [Fact]
        public void CreateAlert_SameCurrencies_ThrowsArgumentException()
        {
            var (service, _, _, _) = CreateService();

            Assert.Throws<ArgumentException>(() => service.CreateAlert(1, "EUR", "EUR", 1.2m, true));
        }

        [Fact]
        public void CreateAlert_ZeroRate_ThrowsArgumentException()
        {
            var (service, _, _, _) = CreateService();

            Assert.Throws<ArgumentException>(() => service.CreateAlert(1, "EUR", "USD", 0m, true));
        }

        [Fact]
        public void CreateAlert_ValidInput_ReturnsAddedAlert()
        {
            var (service, repoMock, _, _) = CreateService();
            repoMock
                .Setup(x => x.AddAlert(It.IsAny<RateAlert>()))
                .Returns((RateAlert a) =>
                {
                    a.Id = 99;
                    return a;
                });

            var result = service.CreateAlert(1, "EUR", "USD", 1.25m, true);

            Assert.Equal(99, result.Id);
        }

        [Fact]
        public void DeleteAlert_CallsRepositoryDeleteOnce()
        {
            var (service, repoMock, _, _) = CreateService();
            repoMock.Setup(x => x.DeleteAlert(123));

            service.DeleteAlert(123);

            repoMock.Verify(x => x.DeleteAlert(123), Times.Once);
        }

        [Fact]
        public void CheckRateAlerts_BuyAlert_SetsTriggeredWhenCurrentRateIsLowerOrEqual()
        {
            var (service, repoMock, _, _) = CreateService();
            var alert = new RateAlert(1, "EUR", "USD", 1.20m, true);
            repoMock.Setup(x => x.GetAllAlerts(false)).Returns(new List<RateAlert> { alert });

            service.CheckRateAlerts();

            Assert.True(alert.IsTriggered);
        }

        [Fact]
        public void CheckRateAlerts_SellAlert_SetsTriggeredWhenCurrentRateIsHigherOrEqual()
        {
            var (service, repoMock, _, _) = CreateService();
            var alert = new RateAlert(1, "EUR", "USD", 1.10m, false);
            repoMock.Setup(x => x.GetAllAlerts(false)).Returns(new List<RateAlert> { alert });

            service.CheckRateAlerts();

            Assert.True(alert.IsTriggered);
        }

        private static ExchangeDto BuildDto()
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

        private static (ExchangeService Service, Mock<IExchangeRepository> Repo, Mock<ITransactionPipelineService> Pipeline, Mock<IAccountService> Account) CreateService()
        {
            var repoMock = new Mock<IExchangeRepository>(MockBehavior.Strict);
            var pipelineMock = new Mock<ITransactionPipelineService>(MockBehavior.Strict);
            var accountMock = new Mock<IAccountService>(MockBehavior.Strict);

            var service = new ExchangeService(repoMock.Object, pipelineMock.Object, accountMock.Object);
            return (service, repoMock, pipelineMock, accountMock);
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