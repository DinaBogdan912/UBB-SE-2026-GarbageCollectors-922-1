using System.Collections.Generic;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Repositories;

public interface IExchangeRepository
{
    public ExchangeTransaction Add(ExchangeTransaction transaction);
    public List<ExchangeTransaction> GetByUserId(int userId);
    public List<RateAlert> GetAlertsByUser(int userId, bool? isTriggered = null);
    public List<RateAlert> GetAllAlerts(bool? isTriggered = null);
    public RateAlert AddAlert(RateAlert alert);
    public void DeleteAlert(int id);
    public void MarkAlertTriggered(int id);
    
}