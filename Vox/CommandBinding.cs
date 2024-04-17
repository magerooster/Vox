using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Vox
{
    //Model       <-> ViewModel <-> View
    //VerveClient     This          UI Elements

    //VerveClient will need to implement INotifyPropertyChanged at all levels internally
    //Anything the View does to the Model needs to be routed through an ICommand.

    //The view model uses this class to implement actions that operate on the Verve object model (our model) from the UI (our view). 
    public class CommandBinding : ICommand
    {
        private readonly Predicate<object> _canExecute;
        private readonly Action<object> _execute;

        private INotifyPropertyChanged[] Subscribed;

        public CommandBinding(Action<object> execute) : this(execute, null)
        {

        }

        public CommandBinding(Action<object> execute, Predicate<object> canExecute, params INotifyPropertyChanged[] Subscribers)
        {
            _execute = execute;
            _canExecute = canExecute;

            foreach (var Subscriber in Subscribers)
                Subscriber.PropertyChanged += RaiseCanExecuteChangedFromEvent;

            Subscribed = Subscribers;
        }

        ~CommandBinding()
        {
            foreach (var Subscriber in Subscribed)
                Subscriber.PropertyChanged -= RaiseCanExecuteChangedFromEvent;
        }

        public bool CanExecute()
        {
            return CanExecute(null);
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null)
                return true;
            return _canExecute(parameter);
        }

        public void Execute()
        {
            Execute(null);
        }

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseCanExecuteChangedFromEvent(object? sender, EventArgs e)
        {
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CanExecuteChanged?.Invoke(this, e);
                });
            }
            else
            {
                CanExecuteChanged?.Invoke(this, e);
            }
        }

        //A couple premade delegates for common states: Always true and always false.
        public static bool AlwaysTrue(object parameter)
        {
            return true;
        }
        public static bool AlwaysFalse(object parameter)
        {
            return false;
        }

        public static CommandBinding NotYetImplemented = new CommandBinding(NotYetImplementedCommand, AlwaysFalse);
        protected static void NotYetImplementedCommand(object parameter)
        {

        }
    }
}