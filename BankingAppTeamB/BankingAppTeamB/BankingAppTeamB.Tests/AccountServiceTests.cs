using System;
using BankingAppTeamB.Services;
using FluentAssertions;
using Xunit;

namespace BankingAppTeamB.Tests.Services
{
    public class AccountServiceTests
    {
        private AccountService CreateSut() => new ();

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void DebitAccount_WithZeroOrNegativeAmount_ThrowsArgumentOutOfRangeException(decimal amount)
        {
            var sut = CreateSut();
            Action act = () => sut.DebitAccount(1, amount);
            act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*Amount must be > 0.*");
        }

        [Fact]
        public void DebitAccount_WithInvalidAccountId_ThrowsArgumentException()
        {
            var sut = CreateSut();
            Action act = () => sut.DebitAccount(999, 100);
            act.Should().Throw<ArgumentException>().WithMessage("*Account not found.*");
        }

        [Fact]
        public void DebitAccount_WithInsufficientFunds_ThrowsInvalidOperationException()
        {
            var sut = CreateSut();
            Action act = () => sut.DebitAccount(1, 10000); // Balance is 5000
            act.Should().Throw<InvalidOperationException>().WithMessage("Insufficient funds.");
        }

        [Fact]
        public void DebitAccount_WithValidParameters_DoesNotThrow()
        {
            var sut = CreateSut();
            Action act = () => sut.DebitAccount(1, 100);
            act.Should().NotThrow();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void CreditAccount_WithZeroOrNegativeAmount_ThrowsArgumentOutOfRangeException(decimal amount)
        {
            var sut = CreateSut();
            Action act = () => sut.CreditAccount(1, amount);
            act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("*Amount must be > 0.*");
        }

        [Fact]
        public void CreditAccount_WithInvalidAccountId_ThrowsArgumentException()
        {
            var sut = CreateSut();
            Action act = () => sut.CreditAccount(999, 100);
            act.Should().Throw<ArgumentException>().WithMessage("*Account not found.*");
        }

        [Fact]
        public void CreditAccount_WithValidParameters_DoesNotThrow()
        {
            var sut = CreateSut();
            Action act = () => sut.CreditAccount(1, 100);
            act.Should().NotThrow();
        }

        [Fact]
        public void IsAccountValid_ReturnsTrue()
        {
            var sut = CreateSut();
            sut.IsAccountValid(1).Should().BeTrue();
        }

        [Fact]
        public void GetBalance_ReturnsStubBalance()
        {
            var sut = CreateSut();
            sut.GetBalance(1).Should().Be(50m);
        }
    }
}
