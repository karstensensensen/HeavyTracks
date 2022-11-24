using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HeavyTracks.Commands
{
    class RealyCmd<T> : ICommand
    {

        public event EventHandler? CanExecuteChanged;

        public RealyCmd(Action<T?> executed, Predicate<T?>? can_execute = null)
        {
            m_executed = executed;
            m_can_execute = can_execute;
        }


        public bool CanExecute(object? parameter) => m_can_execute?.Invoke((T?)parameter) ?? true;

        public void Execute(object? parameter)
        {
            m_executed((T?)parameter);

            CanExecuteChanged?.Invoke(this, new());
        }

        protected Predicate<T?>? m_can_execute;
        protected Action<T?> m_executed;
    }
}
