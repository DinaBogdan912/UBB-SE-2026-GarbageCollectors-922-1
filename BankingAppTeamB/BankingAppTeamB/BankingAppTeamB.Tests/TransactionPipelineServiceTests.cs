using System;
using BankingAppTeamB.Models;
using BankingAppTeamB.Repositories;
using BankingAppTeamB.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace BankingAppTeamB.Tests;

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
        var actualResult = service.Validate(context);

        // Assert
        actualResult.IsValid.Should().BeFalse();
        actualResult.Message.Should().Be("Amount must be greater than zero.");
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
        var actualResult = service.Validate(context);

        // Assert
        actualResult.IsValid.Should().BeFalse();
        actualResult.Message.Should().Be("Amount must be greater than zero.");
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
        var actualResult = service.Validate(context);

        // Assert
        actualResult.IsValid.Should().BeFalse();
        actualResult.Message.Should().Be("Currency code must be exactly 3 characters.");
    }

    [Fact]
    public void Validate_WhenAccountIsInvalid_ReturnsFailure()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        mockAccountService.Setup(mockServiceDependency => mockServiceDependency.IsAccountValid(DefaultSourceAccountId)).Returns(false);
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = ValidAmount, Currency = ValidCurrency, SourceAccountId = DefaultSourceAccountId };

        // Act
        var actualResult = service.Validate(context);

        // Assert
        actualResult.IsValid.Should().BeFalse();
        actualResult.Message.Should().Be("Source account is invalid or does not exist.");
    }

    [Fact]
    public void Validate_WhenContextIsValid_ReturnsSuccess()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        mockAccountService.Setup(mockServiceDependency => mockServiceDependency.IsAccountValid(DefaultSourceAccountId)).Returns(true);
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = ValidAmount, Currency = ValidCurrency, SourceAccountId = DefaultSourceAccountId };

        // Act
        var actualResult = service.Validate(context);

        // Assert
        actualResult.IsValid.Should().BeTrue();
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
        var actualResult = service.Authorize(context, MissingTwoFaToken);

        // Assert
        actualResult.IsAuthorized.Should().BeFalse();
        actualResult.Message.Should().Be("A 2FA token is required for transfers of 1000 or more.");
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
        var actualResult = service.Authorize(context, TwoFaRequiredToken);

        // Assert
        actualResult.IsAuthorized.Should().BeTrue();
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
        var actualResult = service.Authorize(context, MissingTwoFaToken);

        // Assert
        actualResult.IsAuthorized.Should().BeTrue();
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
        var actualResult = service.Execute(context);

        // Assert
        actualResult.IsSuccess.Should().BeTrue();
        mockAccountService.Verify(mockServiceDependency => mockServiceDependency.DebitAccount(DefaultSourceAccountId, ValidAmount + FeeAmount), Times.Once);
    }

    [Fact]
    public void Execute_WhenDebitFails_ReturnsFailure()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        var exceptionMessage = "Insufficient funds";
        mockAccountService.Setup(mockServiceDependency => mockServiceDependency.DebitAccount(DefaultSourceAccountId, It.IsAny<decimal>())).Throws(new InvalidOperationException(exceptionMessage));
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = ValidAmount, Fee = FeeAmount, SourceAccountId = DefaultSourceAccountId };

        // Act
        var actualResult = service.Execute(context);

        // Assert
        actualResult.IsSuccess.Should().BeFalse();
        actualResult.Message.Should().Be($"Debit failed: {exceptionMessage}");
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
        Action runPipelineOperation = () => service.RunPipeline(context, MissingTwoFaToken);

        // Assert
        runPipelineOperation.Should().Throw<InvalidOperationException>().WithMessage("Amount must be greater than zero.");
    }

    [Fact]
    public void RunPipeline_WhenAuthorizationFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        mockAccountService.Setup(mockServiceDependency => mockServiceDependency.IsAccountValid(DefaultSourceAccountId)).Returns(true);
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = AmountRequiringTwoFa, Currency = ValidCurrency, SourceAccountId = DefaultSourceAccountId };

        // Act
        Action runPipelineOperation = () => service.RunPipeline(context, MissingTwoFaToken);

        // Assert
        runPipelineOperation.Should().Throw<InvalidOperationException>().WithMessage("A 2FA token is required for transfers of 1000 or more.");
    }

    [Fact]
    public void RunPipeline_WhenExecutionFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        var exceptionMessage = "Insufficient funds";
        mockAccountService.Setup(mockServiceDependency => mockServiceDependency.IsAccountValid(DefaultSourceAccountId)).Returns(true);
        mockAccountService.Setup(mockServiceDependency => mockServiceDependency.DebitAccount(DefaultSourceAccountId, It.IsAny<decimal>())).Throws(new InvalidOperationException(exceptionMessage));
        var service = new TransactionPipelineService(mockTransactionRepository.Object, mockAccountService.Object);
        var context = new PipelineContext { Amount = ValidAmount, Currency = ValidCurrency, SourceAccountId = DefaultSourceAccountId };

        // Act
        Action runPipelineOperation = () => service.RunPipeline(context, MissingTwoFaToken);

        // Assert
        runPipelineOperation.Should().Throw<InvalidOperationException>().WithMessage($"Debit failed: {exceptionMessage}");
    }

    [Fact]
    public void RunPipeline_WhenSuccessful_LogsAndReturnsTransaction()
    {
        // Arrange
        var mockTransactionRepository = new Mock<ITransactionRepository>();
        var mockAccountService = new Mock<IAccountService>();
        var expectedBalance = 500m;
        mockAccountService.Setup(mockServiceDependency => mockServiceDependency.IsAccountValid(DefaultSourceAccountId)).Returns(true);
        mockAccountService.Setup(mockServiceDependency => mockServiceDependency.GetBalance(DefaultSourceAccountId)).Returns(expectedBalance);
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

        var expectedToleranceForCreationTime = TimeSpan.FromSeconds(2); // is not magic number
        // Basically the same as int 2

        // Act
        var actualResult = service.RunPipeline(context, MissingTwoFaToken);

        // Assert
        actualResult.Should().NotBeNull();
        actualResult.AccountId.Should().Be(DefaultSourceAccountId);
        actualResult.Type.Should().Be(TransactionType);
        actualResult.Direction.Should().Be("Debit");
        actualResult.Amount.Should().Be(ValidAmount);
        actualResult.Currency.Should().Be(ValidCurrency);
        actualResult.BalanceAfter.Should().Be(expectedBalance);
        actualResult.CounterpartyName.Should().Be(DefaultCounterparty);
        actualResult.Fee.Should().Be(FeeAmount);
        actualResult.Status.Should().Be("Completed");
        actualResult.RelatedEntityType.Should().Be(RelatedEntityType);
        actualResult.RelatedEntityId.Should().Be(RelatedEntityId);
        actualResult.CreatedAt.Should().BeCloseTo(DateTime.Now, expectedToleranceForCreationTime);

        mockTransactionRepository.Verify(mockRepositoryInstance => mockRepositoryInstance.Add(It.IsAny<Transaction>()), Times.Once);
    }
}
