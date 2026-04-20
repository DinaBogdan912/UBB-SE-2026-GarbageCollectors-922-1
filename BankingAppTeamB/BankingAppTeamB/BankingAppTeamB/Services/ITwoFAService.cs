namespace BankingAppTeamB.Services
{
    public interface ITwoFAService
    {
        string GenerateToken(int userId);
        bool Requires2FA(decimal amount);
        bool ValidateToken(string token);
    }
}