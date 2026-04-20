using System;
using BankingAppTeamB.Models;
using BankingAppTeamB.Repositories;
using BankingAppTeamB.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace BankingAppTeamB.Tests.Services;

public class TransactionPipelineServiceTests
{
    private const int DefaultSourceAccountId = 1;
    private const decimal ValidAmount = 150m;
    private const decimal NegativeAmount = -50m;
    private const decimal ZeroAmount = 0m;
    private const string ValidCurrency = "EUR";
    private const string InvalidCurrency = "US";
    private const string TwoFaRequiredToken = "123456";
    private const string MissingTwoFaToken = "";
    private const decimal AmountRequiringTwoFa = 1500m;
    private const decimal FeeAmount = 1.5m;
    private const string DefaultCounterparty = "Test Company";
    private const string TransactionType = "Transfer";
    private const string RelatedEntityType = "Transfer";
    private const int RelatedEntityId = 0;

    [Fact]
    public void Validate_WhenAmountIsZero_ReturnsFailure()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = ZeroAmount };

        // Act
        var result = service.Validate(context);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Be("Amount must be greater than zero.");
    }

    [Fact]
    public void Validate_WhenAmountIsNegative_ReturnsFailure()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = NegativeAmount };

        // Act
        var result = service.Validate(context);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Be("Amount must be greater than zero.");
    }

    [Fact]
    public void Validate_WhenCurrencyIsInvalidLength_ReturnsFailure()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = ValidAmount, Currency = InvalidCurrency };

        // Act
        var result = service.Validate(context);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Be("Currency code must be exactly 3 characters.");
    }

    [Fact]
    public void Validate_WhenAccountIsInvalid_ReturnsFailure()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        mockAccountService.Setup(s => s.IsAccountValid(DefaultSourceAccountId)).Returns(false);
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = ValidAmount, Currency = ValidCurrency, SourceAccountId = DefaultSourceAccountId };

        // Act
        var result = service.Validate(context);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Message.Should().Be("Source account is invalid or does not exist.");
    }

    [Fact]
    public void Validate_WhenContextIsValid_ReturnsSuccess()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        mockAccountService.Setup(s => s.IsAccountValid(DefaultSourceAccountId)).Returns(true);
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = ValidAmount, Currency = ValidCurrency, SourceAccountId = DefaultSourceAccountId };

        // Act
        var result = service.Validate(context);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Authorize_WhenTwoFaIsRequiredAndMissing_ReturnsFailure()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = AmountRequiringTwoFa };

        // Act
        var result = service.Authorize(context, MissingTwoFaToken);

        // Assert
        result.IsAuthorized.Should().BeFalse();
        result.Message.Should().Be("A 2FA token is required for transfers of 1000 or more.");
    }

    [Fact]
    public void Authorize_WhenTwoFaIsRequiredAndPresent_ReturnsSuccess()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = AmountRequiringTwoFa };

        // Act
        var result = service.Authorize(context, TwoFaRequiredToken);

        // Assert
        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public void Authorize_WhenTwoFaIsNotRequired_ReturnsSuccess()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = ValidAmount };

        // Act
        var result = service.Authorize(context, MissingTwoFaToken);

        // Assert
        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public void Execute_WhenDebitSucceeds_ReturnsSuccess()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = ValidAmount, Fee = FeeAmount, SourceAccountId = DefaultSourceAccountId };

        // Act
        var result = service.Execute(context);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockAccountService.Verify(s => s.DebitAccount(DefaultSourceAccountId, ValidAmount + FeeAmount), Times.Once);
    }

    [Fact]
    public void Execute_WhenDebitFails_ReturnsFailure()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        var exceptionMessage = "Insufficient funds";
        mockAccountService.Setup(s => s.DebitAccount(DefaultSourceAccountId, It.IsAny<decimal>())).Throws(new InvalidOperationException(exceptionMessage));
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = ValidAmount, Fee = FeeAmount, SourceAccountId = DefaultSourceAccountId };

        // Act
        var result = service.Execute(context);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be($"Debit failed: {exceptionMessage}");
    }

    [Fact]
    public void RunPipeline_WhenValidationFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = ZeroAmount };

        // Act
        Action runAction = () => service.RunPipeline(context, MissingTwoFaToken);

        // Assert
        runAction.Should().Throw<InvalidOperationException>().WithMessage("Amount must be greater than zero.");
    }

    [Fact]
    public void RunPipeline_WhenAuthorizationFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        mockAccountService.Setup(s => s.IsAccountValid(DefaultSourceAccountId)).Returns(true);
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = AmountRequiringTwoFa, Currency = ValidCurrency, SourceAccountId = DefaultSourceAccountId };

        // Act
        Action runAction = () => service.RunPipeline(context, MissingTwoFaToken);

        // Assert
        runAction.Should().Throw<InvalidOperationException>().WithMessage("A 2FA token is required for transfers of 1000 or more.");
    }

    [Fact]
    public void RunPipeline_WhenExecutionFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        var exceptionMessage = "Insufficient funds";
        mockAccountService.Setup(s => s.IsAccountValid(DefaultSourceAccountId)).Returns(true);
        mockAccountService.Setup(s => s.DebitAccount(DefaultSourceAccountId, It.IsAny<decimal>())).Throws(new InvalidOperationException(exceptionMessage));
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = ValidAmount, Currency = ValidCurrency, SourceAccountId = DefaultSourceAccountId };

        // Act
        Action runAction = () => service.RunPipeline(context, MissingTwoFaToken);

        // Assert
        runAction.Should().Throw<InvalidOperationException>().WithMessage($"Debit failed: {exceptionMessage}");
    }

    [Fact]
    public void RunPipeline_WhenSuccessful_LogsAndReturnsTransaction()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        var expectedBalance = 500m;
        
        mockAccountService.Setup(s => s.IsAccountValid(DefaultSourceAccountId)).Returns(true);
        mockAccountService.Setup(s => s.GetBalance(DefaultSourceAccountId)).Returns(expectedBalance);
        
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        
        var context = new PipelineContext 
        { 
            Amount = ValidAmount, 
            Currency = ValidCurrency, 
            SourceAccountId = DefaultSourceAccountId,
            Fee = FeeAmount,
            CounterpartyName = DefaultCounterparty,
            Type = TransactionType,
            RelatedEntityType = RelatedEntityType,
            RelatedEntityId = RelatedEntityId
        };
        
        var expectedToleranceForCreationTime = TimeSpan.FromSeconds(2);

        // Act
        var result = service.RunPipeline(context, MissingTwoFaToken);

        // Assert
        result.Should().NotBeNull();
        result.AccountId.Should().Be(DefaultSourceAccountId);
        result.Type.Should().Be(TransactionType);
        result.Direction.Should().Be("Debit");
        result.Amount.Should().Be(ValidAmount);
        result.Currency.Should().Be(ValidCurrency);
        result.BalanceAfter.Should().Be(expectedBalance);
        result.CounterpartyName.Should().Be(DefaultCounterparty);
        result.Fee.Should().Be(FeeAmount);
        result.Status.Should().Be("Completed");
        result.RelatedEntityType.Should().Be(RelatedEntityType);
        result.RelatedEntityId.Should().Be(RelatedEntityId);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, expectedToleranceForCreationTime);

        mockTransactionRepository.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Once);
    }
}
