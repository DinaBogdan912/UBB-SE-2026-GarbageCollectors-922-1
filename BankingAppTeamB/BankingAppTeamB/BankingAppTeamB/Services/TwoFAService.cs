using System;
using System.Diagnostics;

namespace BankingAppTeamB.Services
{
    public class TwoFAService : ITwoFAService
    {
        private const int TwoFaAmountThreshold = 1000;
        private const string PlaceholderToken = "123456";

        /// <summary>Returns <see langword="true"/> when <paramref name="amount"/> meets or exceeds the €1 000 threshold that mandates two-factor authentication.</summary>
        public bool Requires2FA(decimal amount)
        {
            return amount >= TwoFaAmountThreshold;
        }

        /// <summary>Returns <see langword="true"/> for any non-blank token (placeholder implementation — does not verify against a real OTP).</summary>
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

        /// <summary>Returns the placeholder 2FA token <c>"123456"</c> for any user (stub — replace with a real OTP generator in production).</summary>
        public string GenerateToken(int userId)
        {
            string placeholder = PlaceholderToken;
            Debug.WriteLine($"[2FA] Placeholder token generated for userId={userId}: {placeholder}");
            return placeholder;
        }
    }
}
