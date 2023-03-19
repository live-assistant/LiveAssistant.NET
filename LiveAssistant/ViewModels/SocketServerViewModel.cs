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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EmbedIO;
using LiveAssistant.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LiveAssistant.Common;
using LiveAssistant.SocketServer;

namespace LiveAssistant.ViewModels;

internal class SocketServerViewModel : ObservableObject
{
    public SocketServerViewModel()
    {
        Manager.IsRunningChanged += delegate
        {
            ApplyState();
        };

        WeakReferenceMessenger.Default.Register<SessionIsConnectedChangedMessage>(this, (_, message) =>
        {
            if (message.Value && IsTestModeEnabled) IsTestModeEnabled = false;
        });

        // Set a initial password
        if (string.IsNullOrEmpty(Password))
        {
            RegeneratePasswordCommand.Execute(null);
        }

        // Listen to messages
        WeakReferenceMessenger.Default.Register<NewSocketClientMessage>(this, (_, m) =>
        {
            var client = m.Value;
            var existingClient = Clients.FirstOrDefault(c => c.Socket == client.Socket);
            if (existingClient != null)
            {
                client.Socket.CloseAsync();
                return;
            }
            Clients.Add(client);
            OnPropertyChanged(nameof(IsClientsEmpty));
        });

        WeakReferenceMessenger.Default.Register<RemoveSocketClientMessage>(this, (_, m) =>
        {
            var client = Clients.FirstOrDefault(c => c.Socket == m.Value.WebSocket);
            if (client is null) return;
            var index = Clients.IndexOf(client);

            if (index < 0) return;
            Clients.RemoveAt(index);
            OnPropertyChanged(nameof(IsClientsEmpty));
        });
    }

    public readonly ExtensionSettingsManager Manager = new(Common.Constants.ExtensionIdSocketServer, new Dictionary<string, string>
    {
        { nameof(Port), 7196.ToString() },
        { nameof(Password), "" },
    });

    // Server
    private WebServer? _server;
    private void SetupServer()
    {
        StopServer();

        _server = new WebServer(options => options
            .WithUrlPrefix($"http://localhost:{Port}/")
            .WithMode(HttpListenerMode.EmbedIO));
        _server.WithLocalSessionManager()
            .WithModule(new SocketServerModule("/socket", true)
            {
                Password = Password,
            });
        _server.StateChanged += OnServerStateChange;
        _server.RunAsync();
    }

    private void StopServer()
    {
        if (_server == null) return;

        _server.StateChanged -= OnServerStateChange;
        if (_server.State is WebServerState.Listening)
        {
            _server.Dispose();
        }

        State = WebServerState.Stopped;

        _server = null;
    }

    private void ApplyState()
    {
        if (Manager.IsRunning)
        {
            SetupServer();
        }
        else
        {
            StopServer();
        }
    }

    private WebServerState _state = WebServerState.Stopped;
    public WebServerState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    private void OnServerStateChange(object sender, WebServerStateChangedEventArgs e)
    {
        State = e.NewState;
    }

    public int Port
    {
        get => Convert.ToInt32(Manager.Settings[Common.Constants.ExtensionSettingKeySocketServerPort]);
        set
        {
            Manager.SaveSetting(Common.Constants.ExtensionSettingKeySocketServerPort, value.ToString());
        }
    }

    public string Password
    {
        get => Manager.Settings[Common.Constants.ExtensionSettingKeySocketServerPassword];
        set
        {
            Manager.SaveSetting(Common.Constants.ExtensionSettingKeySocketServerPassword, value);
        }
    }

    public RelayCommand CopyPasswordCommand => new(delegate
    {
        Clipboard.SetText(Password);
    });

    public RelayCommand RegeneratePasswordCommand => new(delegate
    {
        Password = Helpers.GetUniqueKey(20);
    });

    // Clients
    public readonly ObservableCollection<SocketClient> Clients = new();
    public bool IsClientsEmpty => !Clients.Any();

    // Test mode
    private bool _isTestModeEnabled;
    public bool IsTestModeEnabled
    {
        get => _isTestModeEnabled;
        set
        {
            SetProperty(ref _isTestModeEnabled, value);
            WeakReferenceMessenger.Default.Send(new SocketSeverTestModeIsEnabledChangeMessage(value));

            if (value)
            {
                _tester.Start();
            }
            else
            {
                _tester.Stop();
            }
        }
    }

    private readonly Tester _tester = new();
}

public class SocketSeverTestModeIsEnabledChangeMessage : ValueChangedMessage<bool>
{
    public SocketSeverTestModeIsEnabledChangeMessage(bool value) : base(value) { }
}
