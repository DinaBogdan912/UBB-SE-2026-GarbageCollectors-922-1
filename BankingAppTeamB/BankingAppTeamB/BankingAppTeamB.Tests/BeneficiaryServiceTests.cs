using BankingAppTeamB.Models;
using BankingAppTeamB.Repositories;
using BankingAppTeamB.Services;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;

namespace BankingAppTeamB.Tests.Services
{
    public class BeneficiaryServiceTests
    {
        private readonly Mock<IBeneficiaryRepository> _beneficiaryRepository = new();

        private BeneficiaryService CreateSut() => new(_beneficiaryRepository.Object);

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateIBAN_ReturnsFalse_ForNullOrWhitespace(string? iban)
        {
            var sut = CreateSut();

            sut.ValidateIBAN(iban!).Should().BeFalse();
        }

        [Theory]
        [InlineData("R")]
        [InlineData("RO12345678901234567890123456789012345")]
        public void ValidateIBAN_ReturnsFalse_ForInvalidLength(string iban)
        {
            var sut = CreateSut();

            sut.ValidateIBAN(iban).Should().BeFalse();
        }

        [Theory]
        [InlineData("1O49AAAA1B31007593840000")]
        [InlineData("R149AAAA1B31007593840000")]
        public void ValidateIBAN_ReturnsFalse_WhenFirstTwoCharactersAreNotLetters(string iban)
        {
            var sut = CreateSut();

            sut.ValidateIBAN(iban).Should().BeFalse();
        }

        [Theory]
        [InlineData("ROA9AAAA1B31007593840000")]
        [InlineData("RO4AAAAA1B31007593840000")]
        public void ValidateIBAN_ReturnsFalse_WhenThirdOrFourthCharactersAreNotDigits(string iban)
        {
            var sut = CreateSut();

            sut.ValidateIBAN(iban).Should().BeFalse();
        }

        [Fact]
        public void ValidateIBAN_ReturnsTrue_ForValidIban()
        {
            var sut = CreateSut();

            sut.ValidateIBAN("RO49AAAA1B31007593840000").Should().BeTrue();
        }

        [Fact]
        public void GetByUser_ReturnsRepositoryData()
        {
            var expected = new List<Beneficiary> { new() { UserId = 7, Name = "Ana", IBAN = "RO49AAAA1B31007593840000" } };
            _beneficiaryRepository.Setup(repository => repository.GetByUserId(7)).Returns(expected);
            var sut = CreateSut();

            var result = sut.GetByUser(7);

            result.Should().BeSameAs(expected);
        }

        [Fact]
        public void Add_Throws_WhenIbanInvalid()
        {
            var sut = CreateSut();

            Action act = () => sut.Add("Ana", "BAD", 1);

            act.Should().Throw<ArgumentException>().WithMessage("Invalid IBAN format.");
        }

        [Fact]
        public void Add_Throws_WhenNameEmpty()
        {
            var sut = CreateSut();

            Action act = () => sut.Add(" ", "RO49AAAA1B31007593840000", 1);

            act.Should().Throw<ArgumentException>().WithMessage("Name cannot be empty");
        }

        [Fact]
        public void Add_Throws_WhenDuplicateIbanExists_CaseInsensitive()
        {
            _beneficiaryRepository.Setup(repository => repository.GetByUserId(1)).Returns(new List<Beneficiary>
            {
                new() { IBAN = "RO49AAAA1B31007593840000" }
            });
            var sut = CreateSut();

            Action act = () => sut.Add("Ana", "ro49aaaa1b31007593840000", 1);

            act.Should().Throw<ArgumentException>().WithMessage("A beneficiary with this IBAN already exists for this user.");
        }

        [Fact]
        public void Add_CallsRepoAdd_WhenInputValid()
        {
            _beneficiaryRepository.Setup(repository => repository.GetByUserId(1)).Returns(new List<Beneficiary>());
            var sut = CreateSut();

            sut.Add("Ana", "RO49AAAA1B31007593840000", 1);

            _beneficiaryRepository.Verify(repository => repository.Add(It.IsAny<Beneficiary>()), Times.Once);
        }

        [Fact]
        public void Add_ReturnsBeneficiary_WhenInputValid()
        {
            _beneficiaryRepository.Setup(repository => repository.GetByUserId(1)).Returns(new List<Beneficiary>());
            var sut = CreateSut();

            var result = sut.Add("Ana", "RO49AAAA1B31007593840000", 1);

            result.Should().BeOfType<Beneficiary>();
        }

        [Fact]
        public void Add_PassesCorrectlyMappedBeneficiaryToRepository()
        {
            _beneficiaryRepository.Setup(repository => repository.GetByUserId(1)).Returns(new List<Beneficiary>());
            Beneficiary? captured = null;
            _beneficiaryRepository.Setup(repository => repository.Add(It.IsAny<Beneficiary>()))
                .Callback<Beneficiary>(beneficiary => captured = beneficiary);
            var sut = CreateSut();

            sut.Add("Ana", "RO49AAAA1B31007593840000", 1);

            captured.Should().BeEquivalentTo(new
            {
                UserId = 1,
                Name = "Ana",
                IBAN = "RO49AAAA1B31007593840000"
            });
        }

        [Fact]
        public void Update_Throws_WhenNameEmpty()
        {
            var sut = CreateSut();

            Action act = () => sut.Update(new Beneficiary { Name = " " });

            act.Should().Throw<ArgumentException>().WithMessage("Beneficiary name cannot be empty.");
        }

        [Fact]
        public void Update_CallsRepoUpdate_WhenNameValid()
        {
            var sut = CreateSut();

            sut.Update(new Beneficiary { Name = "Ana", IBAN = "RO49AAAA1B31007593840000" });

            _beneficiaryRepository.Verify(repository => repository.Update(It.IsAny<Beneficiary>()), Times.Once);
        }

        [Fact]
        public void Delete_CallsRepoDelete()
        {
            var sut = CreateSut();

            sut.Delete(123);

            _beneficiaryRepository.Verify(repository => repository.Delete(123), Times.Once);
        }

        [Fact]
        public void BuildTransferDtoFrom_MapsFieldsCorrectly()
        {
            var sut = CreateSut();
            var beneficiary = new Beneficiary { Name = "Ana", IBAN = "RO49AAAA1B31007593840000" };

            var dto = sut.BuildTransferDtoFrom(beneficiary, 55, 9);

            dto.Should().BeEquivalentTo(new
            {
                UserId = 9,
                SourceAccountId = 55,
                RecipientName = "Ana",
                RecipientIBAN = "RO49AAAA1B31007593840000"
            });
        }
    }
}
