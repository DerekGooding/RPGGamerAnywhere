using System.Windows.Input;

namespace RPGGamerAnywhere.WPF.ViewModel.Commands;

public class CreatePlaylistCommand(MainVM vm) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => vm.CreatePlaylist();
}