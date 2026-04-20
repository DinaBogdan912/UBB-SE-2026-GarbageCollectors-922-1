using System;

namespace BankingAppTeamB.Services
{
    internal static class IbanValidator
    {
        private const int MinimumIbanLength = 15;
        private const int MaximumIbanLength = 34;
        private const int CountryCodeFirstLetterIndex = 0;
        private const int CountryCodeSecondLetterIndex = 1;
        private const int CheckDigitFirstIndex = 2;
        private const int CheckDigitSecondIndex = 3;

        /// <summary>Returns true when iban is non-empty, between 15 - 34 characters, starts with two letters, and has two digits in positions 3-4. Does not verify the checksum.</summary>
        public static bool Validate(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban))
            {
                return false;
            }

            if (iban.Length < MinimumIbanLength || iban.Length > MaximumIbanLength)
            {
                return false;
            }

            if (!char.IsLetter(iban[CountryCodeFirstLetterIndex]) || !char.IsLetter(iban[CountryCodeSecondLetterIndex]))
            {
                return false;
            }

            if (!char.IsDigit(iban[CheckDigitFirstIndex]) || !char.IsDigit(iban[CheckDigitSecondIndex]))
            {
                return false;
            }

            return true;
        }
    }
}
