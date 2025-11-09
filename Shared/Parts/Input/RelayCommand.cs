using System;
using System.Windows.Input;

namespace Formula81.XrmToolBox.Shared.Parts.Input
{
    public class RelayCommand : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        private readonly Action<object> executeAction;
        private readonly Func<object, bool> canExecuteFunc;

        public RelayCommand(Action<object> executeAction, Func<object, bool> canExecuteFunc)
        {
            this.executeAction = executeAction;
            this.canExecuteFunc = canExecuteFunc;
        }

        public RelayCommand(Action<object> methodToExecute)
            : this(methodToExecute, null)
        {
        }

        public bool CanExecute(object parameter)
        {
            return canExecuteFunc?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            executeAction?.Invoke(parameter);
        }
    }
}
