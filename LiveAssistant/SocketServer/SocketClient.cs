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

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EmbedIO.WebSockets;
using LiveAssistant.Common.Messages;
using LiveAssistant.Extensions.InputInfo;
using LiveAssistant.Extensions.KaraokeStation;
using LiveAssistant.Extensions.MediaInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;
using LiveAssistant.Common;
using LiveAssistant.Extensions;
using LiveAssistant.Extensions.AudioSpectrum;
using LiveAssistant.Protocols.Data;
using LiveAssistant.Protocols.Data.Models;

namespace LiveAssistant.SocketServer;

internal class SocketClient
{
    private readonly IWebSocketContext _context;
    public IWebSocket Socket => _context.WebSocket;
    public readonly IList<string> Types;
    public readonly string Host;

    public SocketClient(
        IWebSocketContext context,
        IList<string> types)
    {
        _context = context;
        Host = _context.Origin;
        Types = types;

        Setup();
    }

    public RelayCommand Close => new(delegate
    {
        Socket.CloseAsync();
    });

    private void Setup()
    {
        foreach (var typeString in Types)
        {
            var result = Enum.TryParse(typeString, true, out RequestedDataType type);
            if (!result)
            {
                Socket.CloseAsync();
                return;
            }

            switch (type)
            {
                case RequestedDataType.Enter:
                    WeakReferenceMessenger.Default.Register<EnterEventMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.Enter, message.Value.Payload);
                    });
                    break;
                case RequestedDataType.Follow:
                    WeakReferenceMessenger.Default.Register<FollowEventMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.Follow, message.Value.Payload);
                    });
                    break;
                case RequestedDataType.AudienceUpdate:
                    WeakReferenceMessenger.Default.Register<AudienceUpdateEventMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.AudienceUpdate, message.Value.Payload);
                    });
                    break;
                case RequestedDataType.Message:
                    WeakReferenceMessenger.Default.Register<MessageEventMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.Message, message.Value.Payload);
                    });
                    break;
                case RequestedDataType.SuperChat:
                    WeakReferenceMessenger.Default.Register<SuperChatEventMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.SuperChat, message.Value.Payload);
                    });
                    break;
                case RequestedDataType.Gift:
                    WeakReferenceMessenger.Default.Register<GiftEventMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.Gift, message.Value.Payload);
                    });
                    break;
                case RequestedDataType.Membership:
                    WeakReferenceMessenger.Default.Register<MembershipEventMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.Membership, message.Value.Payload);
                    });
                    break;
                case RequestedDataType.ViewersCount:
                    WeakReferenceMessenger.Default.Register<ViewersCountEventMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.ViewersCount, message.Value.Payload);
                    });
                    break;
                case RequestedDataType.Caption:
                    WeakReferenceMessenger.Default.Register<CaptionEventMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.Caption, message.Value.Payload);
                    });
                    break;
                case RequestedDataType.HeartRate:
                    WeakReferenceMessenger.Default.Register<HeartRateEventMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.HeartRate, message.Value.Payload);
                    });
                    break;
                case RequestedDataType.MediaInfo:
                    WeakReferenceMessenger.Default.Register<MediaInfoPayloadMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.MediaInfo, message.Value);
                    });
                    WeakReferenceMessenger.Default.Send(new RequireMediaInfoPayloadMessage());
                    break;
                case RequestedDataType.MediaPlayback:
                    WeakReferenceMessenger.Default.Register<MediaPlaybackPayloadMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.MediaPlayback, message.Value);
                    });
                    WeakReferenceMessenger.Default.Send(new RequireMediaPlaybackPayloadMessage());
                    break;
                case RequestedDataType.InputAudioSpectrum:
                    WeakReferenceMessenger.Default.Register<InputAudioSpectrumPayloadMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.InputAudioSpectrum, message.Value);
                    });
                    break;
                case RequestedDataType.OutputAudioSpectrum:
                    WeakReferenceMessenger.Default.Register<OutputAudioSpectrumPayloadMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.OutputAudioSpectrum, message.Value);
                    });
                    break;
                case RequestedDataType.KaraokeStation:
                    WeakReferenceMessenger.Default.Register<KaraokeStationPayloadMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.KaraokeStation, message.Value);
                    });
                    WeakReferenceMessenger.Default.Send(new RequireKaraokeStationMessage());
                    break;
                case RequestedDataType.MousePosition:
                    WeakReferenceMessenger.Default.Register<MousePositionPayloadMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.MousePosition, message.Value);
                    });
                    break;
                case RequestedDataType.MouseButton:
                    WeakReferenceMessenger.Default.Register<MouseButtonPayloadMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.MouseButton, message.Value);
                    });
                    break;
                case RequestedDataType.KeyboardButton:
                    WeakReferenceMessenger.Default.Register<KeyboardButtonPayloadMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.KeyboardButton, message.Value);
                    });
                    break;
                case RequestedDataType.Gamepad:
                    WeakReferenceMessenger.Default.Register<GamepadPayloadMessage>(this, (_, message) =>
                    {
                        SendData(RequestedDataType.Gamepad, message.Value);
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary>
    /// Send the data over socket as JSON
    /// </summary>
    /// <param name="type">The type of the data</param>
    /// <param name="obj">Payload</param>
    private void SendData<TP>(RequestedDataType type, TP obj)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        var payload = new SocketDataPayload
        {
            Type = type.ToString().ToCamelCase(),
            Payload = obj,
        };

        var json = JsonSerializer.SerializeToUtf8Bytes(payload, Common.Constants.DefaultJsonSerializerOptions);
        if (Socket.State is not WebSocketState.Open) return;
        try
        {
            Socket.SendAsync(json, true);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }
}
