namespace BankingAppTeamB.Models
{
    /// <summary>Outcome of the validation step in the transaction pipeline.</summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }

        /// <summary>Creates a successful validation result with no message.</summary>
        public static ValidationResult Success() =>
            new ValidationResult { IsValid = true, Message = string.Empty };

        /// <summary>Creates a failed validation result with a human-readable error message.</summary>
        public static ValidationResult Failure(string message) =>
            new ValidationResult { IsValid = false, Message = message };
    }

    /// <summary>Outcome of the authorisation step in the transaction pipeline (e.g. 2FA check).</summary>
    public class AuthResult
    {
        public bool IsAuthorized { get; set; }
        public string Message { get; set; }

        /// <summary>Creates a successful authorisation result with no message.</summary>
        public static AuthResult Success() =>
            new AuthResult { IsAuthorized = true, Message = string.Empty };

        /// <summary>Creates a failed authorisation result with a human-readable error message.</summary>
        public static AuthResult Failure(string message) =>
            new AuthResult { IsAuthorized = false, Message = message };
    }

    /// <summary>Outcome of the execution step in the transaction pipeline (account debit).</summary>
    public class ExecutionResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }

        /// <summary>Creates a successful execution result with no message.</summary>
        public static ExecutionResult Success() =>
            new ExecutionResult { IsSuccess = true, Message = string.Empty };

        /// <summary>Creates a failed execution result with a human-readable error message.</summary>
        public static ExecutionResult Failure(string message) =>
            new ExecutionResult { IsSuccess = false, Message = message };
    }
}
