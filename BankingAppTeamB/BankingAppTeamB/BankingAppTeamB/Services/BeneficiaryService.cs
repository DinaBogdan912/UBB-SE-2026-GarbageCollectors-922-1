using System;
using System.Collections.Generic;
using System.Linq;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Repositories;

namespace BankingAppTeamB.Services
{
    public class BeneficiaryService : IBeneficiaryService
    {
        private readonly IBeneficiaryRepository beneficiaryRepository;

        public BeneficiaryService(IBeneficiaryRepository inputRepository)
        {
            beneficiaryRepository = inputRepository;
        }

        /// <summary>Returns all beneficiaries registered for <paramref name="userId"/>.</summary>
        public List<Beneficiary> GetByUser(int userId)
        {
            return beneficiaryRepository.GetByUserId(userId);
        }

        /// <summary>Returns <see langword="true"/> when <paramref name="iban"/> passes the structural IBAN validation rules.</summary>
        public bool ValidateIBAN(string iban)
        {
            return IbanValidator.Validate(iban);
        }

        /// <summary>Validates the IBAN and name, checks for duplicates, then creates and persists a new beneficiary for <paramref name="userId"/>.</summary>
        public Beneficiary Add(string name, string iban, int userId)
        {
            if (!ValidateIBAN(iban))
            {
                throw new ArgumentException("Invalid IBAN format.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be empty");
            }

            List<Beneficiary> existingBeneficiaries = beneficiaryRepository.GetByUserId(userId);
            if (existingBeneficiaries.Any(beneficiary => beneficiary.IBAN.Equals(iban, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("A beneficiary with this IBAN already exists for this user.");
            }

            Beneficiary newBeneficiary = new Beneficiary
            {
                UserId = userId,
                Name = name,
                IBAN = iban,
                CreatedAt = DateTime.UtcNow
            };

            beneficiaryRepository.Add(newBeneficiary);
            return newBeneficiary;
        }

        /// <summary>Validates that the beneficiary name is not empty, then persists the updated beneficiary.</summary>
        public void Update(Beneficiary beneficiary)
        {
            if (string.IsNullOrWhiteSpace(beneficiary.Name))
            {
                throw new ArgumentException("Beneficiary name cannot be empty.");
            }

            beneficiaryRepository.Update(beneficiary);
        }

        /// <summary>Permanently removes the beneficiary identified by <paramref name="beneficiaryId"/>.</summary>
        public void Delete(int beneficiaryId)
        {
            beneficiaryRepository.Delete(beneficiaryId);
        }

        /// <summary>Creates a pre-populated <see cref="TransferDto"/> from the beneficiary's name and IBAN, ready to pass to the transfer flow.</summary>
        public TransferDto BuildTransferDtoFrom(Beneficiary beneficiary, int sourceAccountId, int userId)
        {
            return new TransferDto
            {
                UserId = userId,
                SourceAccountId = sourceAccountId,
                RecipientName = beneficiary.Name,
                RecipientIBAN = beneficiary.IBAN
            };
        }

        /// <summary>Overload used internally (e.g. from tests) to add a beneficiary with an explicit bank name.</summary>
        internal object Add(string name, string iban, string bankName, int userId)
        {
            if (!ValidateIBAN(iban))
            {
                throw new ArgumentException("Invalid IBAN format.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be empty");
            }

            List<Beneficiary> existingBeneficiaries = beneficiaryRepository.GetByUserId(userId);
            if (existingBeneficiaries.Any(beneficiary => beneficiary.IBAN.Equals(iban, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException("A beneficiary with this IBAN already exists for this user.");
            }

            Beneficiary newBeneficiary = new Beneficiary
            {
                UserId = userId,
                Name = name,
                IBAN = iban,
                BankName = bankName,
                CreatedAt = DateTime.UtcNow
            };

            beneficiaryRepository.Add(newBeneficiary);
            return newBeneficiary;
        }
    }
}
