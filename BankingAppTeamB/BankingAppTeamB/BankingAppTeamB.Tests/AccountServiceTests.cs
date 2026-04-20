using System;
using System.Collections.Generic;
using BankingAppTeamB.Models;
using BankingAppTeamB.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace BankingAppTeamB.Tests;

public class AccountServiceTests
{
    private const int ValidAccountId = 1;
    private const int InvalidAccountId = 999;
    private const decimal ValidDebitAmount = 50.00m;
    private const decimal ValidCreditAmount = 100.00m;
    private const decimal InitialBalance = 200.00m;
    private const decimal InsufficientBalance = 10.00m;
    private const decimal ZeroAmount = 0m;
    private const decimal NegativeAmount = -10.00m;
    private const decimal ExpectedZeroBalance = 0m;

    [Fact]
    public void DebitAccount_WhenAmountIsZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var accountService = new AccountService(mockUserSessionService.Object);

        // Act
        Action debitAccountOperation = () => accountService.DebitAccount(ValidAccountId, ZeroAmount);

        // Assert
        debitAccountOperation.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Amount must be > 0.*");
    }

    [Fact]
    public void DebitAccount_WhenAmountIsNegative_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var accountService = new AccountService(mockUserSessionService.Object);

        // Act
        Action debitAccountOperation = () => accountService.DebitAccount(ValidAccountId, NegativeAmount);

        // Assert
        debitAccountOperation.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Amount must be > 0.*");
    }

    [Fact]
    public void DebitAccount_WhenAccountDoesNotExist_ThrowsArgumentException()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var accountService = new AccountService(mockUserSessionService.Object);
        var emptyAccountsList = new List<Account>();

        mockUserSessionService.Setup(service => service.GetAccounts()).Returns(emptyAccountsList);

        // Act
        Action debitAccountOperation = () => accountService.DebitAccount(InvalidAccountId, ValidDebitAmount);

        // Assert
        debitAccountOperation.Should().Throw<ArgumentException>()
            .WithMessage("*Account not found.*");
    }

    [Fact]
    public void DebitAccount_WhenFundsAreInsufficient_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var accountService = new AccountService(mockUserSessionService.Object);
        var accountWithInsufficientFunds = new Account
        {
            Id = ValidAccountId,
            Balance = InsufficientBalance
        };
        var accountsList = new List<Account> { accountWithInsufficientFunds };

        mockUserSessionService.Setup(service => service.GetAccounts()).Returns(accountsList);

        // Act
        Action debitAccountOperation = () => accountService.DebitAccount(ValidAccountId, ValidDebitAmount);

        // Assert
        debitAccountOperation.Should().Throw<InvalidOperationException>()
            .WithMessage("Insufficient funds.");
    }

    [Fact]
    public void DebitAccount_WhenDataIsValid_DeductsAmountFromBalance()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var accountService = new AccountService(mockUserSessionService.Object);
        var validAccount = new Account
        {
            Id = ValidAccountId,
            Balance = InitialBalance
        };
        var accountsList = new List<Account> { validAccount };
        var expectedBalanceAfterDebit = InitialBalance - ValidDebitAmount;

        mockUserSessionService.Setup(service => service.GetAccounts()).Returns(accountsList);

        // Act
        accountService.DebitAccount(ValidAccountId, ValidDebitAmount);

        // Assert
        validAccount.Balance.Should().Be(expectedBalanceAfterDebit);
    }

    [Fact]
    public void CreditAccount_WhenAmountIsZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var accountService = new AccountService(mockUserSessionService.Object);

        // Act
        Action creditAccountOperation = () => accountService.CreditAccount(ValidAccountId, ZeroAmount);

        // Assert
        creditAccountOperation.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Amount must be > 0.*");
    }

    [Fact]
    public void CreditAccount_WhenAmountIsNegative_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var accountService = new AccountService(mockUserSessionService.Object);

        // Act
        Action creditAccountOperation = () => accountService.CreditAccount(ValidAccountId, NegativeAmount);

        // Assert
        creditAccountOperation.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Amount must be > 0.*");
    }

    [Fact]
    public void CreditAccount_WhenAccountDoesNotExist_ThrowsArgumentException()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var accountService = new AccountService(mockUserSessionService.Object);
        var emptyAccountsList = new List<Account>();

        mockUserSessionService.Setup(service => service.GetAccounts()).Returns(emptyAccountsList);

        // Act
        Action creditAccountOperation = () => accountService.CreditAccount(InvalidAccountId, ValidCreditAmount);

        // Assert
        creditAccountOperation.Should().Throw<ArgumentException>()
            .WithMessage("*Account not found.*");
    }

    [Fact]
    public void CreditAccount_WhenDataIsValid_AddsAmountToBalance()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var accountService = new AccountService(mockUserSessionService.Object);
        var validAccount = new Account
        {
            Id = ValidAccountId,
            Balance = InitialBalance
        };
        var accountsList = new List<Account> { validAccount };
        var expectedBalanceAfterCredit = InitialBalance + ValidCreditAmount;

        mockUserSessionService.Setup(service => service.GetAccounts()).Returns(accountsList);

        // Act
        accountService.CreditAccount(ValidAccountId, ValidCreditAmount);

        // Assert
        validAccount.Balance.Should().Be(expectedBalanceAfterCredit);
    }

    [Fact]
    public void IsAccountValid_WhenAccountExists_ReturnsTrue()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var accountService = new AccountService(mockUserSessionService.Object);
        var validAccount = new Account
        {
            Id = ValidAccountId
        };
        var accountsList = new List<Account> { validAccount };

        mockUserSessionService.Setup(service => service.GetAccounts()).Returns(accountsList);

        // Act
        var isValid = accountService.IsAccountValid(ValidAccountId);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsAccountValid_WhenAccountDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var accountService = new AccountService(mockUserSessionService.Object);
        var emptyAccountsList = new List<Account>();

        mockUserSessionService.Setup(service => service.GetAccounts()).Returns(emptyAccountsList);

        // Act
        var isValid = accountService.IsAccountValid(InvalidAccountId);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void GetBalance_WhenAccountExists_ReturnsBalance()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var accountService = new AccountService(mockUserSessionService.Object);
        var validAccount = new Account
        {
            Id = ValidAccountId,
            Balance = InitialBalance
        };
        var accountsList = new List<Account> { validAccount };

        mockUserSessionService.Setup(service => service.GetAccounts()).Returns(accountsList);

        // Act
        var actualBalance = accountService.GetBalance(ValidAccountId);

        // Assert
        actualBalance.Should().Be(InitialBalance);
    }

    [Fact]
    public void GetBalance_WhenAccountDoesNotExist_ReturnsZero()
    {
        // Arrange
        var mockUserSessionService = new Mock<IUserSessionService>();
        var accountService = new AccountService(mockUserSessionService.Object);
        var emptyAccountsList = new List<Account>();

        mockUserSessionService.Setup(service => service.GetAccounts()).Returns(emptyAccountsList);

        // Act
        var actualBalance = accountService.GetBalance(InvalidAccountId);

        // Assert
        actualBalance.Should().Be(ExpectedZeroBalance);
    }
}
