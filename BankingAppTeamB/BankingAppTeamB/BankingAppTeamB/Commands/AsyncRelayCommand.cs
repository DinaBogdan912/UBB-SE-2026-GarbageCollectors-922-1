using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BankingAppTeamB.Commands
{
    /// <summary>An <see cref="ICommand"/> that wraps an async delegate and prevents re-entrant execution while a previous invocation is still running.</summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> executeAsyncAction;
        private bool isExecutionInProgress;

        public event EventHandler? CanExecuteChanged;

        /// <summary>Initialises the command with the async delegate to invoke on execution.</summary>
        public AsyncRelayCommand(Func<object?, Task> executeAsyncAction)
        {
            this.executeAsyncAction = executeAsyncAction;
        }

        /// <summary>Returns <see langword="false"/> while a previous execution is still in progress, preventing re-entrant calls.</summary>
        public bool CanExecute(object? parameter)
        {
            return !isExecutionInProgress;
        }

        /// <summary>Fires and forgets <see cref="ExecuteAsync"/>; required by <see cref="ICommand"/>.</summary>
        public void Execute(object? parameter)
        {
           var executeAsync = ExecuteAsync(parameter);
        }

        /// <summary>Runs the wrapped async delegate, disabling the command for the duration and re-enabling it when finished.</summary>
        public async Task ExecuteAsync(object? parameter)
        {
            isExecutionInProgress = true;
            RaiseCanExecuteChanged();

            try
            {
                await executeAsyncAction(parameter);
            }
            finally
            {
                isExecutionInProgress = false;
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>Raises <see cref="CanExecuteChanged"/> to prompt the UI to re-query <see cref="CanExecute"/>.</summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
