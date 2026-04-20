using System;
using System.Collections.Generic;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Repositories;
using BankingAppTeamB.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace BankingAppTeamB.Tests;

public class BeneficiaryServiceTests
{
    private const int DefaultUserId = 1;
    private const int DefaultBeneficiaryId = 10;
    private const int DefaultSourceAccountId = 100;
    private const string ValidIban = "RO12XXXX000000000000000";
    private const string InvalidIban = "INVALID123";
    private const string DefaultName = "John Doe";
    private const string DefaultBankName = "Test Bank";
    private const string EmptyName = "";

    [Fact]
    public void GetByUser_WhenCalled_ReturnsBeneficiaries()
    {
        // Arrange
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var testedService = new BeneficiaryService(mockBeneficiaryRepository.Object);
        var expectedBeneficiaries = new List<Beneficiary>
        {
            new Beneficiary { Id = DefaultBeneficiaryId, UserId = DefaultUserId, Name = DefaultName, IBAN = ValidIban }
        };

        mockBeneficiaryRepository.Setup(repository => repository.GetByUserId(DefaultUserId)).Returns(expectedBeneficiaries);

        // Act
        var result = beneficiaryService.GetByUser(DefaultUserId);

        // Assert
        result.Should().BeEquivalentTo(expectedBeneficiaries);
        mockBeneficiaryRepository.Verify(repository => repository.GetByUserId(DefaultUserId), Times.Once);
    }

    [Fact]
    public void ValidateIBAN_WhenIbanIsValid_ReturnsTrue()
    {
        // Arrange
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var testedService = new BeneficiaryService(mockBeneficiaryRepository.Object);

        // Act
        var result = beneficiaryService.ValidateIBAN(ValidIban);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateIBAN_WhenIbanIsInvalid_ReturnsFalse()
    {
        // Arrange
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var testedService = new BeneficiaryService(mockBeneficiaryRepository.Object);

        // Act
        var result = beneficiaryService.ValidateIBAN(InvalidIban);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Add_WhenIbanIsInvalid_ThrowsArgumentException()
    {
        // Arrange
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var testedService = new BeneficiaryService(mockBeneficiaryRepository.Object);

        // Act
        Action addAction = () => beneficiaryService.Add(DefaultName, InvalidIban, DefaultUserId);

        // Assert
        addAction.Should().Throw<ArgumentException>().WithMessage("Invalid IBAN format.");
        mockBeneficiaryRepository.Verify(repository => repository.Add(It.IsAny<Beneficiary>()), Times.Never);
    }

    [Fact]
    public void Add_WhenNameIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var testedService = new BeneficiaryService(mockBeneficiaryRepository.Object);

        // Act
        Action addAction = () => beneficiaryService.Add(EmptyName, ValidIban, DefaultUserId);

        // Assert
        addAction.Should().Throw<ArgumentException>().WithMessage("Name cannot be empty");
        mockBeneficiaryRepository.Verify(repository => repository.Add(It.IsAny<Beneficiary>()), Times.Never);
    }

    [Fact]
    public void Add_WhenIbanAlreadyExists_ThrowsArgumentException()
    {
        // Arrange
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var testedService = new BeneficiaryService(mockBeneficiaryRepository.Object);
        var existingBeneficiaries = new List<Beneficiary>
        {
            new Beneficiary { IBAN = ValidIban }
        };

        mockBeneficiaryRepository.Setup(repository => repository.GetByUserId(DefaultUserId)).Returns(existingBeneficiaries);

        // Act
        Action addAction = () => beneficiaryService.Add(DefaultName, ValidIban, DefaultUserId);

        // Assert
        addAction.Should().Throw<ArgumentException>().WithMessage("A beneficiary with this IBAN already exists for this user.");
        mockBeneficiaryRepository.Verify(repository => repository.Add(It.IsAny<Beneficiary>()), Times.Never);
    }

    [Fact]
    public void Add_WhenDataIsValid_SavesAndReturnsBeneficiary()
    {
        // Arrange
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var testedService = new BeneficiaryService(mockBeneficiaryRepository.Object);

        mockBeneficiaryRepository.Setup(repository => repository.GetByUserId(DefaultUserId)).Returns(new List<Beneficiary>());
        mockBeneficiaryRepository.Setup(repository => repository.Add(It.IsAny<Beneficiary>()));

        var expectedToleranceForCreationTime = TimeSpan.FromSeconds(2);

        // Act
        var result = beneficiaryService.Add(DefaultName, ValidIban, DefaultUserId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(DefaultUserId);
        result.Name.Should().Be(DefaultName);
        result.IBAN.Should().Be(ValidIban);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, expectedToleranceForCreationTime);
        mockBeneficiaryRepository.Verify(repository => repository.Add(It.IsAny<Beneficiary>()), Times.Once);
    }

    [Fact]
    public void Update_WhenNameIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var testedService = new BeneficiaryService(mockBeneficiaryRepository.Object);
        var beneficiary = new Beneficiary { Name = EmptyName };

        // Act
        Action updateAction = () => beneficiaryService.Update(beneficiary);

        // Assert
        updateAction.Should().Throw<ArgumentException>().WithMessage("Beneficiary name cannot be empty.");
        mockBeneficiaryRepository.Verify(repository => repository.Update(It.IsAny<Beneficiary>()), Times.Never);
    }

    [Fact]
    public void Update_WhenDataIsValid_UpdatesBeneficiary()
    {
        // Arrange
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var testedService = new BeneficiaryService(mockBeneficiaryRepository.Object);
        var beneficiary = new Beneficiary { Name = DefaultName };

        // Act
        beneficiaryService.Update(beneficiary);

        // Assert
        mockBeneficiaryRepository.Verify(repository => repository.Update(beneficiary), Times.Once);
    }

    [Fact]
    public void Delete_WhenCalled_DeletesBeneficiary()
    {
        // Arrange
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var testedService = new BeneficiaryService(mockBeneficiaryRepository.Object);

        // Act
        beneficiaryService.Delete(DefaultBeneficiaryId);

        // Assert
        mockBeneficiaryRepository.Verify(repository => repository.Delete(DefaultBeneficiaryId), Times.Once);
    }

    [Fact]
    public void BuildTransferDtoFrom_WhenCalled_ReturnsCorrectDto()
    {
        // Arrange
        var mockBeneficiaryRepository = new Mock<IBeneficiaryRepository>();
        var testedService = new BeneficiaryService(mockBeneficiaryRepository.Object);
        var beneficiary = new Beneficiary { Name = DefaultName, IBAN = ValidIban };

        // Act
        var result = beneficiaryService.BuildTransferDtoFrom(beneficiary, DefaultSourceAccountId, DefaultUserId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(DefaultUserId);
        result.SourceAccountId.Should().Be(DefaultSourceAccountId);
        result.RecipientName.Should().Be(DefaultName);
        result.RecipientIBAN.Should().Be(ValidIban);
    }
}
