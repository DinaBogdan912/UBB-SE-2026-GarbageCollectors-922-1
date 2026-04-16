using System;
using System.Diagnostics;

namespace BankingAppTeamB.Services
{
    public class TwoFAService : ITwoFAService
    {
        private const int TwoFaAmountThreshold = 1000;
        private const string PlaceholderToken = "123456";

        public bool Requires2FA(decimal amount)
        {
            return amount >= TwoFaAmountThreshold;
        }

        public bool ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                Debug.WriteLine("[2FA] Validation failed: token was empty.");
                return false;
            }

            Debug.WriteLine($"[2FA] Token accepted (placeholder): '{token}'");
            return true;
        }

        public string GenerateToken(int userId)
        {
            string placeholder = PlaceholderToken;
            Debug.WriteLine($"[2FA] Placeholder token generated for userId={userId}: {placeholder}");
            return placeholder;
        }
    }
}
