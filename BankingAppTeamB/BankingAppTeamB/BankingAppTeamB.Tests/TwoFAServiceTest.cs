using BankingAppTeamB.Services;
using FluentAssertions;
using Xunit;

namespace BankingAppTeamB.Tests.Services
{
    public class TwoFAServiceTests
    {
        private static TwoFAService CreateSut() => new ();

        [Theory]
        [InlineData(999.99, false)]
        [InlineData(1000.00, true)]
        [InlineData(1500.00, true)]
        public void Requires2FA_ReturnsExpected(decimal amount, bool expected)
        {
            var sut = CreateSut();

            sut.Requires2FA(amount).Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateToken_ReturnsFalse_ForNullOrWhitespace(string? token)
        {
            var sut = CreateSut();

            sut.ValidateToken(token!).Should().BeFalse();
        }

        [Theory]
        [InlineData("1")]
        [InlineData("123456")]
        [InlineData("abc")]
        [InlineData(" token ")]
        public void ValidateToken_ReturnsTrue_ForNonWhitespace(string token)
        {
            var sut = CreateSut();

            sut.ValidateToken(token).Should().BeTrue();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(42)]
        [InlineData(9999)]
        [InlineData(-7)]
        [InlineData(0)]
        public void GenerateToken_ReturnsPlaceholderToken(int userId)
        {
            var sut = CreateSut();

            sut.GenerateToken(userId).Should().Be("123456");
        }
    }
}