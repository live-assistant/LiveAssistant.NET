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
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Timers;
using CommunityToolkit.Mvvm.Messaging;
using LiveAssistant.Common.Messages;
using LiveAssistant.Pages;
using Websocket.Client;

namespace LiveAssistant.Extensions.HeartRate;

public sealed partial class HeartRateExtension : INotifyPropertyChanged, IDisposable
{
    public HeartRateExtension()
    {
        InitializeComponent();

        _manager.IsRunningChanged += delegate
        {
            ApplyState();
        };

        // HypeRate
        SetupHypeRate();

        // Listen to heart rate change
        HeartRateChanged += OnHeartRateChange;

        WeakReferenceMessenger.Default.Register<MainWindowClosedMessage>(this, delegate
        {
            Dispose();
        });
    }

    private readonly ExtensionSettingsManager _manager = new("heart-rate", new Dictionary<string, string>
    {
        { nameof(HypeRateId), "" },
    });

    private static void OnHeartRateChange(object? sender, int rate)
    {
        WeakReferenceMessenger.Default.Send(new HeartRateEventMessage(new Database.HeartRate(
            rate,
            DateTimeOffset.Now)));
    }

    private void ApplyState()
    {
        if (_manager.IsRunning)
        {
            StartHypeRate();
        }
        else
        {
            StopHypeRate();
        }
    }

    private int _heartRate = 0;
    public int HeartRate
    {
        get => _heartRate;
        set
        {
            SetField(ref _heartRate, value);
            HeartRateChanged?.Invoke(this, value);
        }
    }

    private event EventHandler<int> HeartRateChanged;

    private bool _isDataSourceConnected = false;
    public bool IsDataSourceConnected
    {
        get => _isDataSourceConnected;
        set => SetField(ref _isDataSourceConnected, value);
    }

    // HypeRate
    private string HypeRateId
    {
        get => _manager.Settings[nameof(HypeRateId)];
        set
        {
            _manager.SaveSetting(nameof(HypeRateId), value);
            HypeRateIdChanged?.Invoke(this, value);
        }
    }
    private event EventHandler<string>? HypeRateIdChanged;

    private readonly WebsocketClient _hypeRateClient = new(new Uri($"wss://app.hyperate.io/socket/websocket?token={Env.HypeRateIoApiKey}"))
    {
        IsReconnectionEnabled = true,
        ReconnectTimeout = TimeSpan.FromMinutes(2.5),
    };
    private readonly Timer _hypeRateTimer = new()
    {
        Interval = 30000,
        AutoReset = true,
    };

    private void SetupHypeRate()
    {
        _hypeRateClient.DisconnectionHappened.Subscribe(OnHypeRateDisconnect);
        _hypeRateClient.MessageReceived.Subscribe(OnHypeRateMessage);

        _hypeRateTimer.Elapsed += OnHypeRateTimerFire;

        HypeRateIdChanged += delegate
        {
            ApplyState();
        };
    }

    private async void StartHypeRate()
    {
        if (IsDataSourceConnected || string.IsNullOrEmpty(HypeRateId)) return;

        if (_hypeRateClient.IsRunning)
        {
            StopHypeRate();
        }

        await _hypeRateClient.Start();
        IsDataSourceConnected = true;
        _hypeRateClient.Send(JsonSerializer.Serialize(new HypeRateMessage
        {
            Topic = $"hr:{HypeRateId}",
            Event = "phx_join",
            Payload = new HypeRateMessagePayload(),
            Ref = 0,
        }, Common.Constants.DefaultJsonSerializerOptions));
    }

    private void OnHypeRateTimerFire(object? sender, ElapsedEventArgs e)
    {
        _hypeRateClient.Send(JsonSerializer.Serialize(new HypeRateMessage
        {
            Topic = "phoenix",
            Event = "heartbeat",
            Payload = new HypeRateMessagePayload(),
            Ref = 0,
        }, Common.Constants.DefaultJsonSerializerOptions));
    }

    private async void StopHypeRate()
    {
        if (!_hypeRateClient.IsRunning || !IsDataSourceConnected) return;

        await _hypeRateClient.Stop(WebSocketCloseStatus.NormalClosure, "Close");
    }

    private void OnHypeRateDisconnect(DisconnectionInfo obj)
    {
        App.Current.MainQueue.TryEnqueue(delegate
        {
            IsDataSourceConnected = false;
            HeartRate = 0;
        });
    }

    private void OnHypeRateMessage(ResponseMessage message)
    {
        var response = JsonSerializer.Deserialize<HypeRateReply>(message.Text, Common.Constants.DefaultJsonSerializerOptions);
        App.Current.MainQueue.TryEnqueue(delegate
        {
            HeartRate = response.Payload.Hr;
        });
    }

    // VM
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
        _hypeRateClient.Dispose();
        _hypeRateTimer.Dispose();
    }
}
