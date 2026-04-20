using System;
using BankingAppTeamB.Services;
using FluentAssertions;
using Xunit;

namespace BankingAppTeamB.Tests;

public class TwoFAServiceTests
{
    private const decimal AmountRequiringTwoFactor = 1000m;
    private const decimal AmountNotRequiringTwoFactor = 999.99m;
    private const int DefaultUserId = 1;
    private const string ExpectedPlaceholderToken = "123456";
    private const string EmptyToken = "";
    private const string WhitespaceToken = "   ";
    private const string ValidToken = "123456";

    [Fact]
    public void Requires2FA_WhenAmountIsBelowThreshold_ReturnsFalse()
    {
        // Arrange
        var twoFactorService = new TwoFAService();

        // Act
        var result = twoFactorService.Requires2FA(AmountNotRequiringTwoFactor);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Requires2FA_WhenAmountIsAtOrAboveThreshold_ReturnsTrue()
    {
        // Arrange
        var twoFactorService = new TwoFAService();

        // Act
        var result = twoFactorService.Requires2FA(AmountRequiringTwoFactor);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_WhenTokenIsEmpty_ReturnsFalse()
    {
        // Arrange
        var twoFactorService = new TwoFAService();

        // Act
        var result = twoFactorService.ValidateToken(EmptyToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_WhenTokenIsWhitespace_ReturnsFalse()
    {
        // Arrange
        var twoFactorService = new TwoFAService();

        // Act
        var result = twoFactorService.ValidateToken(WhitespaceToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_WhenTokenIsValid_ReturnsTrue()
    {
        // Arrange
        var twoFactorService = new TwoFAService();

        // Act
        var result = twoFactorService.ValidateToken(ValidToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_WhenCalled_ReturnsPlaceholderToken()
    {
        // Arrange
        var twoFactorService = new TwoFAService();

        // Act
        var result = twoFactorService.GenerateToken(DefaultUserId);

        // Assert
        result.Should().Be(ExpectedPlaceholderToken);
    }
}
