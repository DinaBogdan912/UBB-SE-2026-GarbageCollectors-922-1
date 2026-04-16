namespace BankingAppTeamB.Services
{
    public interface IAccountService
    {
        void CreditAccount(int accountId, decimal amount);
        void DebitAccount(int accountId, decimal amount);
        decimal GetBalance(int id);
        bool IsAccountValid(int id);
    }
}