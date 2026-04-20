using System;

namespace BankingAppTeamB.Models
{
    public class LockedRate
    {
        private const int LockDurationSeconds = 30;
        private const int MinimumSecondsRemaining = 0;
        public int UserId { get; set; }
        public string CurrencyPair { get; set; }
        public decimal Rate { get; set; }
        public DateTime LockedAt { get; set; }

        /// <summary>Returns true when more than 30 seconds have passed since the rate was locked.</summary>
        public bool IsLockExpired()
        {
            var elapsedSeconds = (DateTime.Now - LockedAt).TotalSeconds;
            return elapsedSeconds > LockDurationSeconds;
        }

        /// <summary>Returns the number of whole seconds remaining in the 30-second lock window, clamped to zero when expired.</summary>
        public int GetSecondsRemaining()
        {
            var elapsedSeconds = (DateTime.Now - LockedAt).TotalSeconds;
            int remainingSeconds = LockDurationSeconds - (int)Math.Floor(elapsedSeconds);
            return Math.Max(remainingSeconds, MinimumSecondsRemaining);
        }

        /// <summary>Alias for IsLockExpired.</summary>
        public bool IsExpired() => IsLockExpired();

        /// <summary>Alias for GetSecondsRemaining.</summary>
        public int SecondsRemaining() => GetSecondsRemaining();
    }
}
