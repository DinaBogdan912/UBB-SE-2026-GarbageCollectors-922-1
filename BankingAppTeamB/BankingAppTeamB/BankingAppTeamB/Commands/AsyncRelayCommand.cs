using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BankingAppTeamB.Commands
{
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> execute;
        private bool isExecuting;

        public event EventHandler? CanExecuteChanged;

        public AsyncRelayCommand(Func<object?, Task> execute)
        {
            this.execute = execute;
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
                await execute(parameter);
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