using System.Windows.Input;

namespace RPGGamerAnywhere.WPF.ViewModel.Commands
{
    public class DownloadCommand(MainVM vm) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => vm.SelectedSong is not null;

        public void Execute(object? parameter) => new Thread(async () =>
        {
            await MainVM.SaveSong(vm.SelectedSong);
        }).Start();
    }
}