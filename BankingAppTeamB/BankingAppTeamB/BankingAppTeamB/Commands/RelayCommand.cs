using System;
using System.Windows.Input;

namespace BankingAppTeamB.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> executeAction;
        private readonly Func<object?, bool>? canExecutePredicate;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Action<object?> executeAction, Func<object?, bool>? canExecutePredicate = null)
        {
            this.executeAction = executeAction;
            this.canExecutePredicate = canExecutePredicate;
        }

        public bool CanExecute(object? parameter)
            => canExecutePredicate == null || canExecutePredicate(parameter);

        public void Execute(object? parameter)
            => executeAction(parameter);

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}