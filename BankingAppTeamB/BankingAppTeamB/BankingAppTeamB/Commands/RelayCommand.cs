using System;
using System.Windows.Input;

namespace BankingAppTeamB.Commands
{
    /// <summary>A synchronous <see cref="ICommand"/> implementation that delegates execution and can-execute evaluation to caller-supplied functions.</summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> executeAction;
        private readonly Func<object?, bool>? canExecutePredicate;

        public event EventHandler? CanExecuteChanged;

        /// <summary>Initialises the command with a mandatory execute delegate and an optional can-execute predicate.</summary>
        public RelayCommand(Action<object?> executeAction, Func<object?, bool>? canExecutePredicate = null)
        {
            this.executeAction = executeAction;
            this.canExecutePredicate = canExecutePredicate;
        }

        /// <summary>Returns <see langword="true"/> when no predicate was provided, or when the predicate returns <see langword="true"/> for <paramref name="parameter"/>.</summary>
        public bool CanExecute(object? parameter)
        {
            return canExecutePredicate == null || canExecutePredicate(parameter);
        }

        /// <summary>Invokes the wrapped execute delegate with <paramref name="parameter"/>.</summary>
        public void Execute(object? parameter)
        {
            executeAction(parameter);
        }

        /// <summary>Raises <see cref="CanExecuteChanged"/> to prompt the UI to re-query <see cref="CanExecute"/>.</summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
