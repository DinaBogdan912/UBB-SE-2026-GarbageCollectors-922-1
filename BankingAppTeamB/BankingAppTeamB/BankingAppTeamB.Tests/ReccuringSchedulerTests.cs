using System;
using System.Collections.Generic;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Repositories;
using BankingAppTeamB.Services;
using Moq;
using Xunit;

namespace BankingAppTeamB.Tests.Services
{
    public class RecurringPaymentServiceTests
    {
        private readonly Mock<IRecurringPaymentRepository> mockRecurringPaymentRepository;
        private readonly Mock<IBillPaymentService> mockBillPaymentService;
        private readonly RecurringPaymentService service;

        public RecurringPaymentServiceTests()
        {
            mockRecurringPaymentRepository = new Mock<IRecurringPaymentRepository>(MockBehavior.Strict);
            mockBillPaymentService = new Mock<IBillPaymentService>(MockBehavior.Strict);
            service = new RecurringPaymentService(mockRecurringPaymentRepository.Object, mockBillPaymentService.Object);
        }

        [Fact]
        public void ComputeNextRunDate_Daily_AddsOneDay()
        {
            var from = new DateTime(2026, 1, 1);
            var result = reccuringScheduler.ComputeNextRunDate(RecurringFrequency.Daily, from);
            Assert.Equal(new DateTime(2026, 1, 2), result);
        }

        [Fact]
        public void ComputeNextRunDate_Weekly_AddsSevenDays()
        {
            var from = new DateTime(2026, 1, 1);
            var result = reccuringScheduler.ComputeNextRunDate(RecurringFrequency.Weekly, from);
            Assert.Equal(new DateTime(2026, 1, 8), result);
        }

        [Fact]
        public void ComputeNextRunDate_BiWeekly_AddsFourteenDays()
        {
            var from = new DateTime(2026, 1, 1);
            var result = reccuringScheduler.ComputeNextRunDate(RecurringFrequency.BiWeekly, from);
            Assert.Equal(new DateTime(2026, 1, 15), result);
        }

        [Fact]
        public void ComputeNextRunDate_Monthly_AddsOneMonth()
        {
            var from = new DateTime(2026, 1, 1);
            var result = reccuringScheduler.ComputeNextRunDate(RecurringFrequency.Monthly, from);
            Assert.Equal(new DateTime(2026, 2, 1), result);
        }

        [Fact]
        public void ComputeNextRunDate_Quarterly_AddsThreeMonths()
        {
            var from = new DateTime(2026, 1, 1);
            var result = reccuringScheduler.ComputeNextRunDate(RecurringFrequency.Quarterly, from);
            Assert.Equal(new DateTime(2026, 4, 1), result);
        }

        [Fact]
        public void ComputeNextRunDate_Yearly_AddsOneYear()
        {
            var from = new DateTime(2026, 1, 1);
            var result = reccuringScheduler.ComputeNextRunDate(RecurringFrequency.Yearly, from);
            Assert.Equal(new DateTime(2027, 1, 1), result);
        }

        [Fact]
        public void ComputeNextRunDate_UnknownFrequency_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                reccuringScheduler.ComputeNextRunDate((RecurringFrequency)999, new DateTime(2026, 1, 1)));
        }

        [Fact]
        public void Create_ReturnsRepositoryAddResult()
        {
            var dataTransferObject = new RecurringPaymentDto
            {
                UserId = 1,
                BillerId = 2,
                SourceAccountId = 3,
                Amount = 100m,
                IsPayInFull = false,
                Frequency = RecurringFrequency.Monthly,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31)
            };

            var added = new RecurringPayment { Id = 42 };

            mockRecurringPaymentRepository
                .Setup(repository => repository.Add(It.IsAny<RecurringPayment>()))
                .Returns(added);

            var result = reccuringScheduler.Create(dataTransferObject);

            Assert.Equal(42, result.Id);
        }

        [Fact]
        public void GetByUser_ReturnsRepositoryResult()
        {
            var expected = new List<RecurringPayment> { new RecurringPayment { Id = 1 } };
            mockRecurringPaymentRepository.Setup(repository => repository.GetByUserId(7)).Returns(expected);

            var result = reccuringScheduler.GetByUser(7);

            Assert.Single(result);
        }

        [Fact]
        public void Pause_WhenPaymentMissing_ThrowsInvalidOperationException()
        {
            mockRecurringPaymentRepository.Setup(repository => repository.GetById(9)).Returns((RecurringPayment)null);

            Assert.Throws<InvalidOperationException>(() => reccuringScheduler.Pause(9));
        }

        [Fact]
        public void Pause_WhenPaymentExists_UpdatesStatusToPaused()
        {
            var p = new RecurringPayment { Id = 9, Status = PaymentStatus.Active };
            mockRecurringPaymentRepository.Setup(repository => repository.GetById(9)).Returns(p);
            mockRecurringPaymentRepository.Setup(repository => repository.Update(It.IsAny<RecurringPayment>()));

            reccuringScheduler.Pause(9);

            Assert.Equal(PaymentStatus.Paused, p.Status);
        }

        [Fact]
        public void Resume_WhenPaymentMissing_ThrowsInvalidOperationException()
        {
            mockRecurringPaymentRepository.Setup(repository => repository.GetById(10)).Returns((RecurringPayment)null);

            Assert.Throws<InvalidOperationException>(() => reccuringScheduler.Resume(10));
        }

        [Fact]
        public void Resume_WhenPaymentExists_UpdatesStatusToActive()
        {
            var p = new RecurringPayment { Id = 10, Status = PaymentStatus.Paused };
            mockRecurringPaymentRepository.Setup(repository => repository.GetById(10)).Returns(p);
            mockRecurringPaymentRepository.Setup(repository => repository.Update(It.IsAny<RecurringPayment>()));

            reccuringScheduler.Resume(10);

            Assert.Equal(PaymentStatus.Active, p.Status);
        }

        [Fact]
        public void Cancel_WhenPaymentMissing_ThrowsInvalidOperationException()
        {
            mockRecurringPaymentRepository.Setup(repository => repository.GetById(11)).Returns((RecurringPayment)null);

            Assert.Throws<InvalidOperationException>(() => reccuringScheduler.Cancel(11));
        }

        [Fact]
        public void Cancel_WhenPaymentExists_UpdatesStatusToCancelled()
        {
            var p = new RecurringPayment { Id = 11, Status = PaymentStatus.Active };
            mockRecurringPaymentRepository.Setup(repository => repository.GetById(11)).Returns(p);
            mockRecurringPaymentRepository.Setup(repository => repository.Update(It.IsAny<RecurringPayment>()));

            reccuringScheduler.Cancel(11);

            Assert.Equal(PaymentStatus.Cancelled, p.Status);
        }

        [Fact]
        public void ProcessDuePayments_ProcessesOnlyActivePayments()
        {
            var active = new RecurringPayment
            {
                Id = 1,
                UserId = 1,
                SourceAccountId = 2,
                BillerId = 3,
                Amount = 25m,
                IsPayInFull = false,
                Frequency = RecurringFrequency.Daily,
                NextExecutionDate = DateTime.UtcNow.AddMinutes(-1),
                Status = PaymentStatus.Active
            };
            var paused = new RecurringPayment
            {
                Id = 2,
                Status = PaymentStatus.Paused,
                NextExecutionDate = DateTime.UtcNow.AddMinutes(-1)
            };

            mockRecurringPaymentRepository.Setup(repository => repository.GetDueBefore(It.IsAny<DateTime>()))
                .Returns(new List<RecurringPayment> { active, paused });
            mockBillPaymentService.Setup(billPaymentService => billPaymentService.PayBill(It.IsAny<BillPaymentDto>()))
                .Returns(new BillPayment { BillerReference = string.Empty, ReceiptNumber = string.Empty });
            mockRecurringPaymentRepository.Setup(repository => repository.Update(It.IsAny<RecurringPayment>()));

            reccuringScheduler.ProcessDuePayments();

            mockBillPaymentService.Verify(billPaymentService => billPaymentService.PayBill(It.IsAny<BillPaymentDto>()), Times.Once);
        }

        [Fact]
        public void ProcessDuePayments_OnSuccess_AdvancesNextExecutionDate()
        {
            var oldDate = DateTime.UtcNow.AddMinutes(-30);
            var payment = new RecurringPayment
            {
                Id = 1,
                UserId = 1,
                SourceAccountId = 2,
                BillerId = 3,
                Amount = 25m,
                IsPayInFull = false,
                Frequency = RecurringFrequency.Weekly,
                NextExecutionDate = oldDate,
                Status = PaymentStatus.Active
            };

            mockRecurringPaymentRepository.Setup(repository => repository.GetDueBefore(It.IsAny<DateTime>()))
                .Returns(new List<RecurringPayment> { payment });
            mockBillPaymentService.Setup(billPaymentService => billPaymentService.PayBill(It.IsAny<BillPaymentDto>()))
                .Returns(new BillPayment { BillerReference = string.Empty, ReceiptNumber = string.Empty });
            mockRecurringPaymentRepository.Setup(repository => repository.Update(It.IsAny<RecurringPayment>()));

            reccuringScheduler.ProcessDuePayments();

            Assert.Equal(oldDate.AddDays(7), payment.NextExecutionDate);
        }

        [Fact]
        public void ProcessDuePayments_OnPayBillException_SetsStatusFailed()
        {
            var payment = new RecurringPayment
            {
                Id = 1,
                UserId = 1,
                SourceAccountId = 2,
                BillerId = 3,
                Amount = 25m,
                IsPayInFull = false,
                Frequency = RecurringFrequency.Daily,
                NextExecutionDate = DateTime.UtcNow.AddMinutes(-30),
                Status = PaymentStatus.Active
            };

            mockRecurringPaymentRepository.Setup(repository => repository.GetDueBefore(It.IsAny<DateTime>()))
                .Returns(new List<RecurringPayment> { payment });
            mockBillPaymentService.Setup(billPaymentService => billPaymentService.PayBill(It.IsAny<BillPaymentDto>()))
                .Throws(new Exception("boom"));
            mockRecurringPaymentRepository.Setup(repository => repository.Update(It.IsAny<RecurringPayment>()));

            reccuringScheduler.ProcessDuePayments();

            Assert.Equal(PaymentStatus.Failed, payment.Status);
        }

        [Fact]
        public void GetDueSoon_ReturnsOnlyActivePayments()
        {
            var active = new RecurringPayment { Id = 1, Status = PaymentStatus.Active, NextExecutionDate = DateTime.UtcNow.AddHours(1) };
            var failed = new RecurringPayment { Id = 2, Status = PaymentStatus.Failed, NextExecutionDate = DateTime.UtcNow.AddHours(1) };

            mockRecurringPaymentRepository.Setup(repository => repository.GetDueBefore(It.IsAny<DateTime>()))
                .Returns(new List<RecurringPayment> { active, failed });

            var result = reccuringScheduler.GetDueSoon();
            Assert.Single(result);
        }
    }
}