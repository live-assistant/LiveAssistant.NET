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
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using EmbedIO;
using LiveAssistant.Common;
using LiveAssistant.Common.Connectors;
using LiveAssistant.Extensions;
using LiveAssistant.Pages;
using LiveAssistant.SocketServer;

namespace LiveAssistant.ViewModels;

class ServerViewModel : ObservableObject, IDisposable
{
    public ServerViewModel()
    {
        // Set a initial password
        if (string.IsNullOrEmpty(Password))
        {
            RegeneratePasswordCommand.Execute(null);
        }

        // Create folder for overlay packages
        if (!Directory.Exists(Constants.OverlayPackagesFolderPath))
        {
            Directory.CreateDirectory(Constants.OverlayPackagesFolderPath);
        }

        // Setup server and run
        _server = new WebServer(o => o
                .WithUrlPrefix(Constants.ServerBasePath)
                .WithMode(HttpListenerMode.EmbedIO)).WithLocalSessionManager()
                .WithModule(new SocketServerModule("/data", true)
                {
                    Password = Password,
                })
                .WithStaticFolder("/", Constants.OverlayPackagesFolderPath, false);

        _server.StateChanged += OnServerStateChange;
        _server.RunAsync();

        // Handle app exit
        WeakReferenceMessenger.Default.Register<MainWindowClosedMessage>(this, delegate
        {
            Dispose();
        });

        // Stop test mode once session starts recording
        WeakReferenceMessenger.Default.Register<SessionIsConnectedChangedMessage>(this, (_, message) =>
        {
            if (message.Value && IsTestModeEnabled) IsTestModeEnabled = false;
        });

        // Handle socket clients
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

    public readonly ExtensionSettingsManager Manager = new(Constants.ExtensionIdServer, new Dictionary<string, string>
    {
        { Constants.ExtensionSettingKeySocketServerPassword, "" },
    });

    private string Password
    {
        get => Manager.Settings[Constants.ExtensionSettingKeySocketServerPassword];
        set
        {
            Manager.SaveSetting(Constants.ExtensionSettingKeySocketServerPassword, value);
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

    private readonly WebServer _server;

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
            WeakReferenceMessenger.Default.Send(new SeverTestModeIsEnabledChangeMessage(value));

            if (value)
            {
                _tester.Connect();
            }
            else
            {
                _tester.Disconnect();
            }
        }
    }

    private readonly TestConnector _tester = new(false);

    public void Dispose()
    {
        _server.Dispose();
    }
}

public class SeverTestModeIsEnabledChangeMessage : ValueChangedMessage<bool>
{
    public SeverTestModeIsEnabledChangeMessage(bool value) : base(value) { }
}
