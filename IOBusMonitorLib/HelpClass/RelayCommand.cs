using System;
using System.Windows.Input;

namespace IOBusMonitorLib
{
    /// <summary>
    /// Simple implementation of <see cref="ICommand"/> that relays its
    /// <see cref="Execute"/> and <see cref="CanExecute"/> logic to delegates.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// Initializes a new instance of <see cref="RelayCommand"/>.
        /// </summary>
        /// <param name="execute">Action invoked when the command is executed.</param>
        /// <param name="canExecute">
        /// Optional predicate that determines whether the command can execute.
        /// If <c>null</c>, the command is always executable.
        /// </param>
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));

            _execute = execute;
            _canExecute = canExecute;
        }

        /// <inheritdoc/>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        /// <inheritdoc/>
        public void Execute(object parameter)
        {
            _execute();
        }

        /// <summary>
        /// Raised by WPF’s <see cref="CommandManager"/> when re-querying of
        /// the command’s ability to execute is required.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
