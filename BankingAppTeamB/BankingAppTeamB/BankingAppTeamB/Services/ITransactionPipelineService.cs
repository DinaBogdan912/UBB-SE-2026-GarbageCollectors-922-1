using BankingAppTeamB.Models;

namespace BankingAppTeamB.Services
{
    public interface ITransactionPipelineService
    {
        AuthResult Authorize(PipelineContext context, string? twoFAToken = null);
        ExecutionResult Execute(PipelineContext context);
        IAccountService GetAccountService();
        Transaction LogTransaction(Transaction transaction);
        Transaction RunPipeline(PipelineContext context, string? twoFAToken = null);
        ValidationResult Validate(PipelineContext context);
    }
}