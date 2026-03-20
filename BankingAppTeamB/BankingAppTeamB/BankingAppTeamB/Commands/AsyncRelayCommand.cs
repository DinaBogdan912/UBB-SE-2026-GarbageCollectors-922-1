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
        private readonly Func<object?, Task> _execute;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged;

        public AsyncRelayCommand(Func<object?, Task> execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object? parameter) => !_isExecuting;

        public void Execute(object? parameter)
        {
            _ = ExecuteAsync(parameter);
        }

        public async Task ExecuteAsync(object? parameter)
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();
            try
            {
                await _execute(parameter);
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}