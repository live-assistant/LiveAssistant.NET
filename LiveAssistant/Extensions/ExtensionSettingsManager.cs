//    Copyright (C) 2023  Live Assistant official Windows app Authors
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Messaging;
using LiveAssistant.Common;
using LiveAssistant.Database;
using LiveAssistant.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Swan;

namespace LiveAssistant.Extensions;

internal class ExtensionSettingsManager : INotifyPropertyChanged
{
    public ExtensionSettingsManager(string id, Dictionary<string, string>? defaultSettings = null)
    {
        _settings = ExtensionSettings.Get($"{Constants.ExtensionIdPrefix}.{id}");

        Db.Default.Realm.Write(delegate
        {
            if (defaultSettings is null) return;

            defaultSettings.ForEach((key, value) =>
            {
                if (!_settings.Settings.ContainsKey(key)) _settings.Settings[key] = value;
            });

            _settings.Settings.ForEach((key, _) =>
            {
                if (!defaultSettings.ContainsKey(key)) _settings.Settings.Remove(key);
            });
        });

        WeakReferenceMessenger.Default.Register<SessionIsConnectedChangedMessage>(this, delegate
        {
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(IsRunning));
            IsRunningChanged?.Invoke(this, IsRunning);
        });

        WeakReferenceMessenger.Default.Register<SocketSeverTestModeIsEnabledChangeMessage>(this, (_, message) =>
        {
            _isSocketServerTestModeEnabled = message.Value;
            OnPropertyChanged(nameof(IsRunning));
            IsRunningChanged?.Invoke(this, IsRunning);
        });
    }

    private readonly ExtensionSettings _settings;

    public IDictionary<string, string> Settings => _settings.Settings;
    public void SaveSetting(string key, string value)
    {
        Db.Default.Realm.Write(delegate
        {
            _settings.Settings[key] = value;
        });
        OnPropertyChanged(nameof(Settings));
    }

    private readonly SessionViewModel _sessionViewModel = App.Current.Services.GetService<SessionViewModel>() ?? throw new NullReferenceException();
    public bool IsConnected => _sessionViewModel.IsConnected;
    public bool IsEnabled
    {
        get => _settings.IsEnabled;
        set
        {
            Db.Default.Realm.Write(delegate
            {
                _settings.IsEnabled = value;
            });
            OnPropertyChanged(nameof(IsRunning));
            IsRunningChanged?.Invoke(this, IsRunning);
        }
    }

    private bool _isSocketServerTestModeEnabled;

    public bool IsRunning => (IsEnabled && IsConnected) || _isSocketServerTestModeEnabled;
    public event EventHandler<bool>? IsRunningChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
