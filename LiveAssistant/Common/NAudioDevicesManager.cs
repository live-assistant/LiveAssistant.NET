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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace LiveAssistant.Common;

internal class NAudioDevicesManager : IMMNotificationClient, INotifyPropertyChanged, IDisposable
{
    public NAudioDevicesManager(DataFlow dataFlow)
    {
        _dataFlow = dataFlow;
        _enumerator.RegisterEndpointNotificationCallback(this);
        UpdateDevices();
    }

    private readonly DataFlow _dataFlow;
    private readonly MMDeviceEnumerator _enumerator = new();

    public ObservableCollection<MMDevice> Devices = new();
    public event EventHandler? DevicesChanged;

    public MMDevice GetDevice(string id)
    {
        return _enumerator.GetDevice(id);
    }

    private void UpdateDevices()
    {
        App.Current.MainQueue.TryEnqueue(delegate
        {
            Devices.Clear();
            foreach (var device in _enumerator.EnumerateAudioEndPoints(_dataFlow, DeviceState.Active))
            {
                Devices.Add(device);
            }

            OnPropertyChanged(nameof(Devices));
            DevicesChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        UpdateDevices();
    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
        UpdateDevices();
    }

    public void OnDeviceRemoved(string deviceId)
    {
        UpdateDevices();
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId) { }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key) { }

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

    public void Dispose()
    {
        _enumerator.Dispose();
    }
}
