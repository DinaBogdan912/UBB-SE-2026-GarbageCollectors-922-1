using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Repositories;
using BankingAppTeamB.Services;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace BankingAppTeamB.Tests.Services
{
    public class BillPaymentServiceTests
    {
        private readonly Mock<IBillPaymentRepository> _repo = new();
        private readonly Mock<ITransactionPipelineService> _pipeline = new();

        private BillPaymentService CreateSut() => new(_repo.Object, _pipeline.Object);

        [Theory]
        [InlineData(0f, 0.50f)]
        [InlineData(100f, 0.50f)]
        [InlineData(100.01f, 1.00f)]
        public void CalculateFee_ReturnsExpected(float amount, float expected)
        {
            var sut = CreateSut();
            sut.CalculateFee((decimal)amount).Should().Be((decimal)expected);
        }

        [Fact]
        public void GetBillerDirectory_WithNullCategory_CallsGetAllBillers()
        {
            _repo.Setup(r => r.GetAllBillers()).Returns(new List<Biller>());
            var sut = CreateSut();

            sut.GetBillerDirectory(null);

            _repo.Verify(r => r.GetAllBillers(), Times.Once);
        }

        [Fact]
        public void GetBillerDirectory_WithCategory_CallsSearchBillers()
        {
            _repo.Setup(r => r.SearchBillers(string.Empty, "Utilities")).Returns(new List<Biller>());
            var sut = CreateSut();

            sut.GetBillerDirectory("Utilities");

            _repo.Verify(r => r.SearchBillers(string.Empty, "Utilities"), Times.Once);
        }

        [Fact]
        public void SearchBillers_WithoutCategory_CallsRepoWithNullCategory()
        {
            _repo.Setup(r => r.SearchBillers("gas", null)).Returns(new List<Biller>());
            var sut = CreateSut();

            sut.SearchBillers("gas");

            _repo.Verify(r => r.SearchBillers("gas", null), Times.Once);
        }

        [Fact]
        public void SearchBillers_WithNullQuery_UsesEmptyString()
        {
            _repo.Setup(r => r.SearchBillers(string.Empty, "Utilities")).Returns(new List<Biller>());
            var sut = CreateSut();

            sut.SearchBillers(null!, "Utilities");

            _repo.Verify(r => r.SearchBillers(string.Empty, "Utilities"), Times.Once);
        }

        [Fact]
        public void SearchBillers_WithCategory_CallsRepo()
        {
            _repo.Setup(r => r.SearchBillers("water", "Utilities")).Returns(new List<Biller>());
            var sut = CreateSut();

            sut.SearchBillers("water", "Utilities");

            _repo.Verify(r => r.SearchBillers("water", "Utilities"), Times.Once);
        }

        [Fact]
        public void GetSavedBillers_CallsRepo()
        {
            _repo.Setup(r => r.GetSavedBillers(7)).Returns(new List<SavedBiller>());
            var sut = CreateSut();

            sut.GetSavedBillers(7);

            _repo.Verify(r => r.GetSavedBillers(7), Times.Once);
        }

        [Fact]
        public void SaveBiller_CallsRepoSave()
        {
            var sut = CreateSut();

            sut.SaveBiller(1, 2, "My biller", "Ref");

            _repo.Verify(r => r.SaveBiller(It.IsAny<SavedBiller>()), Times.Once);
        }

        [Fact]
        public void RemoveSavedBiller_CallsRepoDelete()
        {
            var sut = CreateSut();

            sut.RemoveSavedBiller(10);

            _repo.Verify(r => r.DeleteSavedBiller(10), Times.Once);
        }

        [Theory]
        [InlineData(999.99f, false)]
        [InlineData(1000.00f, true)]
        [InlineData(2500.00f, true)]
        public void Requires2FA_ReturnsExpected(float amount, bool expected)
        {
            var sut = CreateSut();
            sut.Requires2FA((decimal)amount).Should().Be(expected);
        }

        [Fact]
        public void PayBill_Throws_WhenBillerNotFound()
        {
            _repo.Setup(r => r.GetBillerById(99)).Returns((Biller?)null);
            var sut = CreateSut();
            var dto = new BillPaymentDto { UserId = 1, SourceAccountId = 10, BillerId = 99, Amount = 50, BillerReference = "A", TwoFAToken = "123456" };

            Action act = () => sut.PayBill(dto);

            act.Should().Throw<InvalidOperationException>().WithMessage("Biller with ID 99 does not exist.");
        }

        [Fact]
        public void PayBill_CallsPipeline_WithExpectedContext()
        {
            _repo.Setup(r => r.GetBillerById(2)).Returns(new Biller { Id = 2, Name = "Electricity Co" });
            _pipeline.Setup(p => p.RunPipeline(It.IsAny<PipelineContext>(), "123456")).Returns(new Transaction { Id = 77 });
            _repo.Setup(r => r.Add(It.IsAny<BillPayment>())).Returns(new BillPayment());
            var sut = CreateSut();
            var dto = new BillPaymentDto { UserId = 1, SourceAccountId = 10, BillerId = 2, Amount = 100, BillerReference = "INV-1", TwoFAToken = "123456" };

            sut.PayBill(dto);

            _pipeline.Verify(p => p.RunPipeline(
                It.Is<PipelineContext>(c =>
                    c.UserId == 1 &&
                    c.SourceAccountId == 10 &&
                    c.Amount == 100 &&
                    c.Currency == "RON" &&
                    c.Type == "BillPayment" &&
                    c.Fee == 0.50m &&
                    c.CounterpartyName == "Electricity Co" &&
                    c.RelatedEntityType == "BillPayment" &&
                    c.RelatedEntityId == 0),
                "123456"), Times.Once);
        }

        [Fact]
        public void PayBill_CallsRepositoryAdd()
        {
            _repo.Setup(r => r.GetBillerById(2)).Returns(new Biller { Id = 2, Name = "Electricity Co" });
            _pipeline.Setup(p => p.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>())).Returns(new Transaction { Id = 77 });
            _repo.Setup(r => r.Add(It.IsAny<BillPayment>())).Returns(new BillPayment());
            var sut = CreateSut();
            var dto = new BillPaymentDto { UserId = 1, SourceAccountId = 10, BillerId = 2, Amount = 150, BillerReference = "INV-2", TwoFAToken = "123456" };

            sut.PayBill(dto);

            _repo.Verify(r => r.Add(It.IsAny<BillPayment>()), Times.Once);
        }

        [Fact]
        public void PayBill_ReturnsRepositoryResult()
        {
            _repo.Setup(r => r.GetBillerById(2)).Returns(new Biller { Id = 2, Name = "Electricity Co" });
            _pipeline.Setup(p => p.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>())).Returns(new Transaction { Id = 77 });
            var expected = new BillPayment { Id = 5, ReceiptNumber = "RCP-TEST" };
            _repo.Setup(r => r.Add(It.IsAny<BillPayment>())).Returns(expected);
            var sut = CreateSut();
            var dto = new BillPaymentDto { UserId = 1, SourceAccountId = 10, BillerId = 2, Amount = 150, BillerReference = "INV-2", TwoFAToken = "123456" };

            var result = sut.PayBill(dto);

            result.Should().BeSameAs(expected);
        }

        [Fact]
        public void PayBill_SetsCompletedStatus_OnAddedEntity()
        {
            _repo.Setup(r => r.GetBillerById(2)).Returns(new Biller { Id = 2, Name = "Electricity Co" });
            _pipeline.Setup(p => p.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>())).Returns(new Transaction { Id = 77 });
            _repo.Setup(r => r.Add(It.IsAny<BillPayment>())).Returns<BillPayment>(x => x);
            var sut = CreateSut();
            var dto = new BillPaymentDto { UserId = 1, SourceAccountId = 10, BillerId = 2, Amount = 150, BillerReference = "INV-2", TwoFAToken = "123456" };

            var result = sut.PayBill(dto);

            result.Status.Should().Be(PaymentStatus.Completed);
        }
    }
}