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

        public List<Beneficiary> GetByUser(int userId)
        {
            return beneficiaryRepository.GetByUserId(userId);
        }

        public bool ValidateIBAN(string iban)
        {
            return IbanValidator.Validate(iban);
        }

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

        public void Update(Beneficiary beneficiary)
        {
            if (string.IsNullOrWhiteSpace(beneficiary.Name))
            {
                throw new ArgumentException("Beneficiary name cannot be empty.");
            }

            beneficiaryRepository.Update(beneficiary);
        }

        public void Delete(int id)
        {
            beneficiaryRepository.Delete(id);
        }

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
