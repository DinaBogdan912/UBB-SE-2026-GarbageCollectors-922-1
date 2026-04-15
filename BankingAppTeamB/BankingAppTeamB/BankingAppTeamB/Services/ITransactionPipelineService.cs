using BankingAppTeamB.Mocks;
using BankingAppTeamB.Models;

namespace BankingAppTeamB.Services
{
    public interface ITransactionPipelineService
    {
        AuthResult Authorize(PipelineContext ctx, string? twoFAToken = null);
        ExecutionResult Execute(PipelineContext ctx);
        AccountService GetAccountService();
        Transaction LogTransaction(Transaction tx);
        Transaction RunPipeline(PipelineContext ctx, string? twoFAToken = null);
        ValidationResult Validate(PipelineContext ctx);
    }
}