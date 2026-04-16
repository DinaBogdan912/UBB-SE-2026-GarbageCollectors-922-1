using BankingAppTeamB.Models;
using BankingAppTeamB.Repositories;

using System;

namespace BankingAppTeamB.Services
{
    public class TransactionPipelineService : ITransactionPipelineService
    {
        private const int ExpectedCurrencyCodeLength = 3;
        private const decimal TwoFaAmountThreshold = 1000m;

        private readonly ITransactionRepository transactionRepository;
        private readonly IAccountService accountService;
        


        public TransactionPipelineService(ITransactionRepository transactionRepository, IAccountService accountService)
        {
            this.transactionRepository = transactionRepository;
            this.accountService = accountService;
        }

        // public TransactionPipelineService(ITransactionRepository transactionRepository)
        // {
        //     this.transactionRepository = transactionRepository;
        // }

        public ValidationResult Validate(PipelineContext context)
        {
            if (context.Amount <= 0)
                return ValidationResult.Failure("Amount must be greater than zero.");

            if (context.Currency == null || context.Currency.Length != ExpectedCurrencyCodeLength)
                return ValidationResult.Failure("Currency code must be exactly 3 characters.");

            if (!accountService.IsAccountValid(context.SourceAccountId))
                // if (!AccountService.IsAccountValid(context.SourceAccountId))
                return ValidationResult.Failure("Source account is invalid or does not exist.");

            return ValidationResult.Success();
        }

        public AuthResult Authorize(PipelineContext context, string? twoFAToken = null)
        {
            if (context.Amount >= TwoFaAmountThreshold && string.IsNullOrWhiteSpace(twoFAToken))
                return AuthResult.Failure("A 2FA token is required for transfers of 1000 or more.");

            return AuthResult.Success();
        }

        public ExecutionResult Execute(PipelineContext context)
        {
            try
            {
                decimal totalDebit = context.Amount + context.Fee;
                accountService.DebitAccount(context.SourceAccountId, totalDebit);
                // AccountService.DebitAccount(context.SourceAccountId, totalDebit);
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

        public Transaction RunPipeline(PipelineContext context, string? twoFAToken = null)
        {
            var validation = Validate(context);
            if (!validation.IsValid)
                throw new InvalidOperationException(validation.Message);

            var auth = Authorize(context, twoFAToken);
            if (!auth.IsAuthorized)
                throw new InvalidOperationException(auth.Message);

            var execution = Execute(context);
            if (!execution.IsSuccess)
                throw new InvalidOperationException(execution.Message);

            return LogTransaction(BuildTransaction(context));
        }

        private Transaction BuildTransaction(PipelineContext context)
        {
            var createdAt = DateTime.Now;
            return new Transaction
            {
                AccountId = context.SourceAccountId,
                TransactionRef = GenerateTransactionRef(createdAt),
                Type = context.Type,
                Direction = "Debit",
                Amount = context.Amount,
                Currency = context.Currency,
                BalanceAfter = accountService.GetBalance(context.SourceAccountId),
                // BalanceAfter = AccountService.GetBalance(context.SourceAccountId),
                CounterpartyName = context.CounterpartyName,
                Fee = context.Fee,
                Status = "Completed",
                RelatedEntityType = context.RelatedEntityType,
                RelatedEntityId = context.RelatedEntityId,
                CreatedAt = createdAt
            };
        }

        private static string GenerateTransactionRef(DateTime createdAt)
        {
            const int suffixLength = 6;
            string suffix = Guid.NewGuid().ToString("N")[..suffixLength].ToUpper();
            return $"TXN-{createdAt:yyyyMMdd}-{suffix}";
        }


        public IAccountService GetAccountService()
        {
            return accountService;
        }

    }
}
