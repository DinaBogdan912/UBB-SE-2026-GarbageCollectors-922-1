using System;
using System.Windows.Input;

namespace BankingAppTeamB.Commands
{
    /// <summary>A basic command that just calls a function when clicked.</summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> executeAction;
        private readonly Func<object?, bool>? canExecutePredicate;

        public event EventHandler? CanExecuteChanged;

        /// <summary>Takes the function to run and optionally a second function to check if running is allowed.</summary>
        public RelayCommand(Action<object?> executeAction, Func<object?, bool>? canExecutePredicate = null)
        {
            this.executeAction = executeAction;
            this.canExecutePredicate = canExecutePredicate;
        }

        /// <summary>Says yes unless you gave it a rule that says otherwise.</summary>
        public bool CanExecute(object? parameter)
        {
            return canExecutePredicate == null || canExecutePredicate(parameter);
        }

        /// <summary>Calls the function you set up.</summary>
        public void Execute(object? parameter)
        {
            executeAction(parameter);
        }

        /// <summary>Tells the UI to re-check if the button should be clickable.</summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
