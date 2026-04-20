using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BankingAppTeamB.Commands
{
    /// <summary>A command that runs async stuff and won't let you spam-click it while it's already running.</summary>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> executeAsyncAction;
        private bool isExecutionInProgress;

        public event EventHandler? CanExecuteChanged;

        /// <summary>Sets it up with the async thing it's supposed to do.</summary>
        public AsyncRelayCommand(Func<object?, Task> executeAsyncAction)
        {
            this.executeAsyncAction = executeAsyncAction;
        }

        /// <summary>Says no if it's already busy, so you can't start it twice.</summary>
        public bool CanExecute(object? parameter)
        {
            return !isExecutionInProgress;
        }

        /// <summary>Starts the async version without waiting for it to finish.</summary>
        public void Execute(object? parameter)
        {
           var executeAsync = ExecuteAsync(parameter);
        }

        /// <summary>Actually runs the async function and disables itself the whole time so nothing weird happens.</summary>
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

        /// <summary>Tells the UI to check again whether the button should be clickable or not.</summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
