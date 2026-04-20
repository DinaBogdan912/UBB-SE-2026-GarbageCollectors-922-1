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
        var mockRepository = new Mock<IBeneficiaryRepository>();
        var service = new BeneficiaryService(mockRepository.Object);
        var expectedBeneficiaries = new List<Beneficiary>
        {
            new Beneficiary { Id = DefaultBeneficiaryId, UserId = DefaultUserId, Name = DefaultName, IBAN = ValidIban }
        };

        mockRepository.Setup(r => r.GetByUserId(DefaultUserId)).Returns(expectedBeneficiaries);

        // Act
        var result = service.GetByUser(DefaultUserId);

        // Assert
        result.Should().BeEquivalentTo(expectedBeneficiaries);
        mockRepository.Verify(r => r.GetByUserId(DefaultUserId), Times.Once);
    }

    [Fact]
    public void ValidateIBAN_WhenIbanIsValid_ReturnsTrue()
    {
        // Arrange
        var mockRepository = new Mock<IBeneficiaryRepository>();
        var service = new BeneficiaryService(mockRepository.Object);

        // Act
        var result = service.ValidateIBAN(ValidIban);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateIBAN_WhenIbanIsInvalid_ReturnsFalse()
    {
        // Arrange
        var mockRepository = new Mock<IBeneficiaryRepository>();
        var service = new BeneficiaryService(mockRepository.Object);

        // Act
        var result = service.ValidateIBAN(InvalidIban);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Add_WhenIbanIsInvalid_ThrowsArgumentException()
    {
        // Arrange
        var mockRepository = new Mock<IBeneficiaryRepository>();
        var service = new BeneficiaryService(mockRepository.Object);

        // Act
        Action addAction = () => service.Add(DefaultName, InvalidIban, DefaultUserId);

        // Assert
        addAction.Should().Throw<ArgumentException>().WithMessage("Invalid IBAN format.");
        mockRepository.Verify(r => r.Add(It.IsAny<Beneficiary>()), Times.Never);
    }

    [Fact]
    public void Add_WhenNameIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var mockRepository = new Mock<IBeneficiaryRepository>();
        var service = new BeneficiaryService(mockRepository.Object);

        // Act
        Action addAction = () => service.Add(EmptyName, ValidIban, DefaultUserId);

        // Assert
        addAction.Should().Throw<ArgumentException>().WithMessage("Name cannot be empty");
        mockRepository.Verify(r => r.Add(It.IsAny<Beneficiary>()), Times.Never);
    }

    [Fact]
    public void Add_WhenIbanAlreadyExists_ThrowsArgumentException()
    {
        // Arrange
        var mockRepository = new Mock<IBeneficiaryRepository>();
        var service = new BeneficiaryService(mockRepository.Object);
        var existingBeneficiaries = new List<Beneficiary>
        {
            new Beneficiary { IBAN = ValidIban }
        };

        mockRepository.Setup(r => r.GetByUserId(DefaultUserId)).Returns(existingBeneficiaries);

        // Act
        Action addAction = () => service.Add(DefaultName, ValidIban, DefaultUserId);

        // Assert
        addAction.Should().Throw<ArgumentException>().WithMessage("A beneficiary with this IBAN already exists for this user.");
        mockRepository.Verify(r => r.Add(It.IsAny<Beneficiary>()), Times.Never);
    }

    [Fact]
    public void Add_WhenDataIsValid_SavesAndReturnsBeneficiary()
    {
        // Arrange
        var mockRepository = new Mock<IBeneficiaryRepository>();
        var service = new BeneficiaryService(mockRepository.Object);

        mockRepository.Setup(r => r.GetByUserId(DefaultUserId)).Returns(new List<Beneficiary>());
        mockRepository.Setup(r => r.Add(It.IsAny<Beneficiary>()));

        var expectedToleranceForCreationTime = TimeSpan.FromSeconds(2);

        // Act
        var result = service.Add(DefaultName, ValidIban, DefaultUserId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(DefaultUserId);
        result.Name.Should().Be(DefaultName);
        result.IBAN.Should().Be(ValidIban);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, expectedToleranceForCreationTime);
        mockRepository.Verify(r => r.Add(It.IsAny<Beneficiary>()), Times.Once);
    }

    [Fact]
    public void Update_WhenNameIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var mockRepository = new Mock<IBeneficiaryRepository>();
        var service = new BeneficiaryService(mockRepository.Object);
        var beneficiary = new Beneficiary { Name = EmptyName };

        // Act
        Action updateAction = () => service.Update(beneficiary);

        // Assert
        updateAction.Should().Throw<ArgumentException>().WithMessage("Beneficiary name cannot be empty.");
        mockRepository.Verify(r => r.Update(It.IsAny<Beneficiary>()), Times.Never);
    }

    [Fact]
    public void Update_WhenDataIsValid_UpdatesBeneficiary()
    {
        // Arrange
        var mockRepository = new Mock<IBeneficiaryRepository>();
        var service = new BeneficiaryService(mockRepository.Object);
        var beneficiary = new Beneficiary { Name = DefaultName };

        // Act
        service.Update(beneficiary);

        // Assert
        mockRepository.Verify(r => r.Update(beneficiary), Times.Once);
    }

    [Fact]
    public void Delete_WhenCalled_DeletesBeneficiary()
    {
        // Arrange
        var mockRepository = new Mock<IBeneficiaryRepository>();
        var service = new BeneficiaryService(mockRepository.Object);

        // Act
        service.Delete(DefaultBeneficiaryId);

        // Assert
        mockRepository.Verify(r => r.Delete(DefaultBeneficiaryId), Times.Once);
    }

    [Fact]
    public void BuildTransferDtoFrom_WhenCalled_ReturnsCorrectDto()
    {
        // Arrange
        var mockRepository = new Mock<IBeneficiaryRepository>();
        var service = new BeneficiaryService(mockRepository.Object);
        var beneficiary = new Beneficiary { Name = DefaultName, IBAN = ValidIban };

        // Act
        var result = service.BuildTransferDtoFrom(beneficiary, DefaultSourceAccountId, DefaultUserId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(DefaultUserId);
        result.SourceAccountId.Should().Be(DefaultSourceAccountId);
        result.RecipientName.Should().Be(DefaultName);
        result.RecipientIBAN.Should().Be(ValidIban);
    }
}
