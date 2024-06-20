using System;
using System.Reflection;
using System.Windows.Input;

namespace WebViewBrowserPanel.Utils
{
    public sealed class RelayCommand : ICommand
    {
        private sealed class WeakAction
        {
            private Action _staticAction;

            public WeakAction(Action action, bool keepTargetAlive = false) : this(action?.Target, action,
                keepTargetAlive)
            {
            }

            public WeakAction(object target, Action action, bool keepTargetAlive = false)
            {
                if (action.Method.IsStatic)
                {
                    _staticAction = action;
                    if (target != null)
                        Reference = new WeakReference(target);
                }
                else
                {
                    Method = action.Method;
                    ActionReference = new WeakReference(action.Target);
                    LiveReference = keepTargetAlive ? action.Target : null;
                    Reference = new WeakReference(target);
                }
            }

            public string MethodName => _staticAction == null ? Method.Name : _staticAction.Method.Name;
            public object Target => Reference?.Target;

            public bool IsStatic => _staticAction != null;

            public bool IsAlive
            {
                get
                {
                    if (_staticAction == null && Reference == null && LiveReference == null)
                        return false;

                    if (_staticAction != null)
                        return Reference == null || Reference.IsAlive;

                    if (LiveReference != null)
                        return true;

                    return Reference != null && Reference.IsAlive;
                }
            }

            private MethodInfo Method { get; set; }
            private WeakReference ActionReference { get; set; }
            private object LiveReference { get; set; }
            private WeakReference Reference { get; set; }
            private object ActionTarget => LiveReference ?? (ActionReference?.Target);

            public void Execute()
            {
                if (_staticAction != null)
                {
                    _staticAction();
                    return;
                }

                object actionTarget = ActionTarget;
                if (IsAlive && Method != null && (LiveReference != null || ActionReference != null) &&
                    actionTarget != null)
                    _ = Method.Invoke(actionTarget, null);
            }

            public void MarkForDeletion()
            {
                Reference = null;
                ActionReference = null;
                LiveReference = null;
                Method = null;
                _staticAction = null;
            }
        }

        private sealed class WeakFunc<TResult>
        {
            private Func<TResult> _staticFunc;

            public bool IsStatic => _staticFunc != null;
            public string MethodName => _staticFunc == null ? Method.Name : _staticFunc.Method.Name;
            public object Target => Reference?.Target;

            public bool IsAlive
            {
                get
                {
                    if (_staticFunc == null && Reference == null && LiveReference == null)
                        return false;

                    if (_staticFunc != null)
                        return Reference == null || Reference.IsAlive;

                    if (LiveReference != null)
                        return true;

                    return Reference != null && Reference.IsAlive;
                }
            }

            private MethodInfo Method { get; set; }
            private WeakReference FuncReference { get; set; }
            private object LiveReference { get; set; }
            private WeakReference Reference { get; set; }
            private object FuncTarget => LiveReference ?? (FuncReference?.Target);

            public WeakFunc(Func<TResult> func, bool keepTargetAlive = false) : this(func?.Target, func,
                keepTargetAlive)
            {
            }

            public WeakFunc(object target, Func<TResult> func, bool keepTargetAlive = false)
            {
                if (func.Method.IsStatic)
                {
                    _staticFunc = func;
                    if (target != null)
                        Reference = new WeakReference(target);
                }
                else
                {
                    Method = func.Method;
                    FuncReference = new WeakReference(func.Target);
                    LiveReference = keepTargetAlive ? func.Target : null;
                    Reference = new WeakReference(target);
                }
            }

            public TResult Execute()
            {
                if (_staticFunc != null)
                    return _staticFunc();

                object funcTarget = FuncTarget;
                return !IsAlive || Method == null || LiveReference == null && FuncReference == null ||
                       funcTarget == null
                    ? default
                    : (TResult)Method.Invoke(funcTarget, null);
            }

            public void MarkForDeletion()
            {
                Reference = null;
                FuncReference = null;
                LiveReference = null;
                Method = null;
                _staticFunc = null;
            }
        }

        private readonly WeakAction _executeAction;
        private readonly WeakFunc<bool> _canExecuteFunc;
        private EventHandler _requerySuggestedLocalEventHandler;

        public event EventHandler CanExecuteChanged
        {
            add
            {
                if (_canExecuteFunc == null) return;

                EventHandler handler2;
                EventHandler canExecuteChanged = _requerySuggestedLocalEventHandler;

                do
                {
                    handler2 = canExecuteChanged;
                    EventHandler handler3 = (EventHandler)Delegate.Combine(handler2, value);
                    canExecuteChanged =
                        System.Threading.Interlocked.CompareExchange(ref _requerySuggestedLocalEventHandler, handler3,
                            handler2);
                } while (canExecuteChanged != handler2);

                CommandManager.RequerySuggested += value;
            }
            remove
            {
                if (_canExecuteFunc == null) return;

                EventHandler handler2;
                EventHandler canExecuteChanged = _requerySuggestedLocalEventHandler;

                do
                {
                    handler2 = canExecuteChanged;
                    EventHandler handler3 = (EventHandler)Delegate.Remove(handler2, value);
                    canExecuteChanged =
                        System.Threading.Interlocked.CompareExchange(ref _requerySuggestedLocalEventHandler, handler3,
                            handler2);
                } while (canExecuteChanged != handler2);

                CommandManager.RequerySuggested -= value;
            }
        }

        public RelayCommand(Action execute, bool keepTargetAlive = false) : this(execute, null, keepTargetAlive)
        {
        }

        public RelayCommand(Action execute, Func<bool> canExecute, bool keepTargetAlive = false)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));

            _executeAction = new WeakAction(execute, keepTargetAlive);
            if (canExecute != null)
                _canExecuteFunc = new WeakFunc<bool>(canExecute, keepTargetAlive);
        }

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();

        public bool CanExecute(object parameter)
        {
            if (_canExecuteFunc != null)
                return (_canExecuteFunc.IsStatic || _canExecuteFunc.IsAlive) && _canExecuteFunc.Execute();
            return true;
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter) && _executeAction != null && (_executeAction.IsStatic || _executeAction.IsAlive))
                _executeAction.Execute();
        }
    }
}
