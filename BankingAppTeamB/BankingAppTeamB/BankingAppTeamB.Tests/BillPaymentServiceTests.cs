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
        private readonly Mock<IBillPaymentRepository> _billPaymentRepository = new();
        private readonly Mock<ITransactionPipelineService> _pipelineService = new();

        private BillPaymentService CreateSut() => new(_billPaymentRepository.Object, _pipelineService.Object);

        [Theory]
        [InlineData(0, 0.50)]
        [InlineData(100, 0.50)]
        [InlineData(100.01, 1.00)]
        public void CalculateFee_ReturnsExpected(decimal amount, decimal expected)
        {
            var sut = CreateSut();

            sut.CalculateFee(amount).Should().Be(expected);
        }

        [Fact]
        public void GetBillerDirectory_WithNullCategory_CallsGetAllBillers()
        {
            _billPaymentRepository.Setup(repository => repository.GetAllBillers(true)).Returns(new List<Biller>());
            var sut = CreateSut();

            sut.GetBillerDirectory(null);

            _billPaymentRepository.Verify(repository => repository.GetAllBillers(true), Times.Once);
        }

        [Fact]
        public void GetBillerDirectory_WithCategory_CallsSearchBillers()
        {
            _billPaymentRepository.Setup(repository => repository.SearchBillers(string.Empty, "Utilities", true)).Returns(new List<Biller>());
            var sut = CreateSut();

            sut.GetBillerDirectory("Utilities");

            _billPaymentRepository.Verify(repository => repository.SearchBillers(string.Empty, "Utilities", true), Times.Once);
        }

        [Fact]
        public void SearchBillers_WithoutCategory_CallsRepoWithNullCategory()
        {
            _billPaymentRepository.Setup(repository => repository.SearchBillers("gas", null, true)).Returns(new List<Biller>());
            var sut = CreateSut();

            sut.SearchBillers("gas");

            _billPaymentRepository.Verify(repository => repository.SearchBillers("gas", null, true), Times.Once);
        }

        [Fact]
        public void SearchBillers_WithNullQuery_UsesEmptyString()
        {
            _billPaymentRepository.Setup(repository => repository.SearchBillers(string.Empty, "Utilities", true)).Returns(new List<Biller>());
            var sut = CreateSut();

            sut.SearchBillers(null!, "Utilities");

            _billPaymentRepository.Verify(repository => repository.SearchBillers(string.Empty, "Utilities", true), Times.Once);
        }

        [Fact]
        public void SearchBillers_WithCategory_CallsRepo()
        {
            _billPaymentRepository.Setup(repository => repository.SearchBillers("water", "Utilities", true)).Returns(new List<Biller>());
            var sut = CreateSut();

            sut.SearchBillers("water", "Utilities");

            _billPaymentRepository.Verify(repository => repository.SearchBillers("water", "Utilities", true), Times.Once);
        }

        [Fact]
        public void GetSavedBillers_CallsRepo()
        {
            _billPaymentRepository.Setup(repository => repository.GetSavedBillers(7)).Returns(new List<SavedBiller>());
            var sut = CreateSut();

            sut.GetSavedBillers(7);

            _billPaymentRepository.Verify(repository => repository.GetSavedBillers(7), Times.Once);
        }

        [Fact]
        public void SaveBiller_CallsRepoSave()
        {
            var sut = CreateSut();

            sut.SaveBiller(1, 2, "My biller", "Ref");

            _billPaymentRepository.Verify(repository => repository.SaveBiller(It.IsAny<SavedBiller>()), Times.Once);
        }

        [Fact]
        public void RemoveSavedBiller_CallsRepoDelete()
        {
            var sut = CreateSut();

            sut.RemoveSavedBiller(10);

            _billPaymentRepository.Verify(repository => repository.DeleteSavedBiller(10), Times.Once);
        }

        [Theory]
        [InlineData(999.99, false)]
        [InlineData(1000.00, true)]
        [InlineData(2500.00, true)]
        public void Requires2FA_ReturnsExpected(decimal amount, bool expected)
        {
            var sut = CreateSut();

            sut.Requires2FA(amount).Should().Be(expected);
        }

        [Fact]
        public void PayBill_Throws_WhenBillerNotFound()
        {
            _billPaymentRepository.Setup(repository => repository.GetBillerById(99)).Returns((Biller?)null);
            var sut = CreateSut();
            var dto = new BillPaymentDto { UserId = 1, SourceAccountId = 10, BillerId = 99, Amount = 50, BillerReference = "A", TwoFAToken = "123456" };

            Action act = () => sut.PayBill(dto);

            act.Should().Throw<InvalidOperationException>().WithMessage("Biller with ID 99 does not exist.");
        }

        [Fact]
        public void PayBill_CallsPipeline_WithExpectedContext()
        {
            _billPaymentRepository.Setup(repository => repository.GetBillerById(2)).Returns(new Biller { Id = 2, Name = "Electricity Co" });
            _pipelineService.Setup(pipelineService => pipelineService.RunPipeline(It.IsAny<PipelineContext>(), "123456")).Returns(new Transaction { Id = 77 });
            _billPaymentRepository.Setup(repository => repository.Add(It.IsAny<BillPayment>())).Returns(new BillPayment { BillerReference = string.Empty, ReceiptNumber = string.Empty });
            var sut = CreateSut();
            var dto = new BillPaymentDto { UserId = 1, SourceAccountId = 10, BillerId = 2, Amount = 100, BillerReference = "INV-1", TwoFAToken = "123456" };

            sut.PayBill(dto);

            _pipelineService.Verify(pipelineService => pipelineService.RunPipeline(
                It.Is<PipelineContext>(context =>
                    context.UserId == 1 &&
                    context.SourceAccountId == 10 &&
                    context.Amount == 100 &&
                    context.Currency == "RON" &&
                    context.Type == "BillPayment" &&
                    context.Fee == 0.50m &&
                    context.CounterpartyName == "Electricity Co" &&
                    context.RelatedEntityType == "BillPayment" &&
                    context.RelatedEntityId == 0),
                "123456"), Times.Once);
        }

        [Fact]
        public void PayBill_CallsRepositoryAdd()
        {
            _billPaymentRepository.Setup(repository => repository.GetBillerById(2)).Returns(new Biller { Id = 2, Name = "Electricity Co" });
            _pipelineService.Setup(pipelineService => pipelineService.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>())).Returns(new Transaction { Id = 77 });
            _billPaymentRepository.Setup(repository => repository.Add(It.IsAny<BillPayment>())).Returns(new BillPayment { BillerReference = string.Empty, ReceiptNumber = string.Empty });
            var sut = CreateSut();
            var dto = new BillPaymentDto { UserId = 1, SourceAccountId = 10, BillerId = 2, Amount = 150, BillerReference = "INV-2", TwoFAToken = "123456" };

            sut.PayBill(dto);

            _billPaymentRepository.Verify(repository => repository.Add(It.IsAny<BillPayment>()), Times.Once);
        }

        [Fact]
        public void PayBill_ReturnsRepositoryResult()
        {
            _billPaymentRepository.Setup(repository => repository.GetBillerById(2)).Returns(new Biller { Id = 2, Name = "Electricity Co" });
            _pipelineService.Setup(pipelineService => pipelineService.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>())).Returns(new Transaction { Id = 77 });
            var expected = new BillPayment { Id = 5, ReceiptNumber = "RCP-TEST", BillerReference = string.Empty };
            _billPaymentRepository.Setup(repository => repository.Add(It.IsAny<BillPayment>())).Returns(expected);
            var sut = CreateSut();
            var dto = new BillPaymentDto { UserId = 1, SourceAccountId = 10, BillerId = 2, Amount = 150, BillerReference = "INV-2", TwoFAToken = "123456" };

            var result = sut.PayBill(dto);

            result.Should().BeSameAs(expected);
        }

        [Fact]
        public void PayBill_SetsCompletedStatus_OnAddedEntity()
        {
            _billPaymentRepository.Setup(repository => repository.GetBillerById(2)).Returns(new Biller { Id = 2, Name = "Electricity Co" });
            _pipelineService.Setup(pipelineService => pipelineService.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>())).Returns(new Transaction { Id = 77 });
            _billPaymentRepository.Setup(repository => repository.Add(It.IsAny<BillPayment>())).Returns<BillPayment>(billPayment => billPayment);
            var sut = CreateSut();
            var dto = new BillPaymentDto { UserId = 1, SourceAccountId = 10, BillerId = 2, Amount = 150, BillerReference = "INV-2", TwoFAToken = "123456" };

            var result = sut.PayBill(dto);

            result.Status.Should().Be(PaymentStatus.Completed);
        }
    }
}
