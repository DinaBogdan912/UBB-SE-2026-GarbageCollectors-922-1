using BankingAppTeamB.Services;
using FluentAssertions;
using Moq;
using System.Reflection;
using Xunit;

namespace BankingAppTeamB.Tests.Services
{
    public class RecurringSchedulerTests
    {
        private readonly Mock<IRecurringPaymentService> _recurringPaymentServiceMock = new();
        private readonly Mock<IExchangeService> _exchangeServiceMock = new();
        private readonly System.Timers.Timer _timer = new();

        private RecurringScheduler CreateSut() => new(_recurringPaymentServiceMock.Object, _exchangeServiceMock.Object, _timer);

        [Fact]
        public void Start_EnablesTimer()
        {
            var sut = CreateSut();
            sut.Start();
            _timer.Enabled.Should().BeTrue();
            // cleanup
            sut.Stop();
        }

        [Fact]
        public void Stop_DisablesTimer()
        {
            var sut = CreateSut();
            sut.Start();
            sut.Stop();
            _timer.Enabled.Should().BeFalse();
        }

        [Fact]
        public void OnTick_CallsProcessDuePaymentsAndCheckRateAlerts()
        {
            var sut = CreateSut();
            
            var methodInfo = typeof(RecurringScheduler).GetMethod("OnTick", BindingFlags.NonPublic | BindingFlags.Instance);
            if(methodInfo != null)
            {
                methodInfo.Invoke(sut, new object?[] { null, null });
            }

            _recurringPaymentServiceMock.Verify(x => x.ProcessDuePayments(), Times.Once);
            _exchangeServiceMock.Verify(x => x.CheckRateAlerts(), Times.Once);
        }
        
        [Fact]
        public void OnTick_DoesNotThrow_WhenProcessDuePaymentsThrows()
        {
            var sut = CreateSut();
            _recurringPaymentServiceMock.Setup(x => x.ProcessDuePayments()).Throws(new Exception("Test Exception"));
            
            var methodInfo = typeof(RecurringScheduler).GetMethod("OnTick", BindingFlags.NonPublic | BindingFlags.Instance);
            Action act = () =>
            {
                if (methodInfo != null)
                {
                    try
                    {
                        methodInfo.Invoke(sut, new object?[] { null, null });
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw ex.InnerException ?? ex;
                    }
                }
            };
            
            act.Should().NotThrow();
            _exchangeServiceMock.Verify(x => x.CheckRateAlerts(), Times.Once);
        }

        [Fact]
        public void OnTick_DoesNotThrow_WhenCheckRateAlertsThrows()
        {
            var sut = CreateSut();
            _exchangeServiceMock.Setup(x => x.CheckRateAlerts()).Throws(new Exception("Test Exception"));
            
            var methodInfo = typeof(RecurringScheduler).GetMethod("OnTick", BindingFlags.NonPublic | BindingFlags.Instance);
            Action act = () =>
            {
                if (methodInfo != null)
                {
                    try
                    {
                        methodInfo.Invoke(sut, new object?[] { null, null });
                    }
                    catch (TargetInvocationException ex)
                    {
                        throw ex.InnerException ?? ex;
                    }
                }
            };
            
            act.Should().NotThrow();
            _recurringPaymentServiceMock.Verify(x => x.ProcessDuePayments(), Times.Once);
        }
    }
}
