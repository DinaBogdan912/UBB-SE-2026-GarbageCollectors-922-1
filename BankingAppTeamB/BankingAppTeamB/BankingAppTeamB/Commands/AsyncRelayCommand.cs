using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BankingAppTeamB.Commands
{
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> executeAsyncAction;
        private bool isExecutionInProgress;

        public event EventHandler? CanExecuteChanged;

        public AsyncRelayCommand(Func<object?, Task> executeAsyncAction)
        {
            this.executeAsyncAction = executeAsyncAction;
        }

        public bool CanExecute(object? parameter)
        {
            return !isExecutionInProgress;
        }

        public void Execute(object? parameter)
        {
           var executeAsync = ExecuteAsync(parameter);
        }

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

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
