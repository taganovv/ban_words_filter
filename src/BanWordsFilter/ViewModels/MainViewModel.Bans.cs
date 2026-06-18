using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using BanWordsFilter.Models;

namespace BanWordsFilter.ViewModels;

public sealed partial class MainViewModel
{
    public ObservableCollection<BanRecord> TodayBans { get; } = [];

    public ICommand SelectBansCommand { get; private set; } = null!;
    public ICommand UnbanUserCommand { get; private set; } = null!;

    public string TodayBansHeader => $"Баны за сегодня ({DateTime.Now:dd.MM.yyyy})";

    public bool HasTodayBans => TodayBans.Count > 0;

    public bool ShowTodayBansEmpty => !HasTodayBans;

    public string TodayBansEmptyText =>
        "Сегодня банов пока нет. Запустите фильтр — забаненные пользователи появятся здесь.";

    public string TodayBansCountText => TodayBans.Count switch
    {
        0 => "Нет активных банов",
        1 => "Забанен 1 пользователь",
        >= 2 and <= 4 => $"Забанено {TodayBans.Count} пользователя",
        _ => $"Забанено {TodayBans.Count} пользователей",
    };

    private void InitBansCommands()
    {
        SelectBansCommand = new RelayCommand(async () =>
        {
            SelectedTab = 4;
            await LoadBansAsync();
        });
        UnbanUserCommand = new RelayCommand<BanRecord>(async record => await UnbanUserAsync(record));
    }

    public async Task LoadBansAsync()
    {
        try
        {
            var bans = await _services.GetTodayBansAsync();
            TodayBans.Clear();
            foreach (var ban in bans)
                TodayBans.Add(ban);

            NotifyBansChanged();
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, true);
        }
    }

    public async Task UnbanUserAsync(BanRecord? record)
    {
        if (record is null)
            return;

        try
        {
            var (ok, message) = await _services.UnbanUserAsync(record);
            ShowToast(message, !ok);
            if (!ok)
                return;

            await LoadBansAsync();
        }
        catch (Exception ex)
        {
            ShowToast(ex.Message, true);
        }
    }

    private void NotifyBansChanged()
    {
        OnPropertyChanged(nameof(TodayBansHeader));
        OnPropertyChanged(nameof(HasTodayBans));
        OnPropertyChanged(nameof(ShowTodayBansEmpty));
        OnPropertyChanged(nameof(TodayBansEmptyText));
        OnPropertyChanged(nameof(TodayBansCountText));
    }
}
