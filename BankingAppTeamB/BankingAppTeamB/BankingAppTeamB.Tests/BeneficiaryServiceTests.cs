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
	private const string EmptyName = "";

	[Fact]
	public void GetByUser_WhenCalled_ReturnsBeneficiaries()
	{
		// Arrange
		var mockRepository = new Mock<IBeneficiaryRepository>();
		var beneficiaryService = new BeneficiaryService(mockRepository.Object);
		var expectedBeneficiaries = new List<Beneficiary>
		{
			new Beneficiary { Id = DefaultBeneficiaryId, UserId = DefaultUserId, Name = DefaultName, IBAN = ValidIban }
		};

		mockRepository.Setup(mockRepositoryInstance => mockRepositoryInstance.GetByUserId(DefaultUserId)).Returns(expectedBeneficiaries);

		// Act
		var beneficiariesForUser = beneficiaryService.GetByUser(DefaultUserId);

		// Assert
		beneficiariesForUser.Should().BeEquivalentTo(expectedBeneficiaries);
		mockRepository.Verify(mockRepositoryInstance => mockRepositoryInstance.GetByUserId(DefaultUserId), Times.Once);
	}

	[Fact]
	public void ValidateIBAN_WhenIbanIsValid_ReturnsTrue()
	{
		// Arrange
		var mockRepository = new Mock<IBeneficiaryRepository>();
		var beneficiaryService = new BeneficiaryService(mockRepository.Object);

		// Act
		var isIbanValid = beneficiaryService.ValidateIBAN(ValidIban);

		// Assert
		isIbanValid.Should().BeTrue();
	}

	[Fact]
	public void ValidateIBAN_WhenIbanIsInvalid_ReturnsFalse()
	{
		// Arrange
		var mockRepository = new Mock<IBeneficiaryRepository>();
		var beneficiaryService = new BeneficiaryService(mockRepository.Object);

		// Act
		var isIbanValid = beneficiaryService.ValidateIBAN(InvalidIban);

		// Assert
		isIbanValid.Should().BeFalse();
	}

	[Fact]
	public void Add_WhenIbanIsInvalid_ThrowsArgumentException()
	{
		// Arrange
		var mockRepository = new Mock<IBeneficiaryRepository>();
		var beneficiaryService = new BeneficiaryService(mockRepository.Object);

		// Act
		Action attemptAddBeneficiaryOperation = () => beneficiaryService.Add(DefaultName, InvalidIban, DefaultUserId);

		// Assert
		attemptAddBeneficiaryOperation.Should().Throw<ArgumentException>().WithMessage("Invalid IBAN format.");
		mockRepository.Verify(mockRepositoryInstance => mockRepositoryInstance.Add(It.IsAny<Beneficiary>()), Times.Never);
	}

	[Fact]
	public void Add_WhenNameIsEmpty_ThrowsArgumentException()
	{
		// Arrange
		var mockRepository = new Mock<IBeneficiaryRepository>();
		var beneficiaryService = new BeneficiaryService(mockRepository.Object);

		// Act
		Action attemptAddBeneficiaryOperation = () => beneficiaryService.Add(EmptyName, ValidIban, DefaultUserId);

		// Assert
		attemptAddBeneficiaryOperation.Should().Throw<ArgumentException>().WithMessage("Name cannot be empty");
		mockRepository.Verify(mockRepositoryInstance => mockRepositoryInstance.Add(It.IsAny<Beneficiary>()), Times.Never);
	}

	[Fact]
	public void Add_WhenIbanAlreadyExists_ThrowsArgumentException()
	{
		// Arrange
		var mockRepository = new Mock<IBeneficiaryRepository>();
		var beneficiaryService = new BeneficiaryService(mockRepository.Object);
		var existingBeneficiaries = new List<Beneficiary>
		{
			new Beneficiary { IBAN = ValidIban }
		};

		mockRepository.Setup(mockRepositoryInstance => mockRepositoryInstance.GetByUserId(DefaultUserId)).Returns(existingBeneficiaries);

		// Act
		Action attemptAddBeneficiaryOperation = () => beneficiaryService.Add(DefaultName, ValidIban, DefaultUserId);

		// Assert
		attemptAddBeneficiaryOperation.Should().Throw<ArgumentException>().WithMessage("A beneficiary with this IBAN already exists for this user.");
		mockRepository.Verify(mockRepositoryInstance => mockRepositoryInstance.Add(It.IsAny<Beneficiary>()), Times.Never);
	}

	[Fact]
	public void Add_WhenDataIsValid_SavesAndReturnsBeneficiary()
	{
		// Arrange
		var mockRepository = new Mock<IBeneficiaryRepository>();
		var beneficiaryService = new BeneficiaryService(mockRepository.Object);

		mockRepository.Setup(mockRepositoryInstance => mockRepositoryInstance.GetByUserId(DefaultUserId)).Returns(new List<Beneficiary>());
		mockRepository.Setup(mockRepositoryInstance => mockRepositoryInstance.Add(It.IsAny<Beneficiary>()));

		var expectedToleranceForCreationTime = TimeSpan.FromSeconds(2);

		// Act
		var createdBeneficiary = beneficiaryService.Add(DefaultName, ValidIban, DefaultUserId);

		// Assert
		createdBeneficiary.Should().NotBeNull();
		createdBeneficiary.UserId.Should().Be(DefaultUserId);
		createdBeneficiary.Name.Should().Be(DefaultName);
		createdBeneficiary.IBAN.Should().Be(ValidIban);
		createdBeneficiary.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, expectedToleranceForCreationTime);
		mockRepository.Verify(mockRepositoryInstance => mockRepositoryInstance.Add(It.IsAny<Beneficiary>()), Times.Once);
	}

	[Fact]
	public void Update_WhenNameIsEmpty_ThrowsArgumentException()
	{
		// Arrange
		var mockRepository = new Mock<IBeneficiaryRepository>();
		var beneficiaryService = new BeneficiaryService(mockRepository.Object);
		var beneficiaryToUpdate = new Beneficiary { Name = EmptyName };

		// Act
		Action attemptUpdateBeneficiaryOperation = () => beneficiaryService.Update(beneficiaryToUpdate);

		// Assert
		attemptUpdateBeneficiaryOperation.Should().Throw<ArgumentException>().WithMessage("Beneficiary name cannot be empty.");
		mockRepository.Verify(mockRepositoryInstance => mockRepositoryInstance.Update(It.IsAny<Beneficiary>()), Times.Never);
	}

	[Fact]
	public void Update_WhenDataIsValid_UpdatesBeneficiary()
	{
		// Arrange
		var mockRepository = new Mock<IBeneficiaryRepository>();
		var beneficiaryService = new BeneficiaryService(mockRepository.Object);
		var beneficiaryToUpdate = new Beneficiary { Name = DefaultName };

		// Act
		beneficiaryService.Update(beneficiaryToUpdate);

		// Assert
		mockRepository.Verify(mockRepositoryInstance => mockRepositoryInstance.Update(beneficiaryToUpdate), Times.Once);
	}

	[Fact]
	public void Delete_WhenCalled_DeletesBeneficiary()
	{
		// Arrange
		var mockRepository = new Mock<IBeneficiaryRepository>();
		var beneficiaryService = new BeneficiaryService(mockRepository.Object);

		// Act
		beneficiaryService.Delete(DefaultBeneficiaryId);

		// Assert
		mockRepository.Verify(mockRepositoryInstance => mockRepositoryInstance.Delete(DefaultBeneficiaryId), Times.Once);
	}

	[Fact]
	public void BuildTransferDtoFrom_WhenCalled_ReturnsCorrectDto()
	{
		// Arrange
		var mockRepository = new Mock<IBeneficiaryRepository>();
		var beneficiaryService = new BeneficiaryService(mockRepository.Object);
		var beneficiary = new Beneficiary { Name = DefaultName, IBAN = ValidIban };

		// Act
		var createdTransferRequest = beneficiaryService.BuildTransferDtoFrom(beneficiary, DefaultSourceAccountId, DefaultUserId);

		// Assert
		createdTransferRequest.Should().NotBeNull();
		createdTransferRequest.UserId.Should().Be(DefaultUserId);
		createdTransferRequest.SourceAccountId.Should().Be(DefaultSourceAccountId);
		createdTransferRequest.RecipientName.Should().Be(DefaultName);
		createdTransferRequest.RecipientIBAN.Should().Be(ValidIban);
	}
}
