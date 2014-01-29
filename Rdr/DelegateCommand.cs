using System;

namespace Rdr
{
    abstract class Command : System.Windows.Input.ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
        }

        public abstract void Execute(object parameter);
        public abstract bool CanExecute(object parameter);
    }

    class DelegateCommandAsync : Command
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public DelegateCommandAsync(Action<object> execute, Predicate<object> canExecute)
        {
            this._execute = execute;
            this._canExecute = canExecute;
        }

        public override async void Execute(object parameter)
        {
            await System.Threading.Tasks.Task.Factory.StartNew(this._execute, parameter);
        }

        public override bool CanExecute(object parameter)
        {
            if (this._canExecute == null)
            {
                return true;
            }

            return this._canExecute(parameter);
        }
    }

    class DelegateCommand : Command
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;
        
        public DelegateCommand(Action<object> execute, Predicate<object> canExecute)
        {
            this._execute = execute;
            this._canExecute = canExecute;
        }

        public override void Execute(object parameter)
        {
            this._execute(parameter);
        }

        public override bool CanExecute(object parameter)
        {
            if (this._canExecute == null)
            {
                return true;
            }

            return this._canExecute(parameter);
        }
    }
}
