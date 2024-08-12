using System.Windows.Input;

namespace RPGGamerAnywhere.WPF.ViewModel.Commands;

public class SearchLinksCommand(MainVM vm) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter) => false;
    public void Execute(object? parameter) => vm.LookForLinksAsync();
}
