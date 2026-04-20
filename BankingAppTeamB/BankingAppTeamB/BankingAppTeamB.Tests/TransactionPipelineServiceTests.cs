using System;
using BankingAppTeamB.Mocks;
using BankingAppTeamB.Models;
using BankingAppTeamB.Repositories;
using BankingAppTeamB.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace BankingAppTeamB.Tests.Services
{
    public class TransactionPipelineServiceTests
    {
        private readonly Mock<ITransactionRepository> transactionRepoMock = new ();
        private readonly Mock<IAccountService> accountServiceMock = new ();
        private readonly TransactionPipelineService sut;

        public TransactionPipelineServiceTests()
        {
            sut = new TransactionPipelineService(transactionRepoMock.Object, accountServiceMock.Object);
        }

        private static PipelineContext ValidContext(decimal amount = 100m, decimal fee = 2m, string currency = "USD")
            => new ()
            {
                SourceAccountId = 1,
                Amount = amount,
                Fee = fee,
                Currency = currency,
                Type = "Transfer",
                CounterpartyName = "Alice",
                RelatedEntityType = "Beneficiary",
                RelatedEntityId = 99
            };

        [Fact]
        public void Validate_Fails_WhenAmountIsZero()
        {
            var result = sut.Validate(ValidContext(amount: 0));
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_Fails_WhenCurrencyIsNull()
        {
            var ctx = ValidContext();
            ctx.Currency = null!;
            var result = sut.Validate(ctx);
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_Fails_WhenCurrencyLengthNot3()
        {
            var result = sut.Validate(ValidContext(currency: "US"));
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_Fails_WhenSourceAccountInvalid()
        {
            var ctx = ValidContext();
            accountServiceMock.Setup(a => a.IsAccountValid(ctx.SourceAccountId)).Returns(false);

            var result = sut.Validate(ctx);

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_Succeeds_WhenInputValid()
        {
            var ctx = ValidContext();
            accountServiceMock.Setup(a => a.IsAccountValid(ctx.SourceAccountId)).Returns(true);

            var result = sut.Validate(ctx);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Authorize_Fails_WhenAmountAtLeast1000_AndTokenMissing()
        {
            var result = sut.Authorize(ValidContext(amount: 1000m), null);
            result.IsAuthorized.Should().BeFalse();
        }

        [Fact]
        public void Authorize_Succeeds_WhenAmountAtLeast1000_WithToken()
        {
            var result = sut.Authorize(ValidContext(amount: 1000m), "123456");
            result.IsAuthorized.Should().BeTrue();
        }

        [Fact]
        public void Authorize_Succeeds_WhenAmountBelow1000_WithoutToken()
        {
            var result = sut.Authorize(ValidContext(amount: 999.99m), null);
            result.IsAuthorized.Should().BeTrue();
        }

        [Fact]
        public void Execute_Succeeds_WhenDebitWorks()
        {
            var result = sut.Execute(ValidContext(amount: 200m, fee: 5m));
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Execute_Debits_AmountPlusFee()
        {
            var ctx = ValidContext(amount: 200m, fee: 5m);

            sut.Execute(ctx);

            accountServiceMock.Verify(a => a.DebitAccount(ctx.SourceAccountId, 205m), Times.Once);
        }

        [Fact]
        public void Execute_Fails_WhenDebitThrows()
        {
            accountServiceMock.Setup(a => a.DebitAccount(It.IsAny<int>(), It.IsAny<decimal>()))
                .Throws(new Exception("Insufficient funds"));

            var result = sut.Execute(ValidContext());

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public void LogTransaction_CallsRepositoryAdd()
        {
            var tx = new Transaction();

            sut.LogTransaction(tx);

            transactionRepoMock.Verify(r => r.Add(tx), Times.Once);
        }

        [Fact]
        public void RunPipeline_Throws_WhenValidationFails()
        {
            Action act = () => sut.RunPipeline(ValidContext(amount: 0));
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void RunPipeline_Throws_WhenAuthorizationFails()
        {
            var ctx = ValidContext(amount: 1000m);
            accountServiceMock.Setup(a => a.IsAccountValid(ctx.SourceAccountId)).Returns(true);

            Action act = () => sut.RunPipeline(ctx, null);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void RunPipeline_Throws_WhenExecutionFails()
        {
            var ctx = ValidContext();
            accountServiceMock.Setup(a => a.IsAccountValid(ctx.SourceAccountId)).Returns(true);
            accountServiceMock.Setup(a => a.DebitAccount(It.IsAny<int>(), It.IsAny<decimal>()))
                .Throws(new Exception("Debit backend down"));

            Action act = () => sut.RunPipeline(ctx);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void RunPipeline_Succeeds_WhenAllStepsPass()
        {
            var ctx = ValidContext();
            accountServiceMock.Setup(a => a.IsAccountValid(ctx.SourceAccountId)).Returns(true);
            accountServiceMock.Setup(a => a.GetBalance(ctx.SourceAccountId)).Returns(5000m);

            var result = sut.RunPipeline(ctx);

            result.Should().NotBeNull();
        }

        [Fact]
        public void RunPipeline_LogsTransaction_WhenAllStepsPass()
        {
            var ctx = ValidContext();
            accountServiceMock.Setup(a => a.IsAccountValid(ctx.SourceAccountId)).Returns(true);
            accountServiceMock.Setup(a => a.GetBalance(ctx.SourceAccountId)).Returns(5000m);

            sut.RunPipeline(ctx);

            transactionRepoMock.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Once);
        }

        [Fact]
        public void GetAccountService_ReturnsInjectedService()
        {
            var result = sut.GetAccountService();
            result.Should().BeSameAs(accountServiceMock.Object);
        }
    }
}