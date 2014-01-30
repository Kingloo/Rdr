using System;
using System.Threading.Tasks;

namespace Rdr
{
    abstract class Command : System.Windows.Input.ICommand
    {
        public event EventHandler CanExecuteChanged;

        public abstract void Execute(object parameter);
        public abstract bool CanExecute(object parameter);

        public void RaiseCanExecuteChanged()
        {
            EventHandler handler = this.CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
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

    class DelegateCommandAsync : Command
    {
        private readonly Func<object, Task> _execute;
        private readonly Predicate<object> _canExecute;

        public DelegateCommandAsync(Func<object, Task> execute, Predicate<object> canExecute)
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
