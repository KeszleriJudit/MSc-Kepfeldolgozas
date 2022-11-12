using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfApp.WpfUiHelpers
{
    public class RelayCommand : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
                this.CanExecuteChangedInternal += value;
            }

            remove
            {
                CommandManager.RequerySuggested -= value;
                this.CanExecuteChangedInternal -= value;
            }
        }

        Action<object> execute_function;
        Predicate<object> canexecute_function;

        private event EventHandler CanExecuteChangedInternal;

        public RelayCommand(Action<object> execute_function,
            Predicate<object> canexecute_function)
        {
            this.execute_function = execute_function ??
                throw new ArgumentException("Execute function not defined!");

            this.canexecute_function = canexecute_function ??
                throw new ArgumentException("Can execute function not defined!");
        }

        public RelayCommand(Action<object> execute_function)
            : this(execute_function, t => true)
        {
        }

        public bool CanExecute(object parameter)
        {
            return this.canexecute_function != null && this.canexecute_function(parameter);
        }

        public void Execute(object parameter)
        {
            this.execute_function?.Invoke(parameter);
        }

        public void OnCanExecuteChanged()
        {
            EventHandler handler = this.CanExecuteChangedInternal;
            handler?.Invoke(this, EventArgs.Empty);
        }

        public void Destroy()
        {
            this.canexecute_function = t => false;
            this.execute_function = t => { return; };
        }
    }
}
