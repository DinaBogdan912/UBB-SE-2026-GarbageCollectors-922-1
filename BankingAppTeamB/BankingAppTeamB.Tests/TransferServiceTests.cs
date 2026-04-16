using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Repositories;
using BankingAppTeamB.Services;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace BankingAppTeamB.Tests.Services
{
    public class TransferServiceTests
    {
        private readonly Mock<ITransferRepository> _transferRepo = new();
        private readonly Mock<IBeneficiaryRepository> _beneficiaryRepo = new();
        private readonly Mock<ITransactionPipelineService> _pipeline = new();
        private readonly Mock<IExchangeService> _exchange = new();

        private TransferService CreateSut(bool withExchange = true)
            => new TransferService(
                _transferRepo.Object,
                _beneficiaryRepo.Object,
                _pipeline.Object,
                withExchange ? _exchange.Object : null);

        [Fact]
        public void ValidateIBAN_ReturnsFalse_ForNullOrWhitespace()
        {
            var sut = CreateSut();
            Assert.False(sut.ValidateIBAN(null!));
            Assert.False(sut.ValidateIBAN(""));
            Assert.False(sut.ValidateIBAN("   "));
        }

        [Theory]
        [InlineData("R")] 
        [InlineData("123456789012345")] 
        [InlineData("ROAA56789012345")] 
        public void ValidateIBAN_ReturnsFalse_ForInvalidFormats(string iban)
        {
            var sut = CreateSut();
            Assert.False(sut.ValidateIBAN(iban));
        }

        [Fact]
        public void ValidateIBAN_ReturnsTrue_ForValidBasicFormat()
        {
            var sut = CreateSut();
            Assert.True(sut.ValidateIBAN("RO49AAAA1B31007593840000"));
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
        public void GetBankNameFromIBAN_ReturnsExpected(string iban, string expected)
        {
            var sut = CreateSut();
            Assert.Equal(expected, sut.GetBankNameFromIBAN(iban!));
        }

        [Theory]
        [InlineData(999.99, false)]
        [InlineData(1000.00, true)]
        [InlineData(1500.00, true)]
        public void Requires2FA_ReturnsExpected(decimal amount, bool expected)
        {
            var sut = CreateSut();
            Assert.Equal(expected, sut.Requires2FA(amount));
        }

        [Fact]
        public void GetFxPreview_ReturnsIdentity_WhenSameCurrency()
        {
            var sut = CreateSut();
            var fx = sut.GetFxPreview("USD", "usd", 123.45m);

            Assert.Equal(1m, fx.Rate);
            Assert.Equal(123.45m, fx.ConvertedAmount);
        }

        [Fact]
        public void GetFxPreview_ReturnsIdentity_WhenExchangeServiceNull()
        {
            var sut = CreateSut(withExchange: false);
            var fx = sut.GetFxPreview("USD", "EUR", 100m);

            Assert.Equal(1m, fx.Rate);
            Assert.Equal(100m, fx.ConvertedAmount);
        }

        [Fact]
        public void GetFxPreview_ReturnsIdentity_WhenPairMissing()
        {
            _exchange.Setup(x => x.GetLiveRates()).Returns(new Dictionary<string, decimal>
            {
                ["USD/JPY"] = 150m
            });

            var sut = CreateSut();
            var fx = sut.GetFxPreview("USD", "EUR", 100m);

            Assert.Equal(1m, fx.Rate);
            Assert.Equal(100m, fx.ConvertedAmount);
        }

        [Fact]
        public void GetFxPreview_ReturnsConvertedAndRounded_WhenPairExists()
        {
            _exchange.Setup(x => x.GetLiveRates()).Returns(new Dictionary<string, decimal>
            {
                ["USD/EUR"] = 0.91337m
            });

            var sut = CreateSut();
            var fx = sut.GetFxPreview("usd", "eur", 10m);

            Assert.Equal(0.91337m, fx.Rate);
            Assert.Equal(9.13m, fx.ConvertedAmount); // rounded 2 decimals
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

            Assert.Throws<InvalidOperationException>(() => sut.ExecuteTransfer(dto));
            _pipeline.Verify(x => x.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>()), Times.Never);
            _transferRepo.Verify(x => x.Add(It.IsAny<Transfer>()), Times.Never);
        }

        [Fact]
        public void ExecuteTransfer_AddsTransfer_AndUpdatesMatchingBeneficiary()
        {
            var nowBefore = DateTime.UtcNow;

            _pipeline.Setup(x => x.RunPipeline(It.IsAny<PipelineContext>(), "654321"))
                .Returns(new Transaction { Id = 777 });

            var beneficiary = new Beneficiary
            {
                IBAN = "RO49AAAA1B31007593840000",
                TransferCount = 1,
                TotalAmountSent = 50m
            };

            _beneficiaryRepo.Setup(x => x.GetByUserId(42))
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

            var result = sut.ExecuteTransfer(dto);

            Assert.NotNull(result);
            Assert.Equal(42, result.UserId);
            Assert.Equal(100, result.SourceAccountId);
            Assert.Equal(777, result.TransactionId);
            Assert.Equal("Alice", result.RecipientName);
            Assert.Equal("Romanian Bank", result.RecipientBankName);
            Assert.Equal(TransferStatus.Completed, result.Status);

            _pipeline.Verify(x => x.RunPipeline(
                It.Is<PipelineContext>(c =>
                    c.UserId == dto.UserId &&
                    c.SourceAccountId == dto.SourceAccountId &&
                    c.Amount == dto.Amount &&
                    c.Currency == dto.Currency &&
                    c.Type == "Transfer" &&
                    c.RelatedEntityType == "Transfer"),
                dto.TwoFAToken), Times.Once);

            _transferRepo.Verify(x => x.Add(It.Is<Transfer>(t =>
                t.UserId == dto.UserId &&
                t.SourceAccountId == dto.SourceAccountId &&
                t.TransactionId == 777 &&
                t.RecipientIBAN == dto.RecipientIBAN &&
                t.Amount == dto.Amount &&
                t.Currency == dto.Currency &&
                t.Reference == dto.Reference)), Times.Once);

            _beneficiaryRepo.Verify(x => x.Update(It.Is<Beneficiary>(b =>
                b.IBAN == dto.RecipientIBAN &&
                b.TransferCount == 2 &&
                b.TotalAmountSent == 200m &&
                b.LastTransferDate >= nowBefore)), Times.Once);
        }

        [Fact]
        public void ExecuteTransfer_DoesNotUpdateBeneficiary_WhenNoMatch()
        {
            _pipeline.Setup(x => x.RunPipeline(It.IsAny<PipelineContext>(), It.IsAny<string>()))
                .Returns(new Transaction { Id = 99 });

            _beneficiaryRepo.Setup(x => x.GetByUserId(1))
                .Returns(new List<Beneficiary>
                {
                    new Beneficiary { IBAN = "DE12123456789012345" }
                });

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

            _beneficiaryRepo.Verify(x => x.Update(It.IsAny<Beneficiary>()), Times.Never);
            _transferRepo.Verify(x => x.Add(It.IsAny<Transfer>()), Times.Once);
        }

        [Fact]
        public void GetHistory_ReturnsRepositoryData()
        {
            var expected = new List<Transfer>
            {
                new Transfer { UserId = 7, Amount = 12m },
                new Transfer { UserId = 7, Amount = 34m }
            };

            _transferRepo.Setup(x => x.GetByUserId(7)).Returns(expected);

            var sut = CreateSut();
            var result = sut.GetHistory(7);

            Assert.Same(expected, result);
            _transferRepo.Verify(x => x.GetByUserId(7), Times.Once);
        }
    }
}