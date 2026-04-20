using System.Collections.Generic;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Repositories;

public interface IExchangeRepository
{
    public ExchangeTransaction Add(ExchangeTransaction exchangeTransaction);
    public List<ExchangeTransaction> GetByUserId(int userId);
    public List<RateAlert> GetAlertsByUser(int userId, bool? isTriggered = null);
    public List<RateAlert> GetAllAlerts(bool? isTriggered = null);
    public RateAlert AddAlert(RateAlert rateAlert);
    public void DeleteAlert(int rateAlertId);
    public void MarkAlertTriggered(int rateAlertId);
}