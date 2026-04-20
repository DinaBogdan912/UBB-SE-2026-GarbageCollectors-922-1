using System;
using BankingAppTeamB.Models;
using BankingAppTeamB.Repositories;

namespace BankingAppTeamB.Services
{
    public class TransactionPipelineService : ITransactionPipelineService
    {
        private const int ExpectedCurrencyCodeLength = 3;
        private const int TwoFaAmountThreshold = 1000;

        private readonly ITransactionRepository transactionRepository;
        private readonly IAccountService accountService;

        public TransactionPipelineService(ITransactionRepository transactionRepository, IAccountService accountService)
        {
            this.transactionRepository = transactionRepository;
            this.accountService = accountService;
        }

        public ValidationResult Validate(PipelineContext ctx)
        {
            if (ctx.Amount <= 0)
            {
                return ValidationResult.Failure("Amount must be greater than zero.");
            }

            if (ctx.Currency == null || ctx.Currency.Length != ExpectedCurrencyCodeLength)
            {
                return ValidationResult.Failure("Currency code must be exactly 3 characters.");
            }

            if (!accountService.IsAccountValid(ctx.SourceAccountId))
            {
                return ValidationResult.Failure("Source account is invalid or does not exist.");
            }

            return ValidationResult.Success();
        }

        public AuthResult Authorize(PipelineContext ctx, string? twoFAToken = null)
        {
            if (ctx.Amount >= TwoFaAmountThreshold && string.IsNullOrWhiteSpace(twoFAToken))
            {
                return AuthResult.Failure("A 2FA token is required for transfers of 1000 or more.");
            }

            return AuthResult.Success();
        }
        public IAccountService GetAccountService()
        {
            return accountService;
        }

        public ExecutionResult Execute(PipelineContext ctx)
        {
            try
            {
                decimal totalDebit = ctx.Amount + ctx.Fee;
                accountService.DebitAccount(ctx.SourceAccountId, totalDebit);
                return ExecutionResult.Success();
            }
            catch (Exception ex)
            {
                return ExecutionResult.Failure($"Debit failed: {ex.Message}");
            }
        }

        public Transaction LogTransaction(Transaction transaction)
        {
            transactionRepository.Add(transaction);
            return transaction;
        }

        public Transaction RunPipeline(PipelineContext ctx, string? twoFAToken = null)
        {
            var validation = Validate(ctx);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.Message);
            }

            var auth = Authorize(ctx, twoFAToken);
            if (!auth.IsAuthorized)
            {
                throw new InvalidOperationException(auth.Message);
            }

            var execution = Execute(ctx);
            if (!execution.IsSuccess)
            {
                throw new InvalidOperationException(execution.Message);
            }

            return LogTransaction(BuildTransaction(ctx));
        }

        private Transaction BuildTransaction(PipelineContext ctx)
        {
            return new Transaction
            {
                AccountId = ctx.SourceAccountId,
                Type = ctx.Type,
                Direction = "Debit",
                Amount = ctx.Amount,
                Currency = ctx.Currency,
                BalanceAfter = accountService.GetBalance(ctx.SourceAccountId),
                CounterpartyName = ctx.CounterpartyName,
                Fee = ctx.Fee,
                Status = "Completed",
                RelatedEntityType = ctx.RelatedEntityType,
                RelatedEntityId = ctx.RelatedEntityId,
                CreatedAt = DateTime.Now
            };
        }
    }
}
