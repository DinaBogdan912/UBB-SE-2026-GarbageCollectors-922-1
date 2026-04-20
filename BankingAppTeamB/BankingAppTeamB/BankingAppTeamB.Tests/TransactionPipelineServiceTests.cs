using BankingAppTeamB.Mocks;
using BankingAppTeamB.Models;
using BankingAppTeamB.Repositories;
using BankingAppTeamB.Services;
using FluentAssertions;
using Moq;
using System;
using Xunit;

namespace BankingAppTeamB.Tests.Services
{
    public class TransactionPipelineServiceTests
    {
        private readonly Mock<ITransactionRepository> _transactionRepositoryMock = new();
        private readonly Mock<IAccountService> _accountServiceMock = new();
        private readonly TransactionPipelineService _sut;

        public TransactionPipelineServiceTests()
        {
            _sut = new TransactionPipelineService(_transactionRepositoryMock.Object, _accountServiceMock.Object);
        }

        private static PipelineContext ValidContext(decimal amount = 100m, decimal fee = 2m, string currency = "USD")
            => new()
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
            var result = _sut.Validate(ValidContext(amount: 0));

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_Fails_WhenCurrencyIsNull()
        {
            var context = ValidContext();
            context.Currency = null!;

            var result = _sut.Validate(context);

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_Fails_WhenCurrencyLengthNot3()
        {
            var result = _sut.Validate(ValidContext(currency: "US"));

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_Fails_WhenSourceAccountInvalid()
        {
            var context = ValidContext();
            _accountServiceMock.Setup(accountService => accountService.IsAccountValid(context.SourceAccountId)).Returns(false);

            var result = _sut.Validate(context);

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_Succeeds_WhenInputValid()
        {
            var context = ValidContext();
            _accountServiceMock.Setup(accountService => accountService.IsAccountValid(context.SourceAccountId)).Returns(true);

            var result = _sut.Validate(context);

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Authorize_Fails_WhenAmountAtLeast1000_AndTokenMissing()
        {
            var result = _sut.Authorize(ValidContext(amount: 1000m), null);

            result.IsAuthorized.Should().BeFalse();
        }

        [Fact]
        public void Authorize_Succeeds_WhenAmountAtLeast1000_WithToken()
        {
            var result = _sut.Authorize(ValidContext(amount: 1000m), "123456");

            result.IsAuthorized.Should().BeTrue();
        }

        [Fact]
        public void Authorize_Succeeds_WhenAmountBelow1000_WithoutToken()
        {
            var result = _sut.Authorize(ValidContext(amount: 999.99m), null);

            result.IsAuthorized.Should().BeTrue();
        }

        [Fact]
        public void Execute_Succeeds_WhenDebitWorks()
        {
            var result = _sut.Execute(ValidContext(amount: 200m, fee: 5m));

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void Execute_Debits_AmountPlusFee()
        {
            var context = ValidContext(amount: 200m, fee: 5m);

            _sut.Execute(context);

            _accountServiceMock.Verify(accountService => accountService.DebitAccount(context.SourceAccountId, 205m), Times.Once);
        }

        [Fact]
        public void Execute_Fails_WhenDebitThrows()
        {
            _accountServiceMock.Setup(accountService => accountService.DebitAccount(It.IsAny<int>(), It.IsAny<decimal>()))
                .Throws(new Exception("Insufficient funds"));

            var result = _sut.Execute(ValidContext());

            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public void LogTransaction_CallsRepositoryAdd()
        {
            var transaction = new Transaction();

            _sut.LogTransaction(transaction);

            _transactionRepositoryMock.Verify(repository => repository.Add(transaction), Times.Once);
        }

        [Fact]
        public void RunPipeline_Throws_WhenValidationFails()
        {
            Action act = () => _sut.RunPipeline(ValidContext(amount: 0));

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void RunPipeline_Throws_WhenAuthorizationFails()
        {
            var context = ValidContext(amount: 1000m);
            _accountServiceMock.Setup(accountService => accountService.IsAccountValid(context.SourceAccountId)).Returns(true);

            Action act = () => _sut.RunPipeline(context, null);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void RunPipeline_Throws_WhenExecutionFails()
        {
            var context = ValidContext();
            _accountServiceMock.Setup(accountService => accountService.IsAccountValid(context.SourceAccountId)).Returns(true);
            _accountServiceMock.Setup(accountService => accountService.DebitAccount(It.IsAny<int>(), It.IsAny<decimal>()))
                .Throws(new Exception("Debit backend down"));

            Action act = () => _sut.RunPipeline(context);

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void RunPipeline_Succeeds_WhenAllStepsPass()
        {
            var context = ValidContext();
            _accountServiceMock.Setup(accountService => accountService.IsAccountValid(context.SourceAccountId)).Returns(true);
            _accountServiceMock.Setup(accountService => accountService.GetBalance(context.SourceAccountId)).Returns(5000m);

            var result = _sut.RunPipeline(context);

            result.Should().NotBeNull();
        }

        [Fact]
        public void RunPipeline_LogsTransaction_WhenAllStepsPass()
        {
            var context = ValidContext();
            _accountServiceMock.Setup(accountService => accountService.IsAccountValid(context.SourceAccountId)).Returns(true);
            _accountServiceMock.Setup(accountService => accountService.GetBalance(context.SourceAccountId)).Returns(5000m);

            _sut.RunPipeline(context);

            _transactionRepositoryMock.Verify(repository => repository.Add(It.IsAny<Transaction>()), Times.Once);
        }

        [Fact]
        public void GetAccountService_ReturnsInjectedService()
        {
            var result = _sut.GetAccountService();

            result.Should().BeSameAs(_accountServiceMock.Object);
        }
    }
}
