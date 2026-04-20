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
        var result = IbanValidator.Validate(ValidIban);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenIbanIsEmpty_ReturnsFalse()
    {
        // Act
        var result = IbanValidator.Validate(EmptyIban);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenIbanIsWhitespace_ReturnsFalse()
    {
        // Act
        var result = IbanValidator.Validate(WhitespaceIban);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenIbanIsTooShort_ReturnsFalse()
    {
        // Act
        var result = IbanValidator.Validate(TooShortIban);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenIbanIsTooLong_ReturnsFalse()
    {
        // Act
        var result = IbanValidator.Validate(TooLongIban);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenCountryCodeIsInvalid_ReturnsFalse()
    {
        // Act
        var result = IbanValidator.Validate(IbanWithInvalidCountryCode);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhenCheckDigitsAreInvalid_ReturnsFalse()
    {
        // Act
        var result = IbanValidator.Validate(IbanWithInvalidCheckDigits);

        // Assert
        result.Should().BeFalse();
    }
}
