using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BanWordsFilter.Models;

public sealed class SettingFieldViewModel : INotifyPropertyChanged
{
    private string _value = "";

    public SettingFieldViewModel(string key, string label, bool isSecret)
    {
        Key = key;
        Label = label;
        IsSecret = isSecret;
    }

    public string Key { get; }
    public string Label { get; }
    public bool IsSecret { get; }

    public string Value
    {
        get => _value;
        set => SetField(ref _value, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
            field.Value = values.GetValueOrDefault(field.Key, "");
    }

    public new void Clear()
    {
        foreach (var field in this)
            field.Value = "";
    }
}
