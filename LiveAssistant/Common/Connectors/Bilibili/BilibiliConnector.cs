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
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ISO._4217;
using LiveAssistant.Common.Connectors.Bilibili.Models;
using LiveAssistant.Common.Messages;
using LiveAssistant.Common.Types;
using LiveAssistant.Database;
using Microsoft.UI.Xaml.Controls;
using Host = LiveAssistant.Database.Host;
using Message = LiveAssistant.Database.Message;
using StringContent = LiveAssistant.Database.StringContent;

namespace LiveAssistant.Common.Connectors.Bilibili;

internal class BilibiliConnector : IConnector, IDisposable
{
    public BilibiliConnector(Host host)
    {
        _roomId = Convert.ToInt32(host.ChannelId);
    }

    public static RelayCommand AddHost => new(delegate
    {
        var channelIdInput = new TextBox
        {
            PlaceholderText = "BilibiliHostDialogChannelIdTextBoxPlaceholderText".Localize(),
        };

        var dialog = new ContentDialog
        {
            Title = "BilibiliHostDialogTitle".Localize(),
            PrimaryButtonText = "ButtonConfirm".Localize(),
            CloseButtonText = "ButtonCancel".Localize(),
            DefaultButton = ContentDialogButton.Primary,
            Content = channelIdInput,
        };

        dialog.PrimaryButtonClick += (sender, args) =>
        {
            var deferral = args.GetDeferral();
            sender.IsPrimaryButtonEnabled = false;
            channelIdInput.IsEnabled = false;

            var channelId = channelIdInput.Text;
            var host = new Host(
                Platforms.Bilibili,
                channelId)
            {
                ChannelId = channelId,
                AuthTime = DateTimeOffset.Now,
            };

            Db.Default.Realm.Write(delegate
            {
                Db.Default.Realm.Add(host, true);
            });

            deferral.Complete();
        };

        WeakReferenceMessenger.Default.Send(new ShowContentDialogMessage(dialog));
    });

    private bool _connected;
    private readonly int _roomId;
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5),
    };
    private BilibiliTcpConnection? _connection;

    public async void Connect()
    {
        GetGiftsList();

        var response = await _httpClient.GetStringAsync($"https://api.live.bilibili.com/xlive/web-room/v1/index/getDanmuInfo?id={_roomId}");
        var json = JsonSerializer.Deserialize<ConnectionData>(response, new JsonSerializerOptions
        {
            IncludeFields = true,
        });
        var token = json.data.token;

        foreach (var host in json.data.host_list)
        {
            BilibiliTcpConnection? connection = null;

            try
            {
                connection = new BilibiliTcpConnection(host.host, host.port, _roomId, token);
                _connection = connection;
                _connected = true;
                _connection.OnDataBlock += OnDataBlock;
                _connection.OnError += OnConnectionError;
                await connection.ConnectAsync();
                return;
            }
            catch (Exception e)
            {
                OnError?.Invoke(this, e);
                _connected = false;
                connection?.Dispose();
            }
        }

        OnFatalError?.Invoke(this, new Exception("BilibiliConnectorExceptionCannotConnect".Localize()));
    }

    public void Disconnect()
    {
        _connected = false;
        if (_connection is null) return;
        _connection.OnDataBlock -= OnDataBlock;
        _connection.OnError -= OnConnectionError;
        _connection = null;
    }

    public event EventHandler<Exception>? OnError;
    public event EventHandler<Exception>? OnFatalError;

    private void OnConnectionError(object? sender, Exception e)
    {
        OnError?.Invoke(this, e);
        if (!_connected) return;
        Disconnect();
        Connect();
    }

    private void OnDataBlock(object? sender, BilibiliDataBlock block)
    {
        if (!_connected) return;
        var version = block.Version;
        var buffer = block.Sequence;

        if (version is not 0) return;
        try
        {
            var json = Encoding.UTF8.GetString(buffer);
            App.Current.MainQueue.TryEnqueue(delegate
            {
                ProcessCommandPayload(json);
            });
        }
        catch (Exception e)
        {
            OnError?.Invoke(this, e);
        }
    }

    private void ProcessCommandPayload(string json)
    {
        try
        {
            var payload = JsonNode.Parse(json);
            if (payload == null) return;
            var command = payload["cmd"]?.ToString();

            switch (command)
            {
                case "DANMU_MSG":
                    OnMessage(payload);
                    break;
                case "SUPER_CHAT_MESSAGE":
                case "SUPER_CHAT_MESSAGE_JP":
                    OnSuperChat(payload);
                    break;
                case "SEND_GIFT":
                    OnGift(payload);
                    break;
                case "GUARD_BUY":
                    OnMembership(payload);
                    break;
                case "WATCHED_CHANGE":
                    OnViewersCount(payload);
                    break;
                case "INTERACT_WORD":
                    OnInteract(payload);
                    break;
                default:
                    if (command?.StartsWith("DANMU_MSG") ?? false)
                    {
                        OnMessage(payload);
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            OnError?.Invoke(this, e);
        }
    }

    public event EventHandler<Enter>? Entered;
    public event EventHandler<Follow>? Followed;
    private void OnInteract(JsonNode payload)
    {
        var data = payload["data"];
        var username = data?["uname"]?.ToString();
        var badgeData = data?["fans_medal"];

        var audience = Audience.Create(
            Platforms.Bilibili,
            data?["uid"]?.ToString() ?? throw new NullReferenceException(),
            userName: username,
            displayName: username,
            badges: badgeData?.AsObject().Count > 0 ? new List<Badge>
            {
                Badge.Create(
                    Platforms.Bilibili,
                    _roomId.ToString(),
                    badgeData["medal_level"]?.GetValue<int>() ?? 0,
                    displayName: badgeData["medal_name"]?.ToString()),
            } : null,
            type: (data["isadmin"]?.GetValue<int>() ?? 0) is 1
                ? AudienceTypes.ChannelModerator
                : AudienceTypes.General);
        UpdateAudience(audience);

        var type = data["msg_type"]?.GetValue<int>();
        switch (type)
        {
            case 1:
                var enter = new Enter(
                    Platforms.Bilibili,
                    audience);

                Entered?.Invoke(this, enter);
                break;
            case 2:
                var follow = new Follow(
                    Platforms.Bilibili,
                    audience);

                Followed?.Invoke(this, follow);
                break;
        }
    }

    public event EventHandler<Message>? MessageReceived;
    private void OnMessage(JsonNode payload)
    {
        var info = payload["info"];
        var userData = info?[2];
        var userName = userData?[1]?.ToString();
        var badgeData = info?[3];

        var extraData = info?[0]?[15]?["extra"]?.ToString();
        var emotes = new List<Emote>();
        if (extraData != null)
        {
            var emotesData = JsonNode.Parse(extraData)?["emots"]?.AsObject();
            if (emotesData != null)
            {
                foreach ((string _, JsonNode? value) in emotesData)
                {
                    emotes.Add(Emote.Create(
                        Platforms.Bilibili,
                        value?["emoji"]?.ToString() ?? throw new NullReferenceException(),
                        value["url"]?.ToString() ?? throw new NullReferenceException(),
                        id: value["emoticon_unique"]?.ToString()));
                }
            }
        }

        var audience = Audience.Create(
            Platforms.Bilibili,
            userData?[0]?.ToString() ?? throw new NullReferenceException(),
            userName: userName,
            displayName: userName,
            badges: badgeData?.AsArray().Any() ?? false ? new List<Badge>
            {
                Badge.Create(
                    Platforms.Bilibili,
                    _roomId.ToString(),
                    level: badgeData[0]?.GetValue<int>() ?? 0,
                    displayName: badgeData[1]?.ToString()),
            } : null,
            isMember: userData[3]?.GetValue<int>() is 1,
            type: userData[2]?.GetValue<int>() is 1 ? AudienceTypes.ChannelModerator : AudienceTypes.General);
        UpdateAudience(audience);

        var message = new Message(
            Platforms.Bilibili,
            $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}{Helpers.GetUniqueKey(24)}",
            DateTimeOffset.Now,
            audience,
            StringContent.Create(info?[1]?.ToString() ?? throw new NullReferenceException()),
            emotes: emotes.Any() ? emotes : null);

        MessageReceived?.Invoke(this, message);
    }

    public event EventHandler<SuperChat>? SuperChatReceived;
    private void OnSuperChat(JsonNode? payload)
    {
        var data = payload?["data"];
        var userName = data?["user_info"]?["uname"]?.ToString();

        var audience = Audience.Create(
            Platforms.Bilibili,
            data?["uid"]?.ToString() ?? throw new NullReferenceException(),
            userName: userName,
            displayName: userName);
        UpdateAudience(audience);

        var sku = new Sku(
            Platforms.Bilibili,
            "SuperChat",
            0)
        {
            Amount = (data["price"]?.GetValue<int>() ?? 0) / (float)1000,
            Currency = CurrencyCodesResolver.GetCodeByNumber(Constants.CurrencyCodeCny),
            DisplayName = StringContent.Create("TermSuperChat".Localize()),
        };

        var superChat = new SuperChat(
            Platforms.Bilibili,
            $"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}{Helpers.GetUniqueKey(24)}",
            sku,
            audience,
            DateTimeOffset.Now,
            StringContent.Create(data["message"]?.ToString() ?? throw new NullReferenceException()),
            DateTimeOffset.Now,
            DateTimeOffset.Now.AddSeconds(data["time"]?.GetValue<int>() ?? 30));

        SuperChatReceived?.Invoke(this, superChat);
    }

    private readonly Dictionary<int, BilibiliGiftPayload> _giftsDictionary = new();
    private async void GetGiftsList()
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"https://api.live.bilibili.com/xlive/web-room/v1/giftPanel/giftConfig?platform=pc&room_id={_roomId}");
            var json = JsonSerializer.Deserialize<BilibiliGiftListResponse>(response);
            foreach (var payload in json.data.list)
            {
                _giftsDictionary[payload.id] = payload;
            }
        }
        catch (Exception e)
        {
            OnError?.Invoke(this, e);
        }
    }

    public event EventHandler<Gift>? GiftReceived;
    private void OnGift(JsonNode payload)
    {
        var data = payload["data"];
        var userName = data?["uname"]?.ToString();
        var badgeData = data?["medal_info"];

        var audience = Audience.Create(
            Platforms.Bilibili,
            data?["uid"]?.ToString() ?? throw new NullReferenceException(),
            userName: userName,
            displayName: userName,
            avatar: data["face"]?.ToString(),
            badges: badgeData?.AsObject().Count > 0
                ? new List<Badge>
                {
                    Badge.Create(
                        Platforms.Bilibili,
                        _roomId.ToString(),
                        badgeData["medal_level"]?.GetValue<int>() ?? 0,
                        displayName: badgeData["medal_name"]?.ToString()),
                }
                : null);
        UpdateAudience(audience);

        var skuId = data["giftId"]?.GetValue<int>() ?? 0;
        _giftsDictionary.TryGetValue(skuId, out var giftPayload);
        var sku = new Sku(
            Platforms.Bilibili,
            skuId.ToString(),
            0)
        {
            Amount = (data["price"]?.GetValue<int>() ?? 0) / (float)1000,
            Currency = CurrencyCodesResolver.GetCodeByNumber(Constants.CurrencyCodeCny),
            DisplayName = StringContent.Create(data["giftName"]?.ToString() ?? throw new NullReferenceException()),
            Image = ImageContent.Create(
                Platforms.Bilibili,
                giftPayload.webp),
        };

        var time = data["timestamp"]?.GetValue<int>();
        var gift = new Gift(
            Platforms.Bilibili,
            time is null ? DateTimeOffset.Now : DateTimeOffset.FromUnixTimeSeconds((long)time),
            sku,
            id: data["tid"]?.ToString(),
            sender: audience,
            count: data["num"]?.GetValue<int>() ?? 1);

        GiftReceived?.Invoke(this, gift);
    }

    public event EventHandler<Membership>? MembershipReceived;
    private void OnMembership(JsonNode payload)
    {
        var data = payload["data"];
        var userName = data?["uname"]?.ToString();

        var audience = Audience.Create(
            Platforms.Bilibili,
            data?["uid"]?.ToString() ?? throw new NullReferenceException(),
            userName: userName,
            displayName: userName,
            isMember: true);
        UpdateAudience(audience);

        var memberLevel = data["guard_level"]?.GetValue<int>() ?? 1;
        var sku = new Sku(
            Platforms.Bilibili,
            $"membership.{memberLevel}",
            memberLevel)
        {
            Amount = Constants.BilibiliMembershipLevelToAmountMap[memberLevel],
            Currency = CurrencyCodesResolver.GetCodeByNumber(Constants.CurrencyCodeCny),
            DisplayName = StringContent.Create(Constants.BilibiliMembershipLevelToNameMap[memberLevel]),
            Image = ImageContent.Create(
                Platforms.Bilibili,
                Constants.BilibiliMembershipLevelToImageMap[memberLevel]),
        };

        var count = data["num"]?.GetValue<int>() ?? 1;
        var membership = new Membership(
            Platforms.Bilibili,
            sku,
            audience,
            DateTimeOffset.Now,
            DateTimeOffset.Now.AddMonths(count),
            count: count);

        MembershipReceived?.Invoke(this, membership);
    }

    public event EventHandler<ViewersCount>? ViewersCountChanged;
    private void OnViewersCount(JsonNode payload)
    {
        var viewersCount = new ViewersCount(
            Platforms.Bilibili,
            payload["data"]?["num"]?.GetValue<int>() ?? 0);

        ViewersCountChanged?.Invoke(this, viewersCount);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _connection?.Dispose();
    }

    private readonly List<string> _updatedAudiences = new();
    private void UpdateAudience(Audience audience)
    {
        var id = audience.Id;
        if (_updatedAudiences.Contains(id)) return;

        try
        {
            App.Current.MainQueue.TryEnqueue(async delegate
            {
                var response = await _httpClient.GetStringAsync($"https://api.bilibili.com/x/space/app/index?mid={audience.UserId}");
                var info = JsonNode.Parse(response)?["data"]?["info"];

                // ReSharper disable once MethodHasAsyncOverload
                Db.Default.Realm.Write(delegate
                {
                    var url = info?["face"]?.ToString();
                    if (!string.IsNullOrEmpty(url)) audience.Avatar = ImageContent.Create(
                        Platforms.Bilibili,
                        url);
                    var username = info?["name"]?.ToString();
                    if (!string.IsNullOrEmpty(username))
                    {
                        audience.Username = StringContent.Create(username);
                        audience.DisplayName = StringContent.Create(username);
                    }
                });

                WeakReferenceMessenger.Default.Send(new AudienceUpdateEventMessage(audience));
                _updatedAudiences.Add(id);
            });
            _updatedAudiences.Add(id);
        }
        catch (Exception e)
        {
            OnError?.Invoke(this, e);
        }
    }
}

public struct ConnectionData
{
    public int code;
    public Data data;
    public string message;
    public int ttl;

    // ReSharper disable UnassignedField.Global
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedMember.Global
    public struct Data
    {
        public string token;
        public Host[] host_list;
        public int business_id;
        public string group;
        public int max_delay;
        public int refresh_rate;
        public double refresh_row_factor;
    }

    public struct Host
    {
        public string host;
        public ushort port;
        public ushort wss_port;
        public ushort ws_port;
    }
    // ReSharper restore UnusedMember.Global
    // ReSharper restore InconsistentNaming
    // ReSharper restore UnassignedField.Global
}
