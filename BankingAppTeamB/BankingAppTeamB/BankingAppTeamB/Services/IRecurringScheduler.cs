namespace BankingAppTeamB.Services
{
    public interface IRecurringScheduler
    {
        void SetExchangeService(ExchangeService exchangeService);
        void Start();
        void Stop();
    }
}