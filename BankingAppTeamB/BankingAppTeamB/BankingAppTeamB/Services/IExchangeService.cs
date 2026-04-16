using System.Collections.Generic;
using BankingAppTeamB.Models;
using BankingAppTeamB.Models.DTOs;

namespace BankingAppTeamB.Services
{
    public interface IExchangeService
    {
        decimal CalculateCommission(decimal amount);
        decimal CalculateTargetAmount(decimal sourceAmount, decimal rate);
        void CheckRateAlerts();
        void ClearLocks(int userId);
        RateAlert CreateAlert(int userId, string source, string target, decimal rate, bool isBuyAlert);
        void DeleteAlert(int id);
        ExchangeTransaction ExecuteExchange(ExchangeDto dto);
        Dictionary<string, decimal> GetLiveRates();
        decimal GetRate(string from, string to);
        List<RateAlert> GetUserAlerts(int userId);
        bool IsRateLockValid(int userId);
        LockedRate LockRate(int userId, string from, string to);
    }
}