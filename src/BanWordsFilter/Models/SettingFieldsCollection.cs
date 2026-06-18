using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace BanWordsFilter.Models;

public sealed class SettingFieldViewModel : INotifyPropertyChanged
{
    private string _value = "";
    private bool _isRevealed;

    public SettingFieldViewModel(string key, string label, bool isSecret)
    {
        Key = key;
        Label = label;
        IsSecret = isSecret;
        ToggleRevealCommand = new RelayCommand(
            () => IsRevealed = !IsRevealed,
            () => IsSecret);
    }

    public string Key { get; }
    public string Label { get; }
    public bool IsSecret { get; }
    public bool ShowRevealToggle => IsSecret;
    public bool IsMasked => IsSecret && !IsRevealed;
    public ICommand ToggleRevealCommand { get; }

    public bool IsRevealed
    {
        get => _isRevealed;
        set
        {
            if (!IsSecret || !SetField(ref _isRevealed, value))
                return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMasked)));
        }
    }

    public string Value
    {
        get => _value;
        set => SetField(ref _value, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}

public sealed class SettingFieldsCollection : ObservableCollection<SettingFieldViewModel>
{
    public SettingFieldsCollection()
    {
        foreach (var key in AppSettings.Fields)
        {
            Add(new SettingFieldViewModel(
                key,
                AppSettings.Labels[key],
                AppSettings.SecretFields.Contains(key)));
        }
    }

    public Dictionary<string, string> ToDictionary()
    {
        var result = new Dictionary<string, string>();
        foreach (var field in this)
            result[field.Key] = field.Value.Trim();
        return result;
    }

    public void Load(Dictionary<string, string> values)
    {
        foreach (var field in this)
        {
            field.Value = values.GetValueOrDefault(field.Key, "");
            field.IsRevealed = false;
        }
    }

    public new void Clear()
    {
        foreach (var field in this)
        {
            field.Value = "";
            field.IsRevealed = false;
        }
    }
}
