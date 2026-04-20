using System;
using System.Collections.Generic;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Repositories;
using BankingAppTeamB.Services;
using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Moq;
using Xunit;

namespace BankingAppTeamB.Tests.Services
{
    public class TransferServiceTests
    {
        private readonly Mock<ITransferRepository> transferRepo = new ();
        private readonly Mock<IBeneficiaryRepository> beneficiaryRepo = new ();
        private readonly Mock<ITransactionPipelineService> pipeline = new ();
        private readonly Mock<IExchangeService> exchange = new ();

        private TransferService CreateSut(bool withExchange = true)
            => new (
                transferRepo.Object,
                beneficiaryRepo.Object,
                pipeline.Object,
                withExchange ? exchange.Object : null);

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateIBAN_ReturnsFalse_ForNullOrWhitespace(string? iban)
        {
            var sut = CreateSut();
            sut.ValidateIBAN(iban!).Should().BeFalse();
        }

        [Theory]
        [InlineData("R")]
        [InlineData("123456789012345")]
        [InlineData("ROAA56789012345")]
        [InlineData("RO49AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAARO49AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
        [InlineData("1O49AAAA1B31007593840000")]
        [InlineData("R149AAAA1B31007593840000")]
        [InlineData("ROA9AAAA1B31007593840000")]
        [InlineData("RO4AAAAA1B31007593840000")]
        public void ValidateIBAN_ReturnsFalse_ForInvalidFormats(string iban)
        {
            var sut = CreateSut();
            sut.ValidateIBAN(iban).Should().BeFalse();
        }

        [Fact]
        public void ValidateIBAN_ReturnsTrue_ForValidBasicFormat()
        {
            var sut = CreateSut();
            sut.ValidateIBAN("RO49AAAA1B31007593840000").Should().BeTrue();
        }

        [Theory]
        [InlineData(null, "Unknown Bank")]
        [InlineData("", "Unknown Bank")]
        [InlineData("R", "Unknown Bank")]
        [InlineData("RO49...", "Romanian Bank")]
        [InlineData("DE12...", "German Bank")]
        [InlineData("GB12...", "UK Bank")]
        [InlineData("FR12...", "French Bank")]
        [InlineData("US12...", "US Bank")]
        [InlineData("ES12...", "International Bank")]
        public void GetBankNameFromIBAN_ReturnsExpected(string? iban, string expected)
        {
            var sut = CreateSut();
            sut.GetBankNameFromIBAN(iban!).Should().Be(expected);
        }

        [Theory]
        [InlineData(999.99, false)]
        [InlineData(1000.00, true)]
        [InlineData(1500.00, true)]
        public void Requires2FA_ReturnsExpected(decimal amount, bool expected)
        {
            var sut = CreateSut();
            sut.Requires2FA(amount).Should().Be(expected);
        }

        [Fact]
        public void GetFxPreview_ReturnsIdentity_WhenSameCurrency()
        {
            var sut = CreateSut();
            var fx = sut.GetFxPreview("USD", "usd", 123.45m);

            fx.Should().BeEquivalentTo(new FxPreview { Rate = 1m, ConvertedAmount = 123.45m });
        }

        [Fact]
        public void GetFxPreview_ReturnsIdentity_WhenExchangeServiceNull()
        {
            var sut = CreateSut(withExchange: false);
            var fx = sut.GetFxPreview("USD", "EUR", 100m);

            fx.Should().BeEquivalentTo(new FxPreview { Rate = 1m, ConvertedAmount = 100m });
        }

        [Fact]
        public void GetFxPreview_ReturnsIdentity_WhenPairMissing()
        {
            exchange.Setup(x => x.GetLiveRates()).Returns(new Dictionary<string, decimal> { ["USD/JPY"] = 150m });
            var sut = CreateSut();

            var fx = sut.GetFxPreview("USD", "EUR", 100m);

            fx.Should().BeEquivalentTo(new FxPreview { Rate = 1m, ConvertedAmount = 100m });
        }

        [Fact]
        public void GetFxPreview_ReturnsConvertedAndRounded_WhenPairExists()
        {
            exchange.Setup(x => x.GetLiveRates()).Returns(new Dictionary<string, decimal> { ["USD/EUR"] = 0.91337m });
            var sut = CreateSut();

            var fx = sut.GetFxPreview("usd", "eur", 10m);

            fx.Should().BeEquivalentTo(new FxPreview { Rate = 0.91337m, ConvertedAmount = 9.13m });
        }

        [Fact]
        public void ExecuteTransfer_Throws_WhenRecipientIbanInvalid()
        {
            var sut = CreateSut();
            var dto = new TransferDto
            {
                UserId = 1,
                SourceAccountId = 10,
                RecipientName = "John",
                RecipientIBAN = "BAD",
                Amount = 100m,
                Currency = "USD",
                Reference = "Ref",
                TwoFAToken = "123456"
            };

            Action act = () => sut.ExecuteTransfer(dto);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ExecuteTransfer_CallsPipeline_WithExpectedContext()
        {
            pipeline.Setup(x => x.RunPipeline(It.IsAny<PipelineContext>(), "654321"))
                .Returns(new Transaction { Id = 777 });
            beneficiaryRepo.Setup(x => x.GetByUserId(42)).Returns(new List<Beneficiary>());

            var sut = CreateSut();
            var dto = new TransferDto
            {
                UserId = 42,
                SourceAccountId = 100,
                RecipientName = "Alice",
                RecipientIBAN = "RO49AAAA1B31007593840000",
                Amount = 150m,
                Currency = "EUR",
                Reference = "Invoice 2026",
                TwoFAToken = "654321"
            };

            sut.ExecuteTransfer(dto);

            pipeline.Verify(x => x.RunPipeline(
                It.Is<PipelineContext>(c =>
                    c.UserId == dto.UserId &&
                    c.SourceAccountId == dto.SourceAccountId &&
                    c.Amount == dto.Amount &&
                    c.Currency == dto.Currency &&
                    c.Type == "Transfer" &&
                    c.RelatedEntityType == "Transfer"),
                dto.TwoFAToken), Times.Once);
        }

        [Fact]
        public void ExecuteTransfer_AddsTransfer_ToRepository()
        {
            pipeline.Setup(x => x.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>()))
                .Returns(new Transaction { Id = 777 });
            beneficiaryRepo.Setup(x => x.GetByUserId(42)).Returns(new List<Beneficiary>());

            var sut = CreateSut();
            var dto = new TransferDto
            {
                UserId = 42,
                SourceAccountId = 100,
                RecipientName = "Alice",
                RecipientIBAN = "RO49AAAA1B31007593840000",
                Amount = 150m,
                Currency = "EUR",
                Reference = "Invoice 2026",
                TwoFAToken = "654321"
            };

            sut.ExecuteTransfer(dto);

            transferRepo.Verify(x => x.Add(It.Is<Transfer>(t =>
                t.UserId == dto.UserId &&
                t.SourceAccountId == dto.SourceAccountId &&
                t.TransactionId == 777 &&
                t.RecipientIBAN == dto.RecipientIBAN &&
                t.Amount == dto.Amount &&
                t.Currency == dto.Currency &&
                t.Reference == dto.Reference &&
                t.Status == TransferStatus.Completed)), Times.Once);
        }

        [Fact]
        public void ExecuteTransfer_UpdatesMatchingBeneficiary()
        {
            pipeline.Setup(x => x.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>()))
                .Returns(new Transaction { Id = 1 });

            var beneficiary = new Beneficiary
            {
                IBAN = "RO49AAAA1B31007593840000",
                TransferCount = 1,
                TotalAmountSent = 50m
            };

            beneficiaryRepo.Setup(x => x.GetByUserId(42))
                .Returns(new List<Beneficiary> { beneficiary });

            var sut = CreateSut();
            var dto = new TransferDto
            {
                UserId = 42,
                SourceAccountId = 100,
                RecipientName = "Alice",
                RecipientIBAN = "RO49AAAA1B31007593840000",
                Amount = 150m,
                Currency = "EUR",
                Reference = "Invoice 2026",
                TwoFAToken = "654321"
            };

            sut.ExecuteTransfer(dto);

            beneficiaryRepo.Verify(x => x.Update(It.Is<Beneficiary>(b =>
                b.IBAN == dto.RecipientIBAN &&
                b.TransferCount == 2 &&
                b.TotalAmountSent == 200m)), Times.Once);
        }

        [Fact]
        public void ExecuteTransfer_DoesNotUpdateBeneficiary_WhenNoMatch()
        {
            pipeline.Setup(x => x.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>()))
                .Returns(new Transaction { Id = 99 });

            beneficiaryRepo.Setup(x => x.GetByUserId(1))
                .Returns(new List<Beneficiary> { new () { IBAN = "DE12123456789012345" } });

            var sut = CreateSut();
            var dto = new TransferDto
            {
                UserId = 1,
                SourceAccountId = 2,
                RecipientName = "Bob",
                RecipientIBAN = "RO49123456789012345",
                Amount = 75m,
                Currency = "USD",
                Reference = "Test",
                TwoFAToken = "222"
            };

            sut.ExecuteTransfer(dto);

            beneficiaryRepo.Verify(x => x.Update(It.IsAny<Beneficiary>()), Times.Never);
        }

        [Fact]
        public void GetHistory_ReturnsRepositoryData()
        {
            var expected = new List<Transfer>
            {
                new () { UserId = 7, Amount = 12m },
                new () { UserId = 7, Amount = 34m }
            };
            transferRepo.Setup(x => x.GetByUserId(7)).Returns(expected);

            var sut = CreateSut();
            var result = sut.GetHistory(7);

            result.Should().BeSameAs(expected);
        }
    }
}