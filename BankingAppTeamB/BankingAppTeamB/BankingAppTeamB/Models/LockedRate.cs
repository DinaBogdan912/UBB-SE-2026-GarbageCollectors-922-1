using System;

namespace BankingAppTeamB.Models
{
    public class LockedRate
    {
        private const int LockDurationSeconds = 30;

        public int UserId { get; set; }
        public string CurrencyPair { get; set; }
        public decimal Rate { get; set; }
        public DateTime LockedAt { get; set; }

        public bool IsLockExpired()
        {
            var elapsedSeconds = (DateTime.Now - LockedAt).TotalSeconds;
            return elapsedSeconds > LockDurationSeconds;
        }

        public int GetSecondsRemaining()
        {
            var elapsedSeconds = (DateTime.Now - LockedAt).TotalSeconds;
            int remainingSeconds = LockDurationSeconds - (int)Math.Floor(elapsedSeconds);
            return Math.Max(remainingSeconds, 0);
        }

        public bool IsExpired() => IsLockExpired();

        public int SecondsRemaining() => GetSecondsRemaining();
    }
}
