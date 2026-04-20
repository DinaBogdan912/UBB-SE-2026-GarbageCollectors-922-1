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
        var service = new UserSessionService();

        // Assert
        service.CurrentUserId.Should().Be(ExpectedUserId);
        service.CurrentUserName.Should().Be(ExpectedUserName);
    }

    [Fact]
    public void GetAccounts_WhenCalled_ReturnsHardcodedAccountList()
    {
        // Arrange
        var service = new UserSessionService();

        // Act
        var accounts = service.GetAccounts();

        // Assert
        accounts.Should().NotBeNull();
        accounts.Count.Should().Be(ExpectedAccountCount);
        accounts.Should().ContainSingle(a => a.Id == 1 && a.Currency == "EUR" && a.Balance == 5000.00m);
        accounts.Should().ContainSingle(a => a.Id == 2 && a.Currency == "USD" && a.Balance == 1200.00m);
        accounts.Should().ContainSingle(a => a.Id == 3 && a.Currency == "RON" && a.Balance == 8500.00m);
        accounts.Should().ContainSingle(a => a.Id == 4 && a.Currency == "EUR" && a.Balance == 300.00m);
    }
}
