using BankingAppTeamB.Services;
using FluentAssertions;
using Xunit;

namespace BankingAppTeamB.Tests.Services
{
    public class UserSessionServiceTests
    {
        private static UserSessionService CreateSut() => new();

        [Fact]
        public void CurrentUserId_ShouldBeDefaultValue()
        {
            var sut = CreateSut();

            sut.CurrentUserId.Should().Be(1);
        }

        [Fact]
        public void CurrentUserName_ShouldBeDefaultValue()
        {
            var sut = CreateSut();

            sut.CurrentUserName.Should().Be("Ion Popescu");
        }

        [Fact]
        public void GetAccounts_ShouldReturnExpectedAccountsInOrder()
        {
            var sut = CreateSut();

            var accounts = sut.GetAccounts();

            accounts.Should().HaveCount(4);

            accounts[0].Id.Should().Be(1);
            accounts[0].IBAN.Should().Be("RO49AAAA1B31007593840000");
            accounts[0].Currency.Should().Be("EUR");
            accounts[0].Balance.Should().Be(5000.00m);
            accounts[0].AccountName.Should().Be("Main EUR Account");
            accounts[0].Status.Should().Be("Active");

            accounts[1].Id.Should().Be(2);
            accounts[1].Currency.Should().Be("USD");

            accounts[2].Id.Should().Be(3);
            accounts[2].Currency.Should().Be("RON");

            accounts[3].Id.Should().Be(4);
            accounts[3].AccountName.Should().Be("Savings EUR Account");
        }

        [Fact]
        public void GetAccounts_ShouldReturnNewListInstanceOnEachCall()
        {
            var sut = CreateSut();

            var first = sut.GetAccounts();
            var second = sut.GetAccounts();

            first.Should().NotBeSameAs(second);
        }
    }
}