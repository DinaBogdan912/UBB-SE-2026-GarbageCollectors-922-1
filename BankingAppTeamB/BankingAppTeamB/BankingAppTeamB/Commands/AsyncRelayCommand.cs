using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BankingAppTeamB.Commands
{
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> executeAsyncAction;
        private bool isExecuting;

        public event EventHandler? CanExecuteChanged;

        public AsyncRelayCommand(Func<object?, Task> executeAsyncAction)
        {
            this.executeAsyncAction = executeAsyncAction;
        }

        public bool CanExecute(object? parameter) => !isExecuting;

        public void Execute(object? parameter)
        {
            _ = ExecuteAsync(parameter);
        }

        public async Task ExecuteAsync(object? parameter)
        {
            isExecuting = true;
            RaiseCanExecuteChanged();
            try
            {
                await executeAsyncAction(parameter);
            }
            finally
            {
                isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}