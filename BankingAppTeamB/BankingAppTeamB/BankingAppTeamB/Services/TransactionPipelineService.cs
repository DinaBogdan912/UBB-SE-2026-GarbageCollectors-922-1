using System;
using BankingAppTeamB.Models;
using BankingAppTeamB.Repositories;

namespace BankingAppTeamB.Services
{
    public class TransactionPipelineService : ITransactionPipelineService
    {
        private const int ExpectedCurrencyCodeLength = 3;
        private const int TwoFaAmountThreshold = 1000;
        private const int MinimumAmount = 0;

        private readonly ITransactionRepository transactionRepository;
        private readonly IAccountService accountService;

        public TransactionPipelineService(ITransactionRepository transactionRepository, IAccountService accountService)
        {
            this.transactionRepository = transactionRepository;
            this.accountService = accountService;
        }

        /// <summary>Validates the pipeline context: amount must be positive, currency must be a 3-letter code, and the source account must exist.</summary>
        public ValidationResult Validate(PipelineContext pipelineContext)
        {
            if (pipelineContext.Amount <= MinimumAmount)
            {
                return ValidationResult.Failure("Amount must be greater than zero.");
            }

            if (pipelineContext.Currency == null || pipelineContext.Currency.Length != ExpectedCurrencyCodeLength)
            {
                return ValidationResult.Failure("Currency code must be exactly 3 characters.");
            }

            if (!accountService.IsAccountValid(pipelineContext.SourceAccountId))
            {
                return ValidationResult.Failure("Source account is invalid or does not exist.");
            }

            return ValidationResult.Success();
        }

        /// <summary>Checks that a 2FA token is present when the transfer amount is >= 1 000.</summary>
        public AuthResult Authorize(PipelineContext pipelineContext, string? twoFAToken = null)
        {
            if (pipelineContext.Amount >= TwoFaAmountThreshold && string.IsNullOrWhiteSpace(twoFAToken))
            {
                return AuthResult.Failure("A 2FA token is required for transfers of 1000 or more.");
            }

            return AuthResult.Success();
        }
        /// <summary>Returns the underlying IAccountService for callers that need direct balance operations outside the pipeline.</summary>
        public IAccountService GetAccountService()
        {
            return accountService;
        }

        /// <summary>Debits the source account for amount + fee; returns a failure result instead of throwing if the debit fails.</summary>
        public ExecutionResult Execute(PipelineContext pipelineContext)
        {
            try
            {
                decimal totalDebit = pipelineContext.Amount + pipelineContext.Fee;
                accountService.DebitAccount(pipelineContext.SourceAccountId, totalDebit);
                return ExecutionResult.Success();
            }
            catch (Exception debitException)
            {
                return ExecutionResult.Failure($"Debit failed: {debitException.Message}");
            }
        }

        /// <summary>Persists transaction to the database and returns the same object (with its new Id populated).</summary>
        public Transaction LogTransaction(Transaction transaction)
        {
            transactionRepository.Add(transaction);
            return transaction;
        }

        /// <summary>Runs validate -> authorise -> execute -> log in sequence; throws InvalidOperationException at the first failed step.</summary>
        public Transaction RunPipeline(PipelineContext pipelineContext, string? twoFAToken = null)
        {
            var validationResult = Validate(pipelineContext);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(validationResult.Message);
            }

            var authorizationResult = Authorize(pipelineContext, twoFAToken);
            if (!authorizationResult.IsAuthorized)
            {
                throw new InvalidOperationException(authorizationResult.Message);
            }

            var executionResult = Execute(pipelineContext);
            if (!executionResult.IsSuccess)
            {
                throw new InvalidOperationException(executionResult.Message);
            }

            return LogTransaction(BuildTransaction(pipelineContext));
        }

        /// <summary>Constructs a completed debit Transaction from the pipeline context, capturing the post-debit balance.</summary>
        private Transaction BuildTransaction(PipelineContext pipelineContext)
        {
            return new Transaction
            {
                AccountId = pipelineContext.SourceAccountId,
                Type = pipelineContext.Type,
                Direction = "Debit",
                Amount = pipelineContext.Amount,
                Currency = pipelineContext.Currency,
                BalanceAfter = accountService.GetBalance(pipelineContext.SourceAccountId),
                CounterpartyName = pipelineContext.CounterpartyName,
                Fee = pipelineContext.Fee,
                Status = "Completed",
                RelatedEntityType = pipelineContext.RelatedEntityType,
                RelatedEntityId = pipelineContext.RelatedEntityId,
                CreatedAt = DateTime.Now
            };
        }
    }
}
