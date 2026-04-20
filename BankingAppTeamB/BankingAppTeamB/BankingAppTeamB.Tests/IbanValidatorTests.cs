using System;
using BankingAppTeamB.Services;
using FluentAssertions;
using Xunit;

namespace BankingAppTeamB.Tests;

public class IbanValidatorTests
{
    private const string ValidIban = "RO12BTRL0000000000000000";
    private const string TooShortIban = "RO12BTRL";
    private const string TooLongIban = "RO12BTRL0000000000000000000000000000000";
    private const string IbanWithInvalidCountryCode = "1212BTRL0000000000000000";
    private const string IbanWithInvalidCheckDigits = "ROXXBTRL0000000000000000";
    private const string EmptyIban = "";
    private const string WhitespaceIban = "   ";

    [Fact]
    public void Validate_WhenIbanIsValid_ReturnsTrue()
    {
        // Act
        var actualResult = IbanValidator.Validate(ValidIban);

        // Assert
        actualResult.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenIbanIsEmpty_ReturnsFalse()
    {
        // Act
        var actualResult = IbanValidator.Validate(EmptyIban);

        // Assert
        actualResult.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenIbanIsWhitespace_ReturnsFalse()
    {
        // Act
        var actualResult = IbanValidator.Validate(WhitespaceIban);

        // Assert
        actualResult.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenIbanIsTooShort_ReturnsFalse()
    {
        // Act
        var actualResult = IbanValidator.Validate(TooShortIban);

        // Assert
        actualResult.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenIbanIsTooLong_ReturnsFalse()
    {
        // Act
        var actualResult = IbanValidator.Validate(TooLongIban);

        // Assert
        actualResult.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenCountryCodeIsInvalid_ReturnsFalse()
    {
        // Act
        var actualResult = IbanValidator.Validate(IbanWithInvalidCountryCode);

        // Assert
        actualResult.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenCheckDigitsAreInvalid_ReturnsFalse()
    {
        // Act
        var actualResult = IbanValidator.Validate(IbanWithInvalidCheckDigits);

        // Assert
        actualResult.Should().BeFalse();
    }
}
