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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ISO._4217;
using LiveAssistant.Common.Messages;
using LiveAssistant.Common.Types;
using LiveAssistant.Database;
using LiveAssistant.ViewModels;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using Emote = LiveAssistant.Database.Emote;
using Message = LiveAssistant.Database.Message;

namespace LiveAssistant.Common.Connectors;

internal class TwitchConnector : IConnector
{
    public TwitchConnector(Host host)
    {
        var secrets = host.Secrets;
        _expiresIn = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(secrets[Constants.SecretNameExpiresOn]));
        var accessToken = secrets[Constants.SecretNameAccessToken];
        var channelId = host.ChannelId;

        // API
        _api = new TwitchAPI
        {
            Settings =
            {
                ClientId = Env.TwitchClientId,
                AccessToken = accessToken,
            },
        };

        // Client
        _client = new TwitchClient(new WebSocketClient(new ClientOptions
        {
            MessagesAllowedInPeriod = 500,
            ThrottlingPeriod = TimeSpan.FromSeconds(15),
            ReconnectionPolicy = new ReconnectionPolicy(5),
        }));

        var credentials = new ConnectionCredentials(channelId, accessToken);
#if DEBUG
        _client.Initialize(credentials, channelId, char.Parse("!"), char.Parse("!"), false);
#else
        _client.Initialize(credentials, channelId);
#endif

        // PubSub
        _pubSub = new TwitchPubSub();
        _pubSub.ListenToFollows(channelId);
        _pubSub.ListenToSubscriptions(channelId);
        _pubSub.ListenToBitsEventsV2(channelId);
    }

    private readonly TwitchAPI _api;
    private readonly TwitchClient _client;
    private readonly TwitchPubSub _pubSub;
    private readonly DateTimeOffset _expiresIn;

    public static RelayCommand AddHost => new(delegate
    {
        WeakReferenceMessenger.Default.Send(new RequestTwitchOAuthMessage());
    });

    public void Connect()
    {
        if (_expiresIn < DateTimeOffset.Now)
        {
            AddHost.Execute(null);
            OnFatalError?.Invoke(this, new Exception("ExceptionMessageAuthenticationExpired".Localize()));
            return;
        }

        Task.Run(delegate
        {
            try
            {
                _client.OnError += OnClientError;
                _client.OnConnectionError += OnClientConnectionError;
                _client.OnMessageReceived += OnMessage;
                _client.Connect();

                _pubSub.OnChannelSubscription += OnMembership;
                _pubSub.OnBitsReceivedV2 += OnBits;
                _pubSub.OnViewCount += OnViewersCount;
                _pubSub.OnPubSubServiceError += OnPubSubServiceError;
                _pubSub.Connect();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                OnFatalError?.Invoke(this, e);
            }
        });
    }

    public void Disconnect()
    {
        Task.Run(delegate
        {
            _client.OnError -= OnClientError;
            _client.OnConnectionError -= OnClientConnectionError;
            _client.OnMessageReceived -= OnMessage;
            _client.Disconnect();

            _pubSub.OnBitsReceivedV2 -= OnBits;
            _pubSub.OnViewCount -= OnViewersCount;
            _pubSub.Disconnect();
        });
    }

    public event EventHandler<Exception>? OnError;
    public event EventHandler<Exception>? OnFatalError;

    private void OnClientError(object? sender, OnErrorEventArgs e)
    {
        OnError?.Invoke(this, e.Exception);
    }

    private void OnClientConnectionError(object? sender, OnConnectionErrorArgs e)
    {
        OnError?.Invoke(this, new Exception(e.Error.Message));
    }

    private void OnPubSubServiceError(object? sender, OnPubSubServiceErrorArgs e)
    {
        OnError?.Invoke(this, e.Exception);
    }

    public event EventHandler<Enter>? Entered;
    public event EventHandler<Follow>? Followed;

    public event EventHandler<Message>? MessageReceived;
    private void OnMessage(object? _, OnMessageReceivedArgs e)
    {
        var message = e.ChatMessage;
        Debug.WriteLine(message.BadgeInfo.FirstOrDefault());
        App.Current.MainQueue.TryEnqueue(delegate
        {
            var audience = Audience.Create(
                Platforms.Twitch,
                message.UserId,
                userName: message.Username,
                displayName: message.DisplayName,
                badges: message.Badges.Select(badge => Badge.Create(
                    Platforms.Twitch,
                    $"{message.Channel}.{badge.Key}",
                    Convert.ToInt32(badge.Value),
                    displayName: badge.Key)).ToList(),
                type: message.IsBroadcaster ? AudienceTypes.Host : message.IsModerator ? AudienceTypes.ChannelModerator : message.IsStaff ? AudienceTypes.PlatformStaff : AudienceTypes.General);

            var id = message.Id;
            var payload = new Message(
                Platforms.Twitch,
                id,
                DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(message.TmiSentTs)),
                audience,
                StringContent.Create(message.Message),
                color: message.ColorHex,
                emotes: message.EmoteSet.Emotes.Select(emote => Emote.Create(
                    Platforms.Twitch,
                    emote.Name,
                    emote.ImageUrl,
                    id: emote.Id)).ToList(),
                replyTo: Db.Default.Realm.Find<Message>(Message.GetId(Platforms.Twitch, id)));

            GetAvatar(audience);
            MessageReceived?.Invoke(this, payload);
        });
    }

    public event EventHandler<SuperChat>? SuperChatReceived;

    public event EventHandler<Gift>? GiftReceived;
    private void OnBits(object? _, OnBitsReceivedV2Args e)
    {
        App.Current.MainQueue.TryEnqueue(delegate
        {
            var audience = e.IsAnonymous ? null : Audience.Create(
                Platforms.Twitch,
                e.UserId,
                userName: e.UserName);

            var sku = new Sku(
                Platforms.Twitch,
                Constants.TwitchBitsSkuId,
                0)
            {
                Amount = Constants.TwitchBitUnitAmount,
                Currency = CurrencyCodesResolver.GetCodeByNumber(Constants.CurrencyCodeUsd),
                DisplayName = StringContent.Create(Constants.TwitchBitsSkuDisplayName),
            };

            var payload = new Gift(
                Platforms.Twitch,
                e.Time,
                sku,
                note: StringContent.Create(e.ChatMessage),
                count: e.TotalBitsUsed,
                sender: audience);

            GetAvatar(audience);
            GiftReceived?.Invoke(this, payload);
        });
    }

    public event EventHandler<Membership>? MembershipReceived;
    private void OnMembership(object? _, OnChannelSubscriptionArgs e)
    {
        var membership = e.Subscription;

        App.Current.MainQueue.TryEnqueue(delegate
        {
            var isGift = membership.IsGift ?? false;
            var userId = membership.UserId;
            var displayName = membership.DisplayName;
            var username = membership.Username;

            var audience = Audience.Create(
                Platforms.Twitch,
                isGift ? membership.RecipientId : userId,
                userName: isGift ? membership.RecipientName : username,
                displayName: isGift ? membership.RecipientDisplayName : displayName);

            var tier = membership.SubscriptionPlan;
            var sku = new Sku(
                Platforms.Twitch,
                tier.ToString(),
                Constants.TwitchSubscriptionPlanToLevelMap[tier])
            {
                Amount = Constants.TwitchSubscriptionPlanToAmountMap[tier],
            };

            var count = membership.Months ?? 1;
            var giftedBy = isGift
                ? Audience.Create(
                    Platforms.Twitch,
                    userId,
                    username,
                    displayName)
                : null;
            var payload = new Membership(
                Platforms.Twitch,
                sku,
                sender: audience,
                note: StringContent.Create(membership.SubMessage.Message),
                start: membership.Time,
                end: membership.Time.AddMonths(count),
                count: count,
                giftedBy: giftedBy);

            GetAvatar(audience);
            GetAvatar(giftedBy);
            MembershipReceived?.Invoke(this, payload);
        });
    }

    public event EventHandler<ViewersCount>? ViewersCountChanged;
    private void OnViewersCount(object? _, OnViewCountArgs args)
    {
        App.Current.MainQueue.TryEnqueue(delegate
        {
            var payload = new ViewersCount(
                Platforms.Twitch,
                args.Viewers,
                DateTimeOffset.Parse(args.ServerTime));

            ViewersCountChanged?.Invoke(this, payload);
        });
    }

    // Handle avatars
    private readonly List<string> _userWithAvatars = new();
    private void GetAvatar(Audience? audience = null)
    {
        if (audience == null) return;

        var id = audience.Id;
        if (_userWithAvatars.Contains(id)) return;

        try
        {
            App.Current.MainQueue.TryEnqueue(async delegate
            {
                var response = await _api.Helix.Users.GetUsersAsync(new List<string> { audience.UserId });
                var data = response.Users.FirstOrDefault();
                if (data == null) return;

                // ReSharper disable once MethodHasAsyncOverload
                Db.Default.Realm.Write(delegate
                {
                    audience.Avatar = ImageContent.Create(
                        Platforms.Twitch,
                        data.ProfileImageUrl);
                });
            });

            WeakReferenceMessenger.Default.Send(new AudienceUpdateEventMessage(audience));
            _userWithAvatars.Add(id);
        }
        catch (Exception e)
        {
            OnError?.Invoke(this, e);
            GetAvatar(audience);
        }
    }
}
