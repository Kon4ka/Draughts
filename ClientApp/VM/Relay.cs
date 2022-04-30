using System;
using System.Windows.Input;

namespace RelayNS
{
    public sealed class Relay : ICommand
    {
        private Predicate<object> _canExecute;
        private Action<object> _action;

        public Relay(Action<object> execute, Predicate<object> canExecute = null)
        {
            _action = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add =>
                CommandManager.RequerySuggested += value;

            remove =>
                CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            _action(parameter);
        }
    }
}
