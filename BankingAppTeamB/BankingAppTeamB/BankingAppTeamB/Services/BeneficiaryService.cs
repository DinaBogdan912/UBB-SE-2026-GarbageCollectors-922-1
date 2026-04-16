using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;
using BankingAppTeamB.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using BankingAppTeamB.Models.DTOs;

namespace BankingAppTeamB.Services
{
    public class BeneficiaryService : IBeneficiaryService
    {
        private const int MinimumIbanLength = 15;
        private const int MaximumIbanLength = 34;

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
            if (string.IsNullOrWhiteSpace(iban)) return false;
            if (iban.Length < MinimumIbanLength || iban.Length > MaximumIbanLength) return false;
            if (!char.IsLetter(iban[0]) || !char.IsLetter(iban[1])) return false;
            if (!char.IsDigit(iban[2]) || !char.IsDigit(iban[3])) return false;
            return true;
        }
        public Beneficiary Add(string name, string iban, int userId)
        {
            if (ValidateIBAN(iban) == false)
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
            Beneficiary beneficiary = new Beneficiary
            {
                UserId = userId,
                Name = name,
                IBAN = iban,
                CreatedAt = DateTime.UtcNow
            };

            beneficiaryRepository.Add(beneficiary);
            return beneficiary;
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

        internal object Add(string name, string iban, string newBankName, int userId)
        {
            if (ValidateIBAN(iban) == false)
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
            Beneficiary beneficiary = new Beneficiary
            {
                UserId = userId,
                Name = name,
                IBAN = iban,
                BankName = newBankName,
                CreatedAt = DateTime.UtcNow
            };

            beneficiaryRepository.Add(beneficiary);
            return beneficiary;
        }
    }
}
