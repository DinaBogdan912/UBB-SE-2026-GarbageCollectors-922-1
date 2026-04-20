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
        private readonly Mock<IRecurringPaymentRepository> recurringRepoMock;
        private readonly Mock<IBillPaymentService> billPaymentServiceMock;
        private readonly RecurringPaymentService service;

        public RecurringPaymentServiceTests()
        {
            recurringRepoMock = new Mock<IRecurringPaymentRepository>(MockBehavior.Strict);
            billPaymentServiceMock = new Mock<IBillPaymentService>(MockBehavior.Strict);
            service = new RecurringPaymentService(recurringRepoMock.Object, billPaymentServiceMock.Object);
        }

        [Fact]
        public void ComputeNextRunDate_Daily_AddsOneDay()
        {
            var from = new DateTime(2026, 1, 1);
            var actualResult = service.ComputeNextRunDate(RecurringFrequency.Daily, from);
            Assert.Equal(new DateTime(2026, 1, 2), actualResult);
        }

        [Fact]
        public void ComputeNextRunDate_Weekly_AddsSevenDays()
        {
            var from = new DateTime(2026, 1, 1);
            var actualResult = service.ComputeNextRunDate(RecurringFrequency.Weekly, from);
            Assert.Equal(new DateTime(2026, 1, 8), actualResult);
        }

        [Fact]
        public void ComputeNextRunDate_BiWeekly_AddsFourteenDays()
        {
            var from = new DateTime(2026, 1, 1);
            var actualResult = service.ComputeNextRunDate(RecurringFrequency.BiWeekly, from);
            Assert.Equal(new DateTime(2026, 1, 15), actualResult);
        }

        [Fact]
        public void ComputeNextRunDate_Monthly_AddsOneMonth()
        {
            var from = new DateTime(2026, 1, 1);
            var actualResult = service.ComputeNextRunDate(RecurringFrequency.Monthly, from);
            Assert.Equal(new DateTime(2026, 2, 1), actualResult);
        }

        [Fact]
        public void ComputeNextRunDate_Quarterly_AddsThreeMonths()
        {
            var from = new DateTime(2026, 1, 1);
            var actualResult = service.ComputeNextRunDate(RecurringFrequency.Quarterly, from);
            Assert.Equal(new DateTime(2026, 4, 1), actualResult);
        }

        [Fact]
        public void ComputeNextRunDate_Yearly_AddsOneYear()
        {
            var from = new DateTime(2026, 1, 1);
            var actualResult = service.ComputeNextRunDate(RecurringFrequency.Yearly, from);
            Assert.Equal(new DateTime(2027, 1, 1), actualResult);
        }

        [Fact]
        public void ComputeNextRunDate_UnknownFrequency_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                service.ComputeNextRunDate((RecurringFrequency)999, new DateTime(2026, 1, 1)));
        }

        [Fact]
        public void Create_ReturnsRepositoryAddResult()
        {
            var recurringPaymentRequest = new RecurringPaymentDto
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

            recurringRepoMock
                .Setup(mockRepositoryInstance => mockRepositoryInstance.Add(It.IsAny<RecurringPayment>()))
                .Returns(added);

            var actualResult = service.Create(recurringPaymentRequest);

            Assert.Equal(42, actualResult.Id);
        }

        [Fact]
        public void GetByUser_ReturnsRepositoryResult()
        {
            var expected = new List<RecurringPayment> { new RecurringPayment { Id = 1 } };
            recurringRepoMock.Setup(mockRepositoryInstance => mockRepositoryInstance.GetByUserId(7)).Returns(expected);

            var actualResult = service.GetByUser(7);

            Assert.Single(actualResult);
        }

        [Fact]
        public void Pause_WhenPaymentMissing_ThrowsInvalidOperationException()
        {
            recurringRepoMock.Setup(mockRepositoryInstance => mockRepositoryInstance.GetById(9)).Returns((RecurringPayment)null);

            Assert.Throws<InvalidOperationException>(() => service.Pause(9));
        }

        [Fact]
        public void Pause_WhenPaymentExists_UpdatesStatusToPaused()
        {
            var recurringPaymentUnderTest = new RecurringPayment { Id = 9, Status = PaymentStatus.Active };
            recurringRepoMock.Setup(mockRepositoryInstance => mockRepositoryInstance.GetById(9)).Returns(recurringPaymentUnderTest);
            recurringRepoMock.Setup(mockRepositoryInstance => mockRepositoryInstance.Update(It.IsAny<RecurringPayment>()));

            service.Pause(9);

            Assert.Equal(PaymentStatus.Paused, recurringPaymentUnderTest.Status);
        }

        [Fact]
        public void Resume_WhenPaymentMissing_ThrowsInvalidOperationException()
        {
            recurringRepoMock.Setup(mockRepositoryInstance => mockRepositoryInstance.GetById(10)).Returns((RecurringPayment)null);

            Assert.Throws<InvalidOperationException>(() => service.Resume(10));
        }

        [Fact]
        public void Resume_WhenPaymentExists_UpdatesStatusToActive()
        {
            var recurringPaymentUnderTest = new RecurringPayment { Id = 10, Status = PaymentStatus.Paused };
            recurringRepoMock.Setup(mockRepositoryInstance => mockRepositoryInstance.GetById(10)).Returns(recurringPaymentUnderTest);
            recurringRepoMock.Setup(mockRepositoryInstance => mockRepositoryInstance.Update(It.IsAny<RecurringPayment>()));

            service.Resume(10);

            Assert.Equal(PaymentStatus.Active, recurringPaymentUnderTest.Status);
        }

        [Fact]
        public void Cancel_WhenPaymentMissing_ThrowsInvalidOperationException()
        {
            recurringRepoMock.Setup(mockRepositoryInstance => mockRepositoryInstance.GetById(11)).Returns((RecurringPayment)null);

            Assert.Throws<InvalidOperationException>(() => service.Cancel(11));
        }

        [Fact]
        public void Cancel_WhenPaymentExists_UpdatesStatusToCancelled()
        {
            var recurringPaymentUnderTest = new RecurringPayment { Id = 11, Status = PaymentStatus.Active };
            recurringRepoMock.Setup(mockRepositoryInstance => mockRepositoryInstance.GetById(11)).Returns(recurringPaymentUnderTest);
            recurringRepoMock.Setup(mockRepositoryInstance => mockRepositoryInstance.Update(It.IsAny<RecurringPayment>()));

            service.Cancel(11);

            Assert.Equal(PaymentStatus.Cancelled, recurringPaymentUnderTest.Status);
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

            recurringRepoMock.Setup(mockRepositoryInstance => mockRepositoryInstance.GetDueBefore(It.IsAny<DateTime>()))
                .Returns(new List<RecurringPayment> { active, paused });
            billPaymentServiceMock.Setup(b => b.PayBill(It.IsAny<BillPaymentDto>()))
                .Returns(new BillPayment { BillerReference = string.Empty, ReceiptNumber = string.Empty });
            recurringRepoMock.Setup(mockRepositoryInstance => mockRepositoryInstance.Update(It.IsAny<RecurringPayment>()));

            service.ProcessDuePayments();

            billPaymentServiceMock.Verify(b => b.PayBill(It.IsAny<BillPaymentDto>()), Times.Once);
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

            recurringRepoMock.Setup(mockRepositoryInstance => mockRepositoryInstance.GetDueBefore(It.IsAny<DateTime>()))
                .Returns(new List<RecurringPayment> { payment });
            billPaymentServiceMock.Setup(b => b.PayBill(It.IsAny<BillPaymentDto>()))
                .Returns(new BillPayment { BillerReference = string.Empty, ReceiptNumber = string.Empty });
            recurringRepoMock.Setup(mockRepositoryInstance => mockRepositoryInstance.Update(It.IsAny<RecurringPayment>()));

            service.ProcessDuePayments();

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

            recurringRepoMock.Setup(mockRepositoryInstance => mockRepositoryInstance.GetDueBefore(It.IsAny<DateTime>()))
                .Returns(new List<RecurringPayment> { payment });
            billPaymentServiceMock.Setup(b => b.PayBill(It.IsAny<BillPaymentDto>()))
                .Throws(new Exception("boom"));
            recurringRepoMock.Setup(mockRepositoryInstance => mockRepositoryInstance.Update(It.IsAny<RecurringPayment>()));

            service.ProcessDuePayments();

            Assert.Equal(PaymentStatus.Failed, payment.Status);
        }

        [Fact]
        public void GetDueSoon_ReturnsOnlyActivePayments()
        {
            var active = new RecurringPayment { Id = 1, Status = PaymentStatus.Active, NextExecutionDate = DateTime.UtcNow.AddHours(1) };
            var failed = new RecurringPayment { Id = 2, Status = PaymentStatus.Failed, NextExecutionDate = DateTime.UtcNow.AddHours(1) };

            recurringRepoMock.Setup(mockRepositoryInstance => mockRepositoryInstance.GetDueBefore(It.IsAny<DateTime>()))
                .Returns(new List<RecurringPayment> { active, failed });

            var actualResult = service.GetDueSoon();
            Assert.Single(actualResult);
        }
    }
}