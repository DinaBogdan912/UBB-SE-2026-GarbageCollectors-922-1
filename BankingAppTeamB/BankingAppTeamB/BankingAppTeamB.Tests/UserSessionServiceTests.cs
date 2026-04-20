using System;
using System.Collections.Generic;
using BankingAppTeamB.Models;
using BankingAppTeamB.Services;
using FluentAssertions;
using Xunit;

namespace BankingAppTeamB.Tests;

public class UserSessionServiceTests
{
    private const int ExpectedUserId = 1;
    private const string ExpectedUserName = "Ion Popescu";
    private const int ExpectedAccountCount = 4;

    [Fact]
    public void Constructor_WhenCreated_SetsExpectedDefaults()
    {
        // Arrange & Act
        var userSessionService = new UserSessionService();

        // Assert
        userSessionService.CurrentUserId.Should().Be(ExpectedUserId);
        userSessionService.CurrentUserName.Should().Be(ExpectedUserName);
    }

    [Fact]
    public void GetAccounts_WhenCalled_ReturnsHardcodedAccountList()
    {
        // Arrange
        var userSessionService = new UserSessionService();

        // Act
        var accounts = userSessionService.GetAccounts();

        // Assert
        accounts.Should().NotBeNull();
        accounts.Count.Should().Be(ExpectedAccountCount);
        accounts.Should().ContainSingle(account => account.Id == 1 && account.Currency == "EUR" && account.Balance == 5000.00m);
        accounts.Should().ContainSingle(account => account.Id == 2 && account.Currency == "USD" && account.Balance == 1200.00m);
        accounts.Should().ContainSingle(account => account.Id == 3 && account.Currency == "RON" && account.Balance == 8500.00m);
        accounts.Should().ContainSingle(account => account.Id == 4 && account.Currency == "EUR" && account.Balance == 300.00m);
    }
}
