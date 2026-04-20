namespace BankingAppTeamB.Services
{
    public interface IAccountService
    {
        void CreditAccount(int accountId, decimal amount);
        void DebitAccount(int accountId, decimal amount);
        decimal GetBalance(int accountId);
        bool IsAccountValid(int accountId);
    }
}