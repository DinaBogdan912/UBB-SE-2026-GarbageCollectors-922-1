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

        /// <summary>Gets everyone saved as a beneficiary for a specific user.</summary>
        public List<Beneficiary> GetByUser(int userId)
        {
            return beneficiaryRepository.GetByUserId(userId);
        }

        /// <summary>Checks if an IBAN has the right format.</summary>
        public bool ValidateIBAN(string iban)
        {
            return IbanValidator.Validate(iban);
        }

        /// <summary>Validates the IBAN and name, makes sure it's not a duplicate, then saves the new beneficiary.</summary>
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

        /// <summary>Makes sure the name isn't empty and saves the changes.</summary>
        public void Update(Beneficiary beneficiary)
        {
            if (string.IsNullOrWhiteSpace(beneficiary.Name))
            {
                throw new ArgumentException("Beneficiary name cannot be empty.");
            }

            beneficiaryRepository.Update(beneficiary);
        }

        /// <summary>Permanently removes a beneficiary — can't undo this.</summary>
        public void Delete(int beneficiaryId)
        {
            beneficiaryRepository.Delete(beneficiaryId);
        }

        /// <summary>Builds a ready-to-use transfer object from a beneficiary's info.</summary>
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

        /// <summary>Same as Add but also lets you set the bank name — mostly used in tests.</summary>
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
