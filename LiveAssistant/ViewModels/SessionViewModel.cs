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
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveAssistant.Common.Connectors;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LiveAssistant.Common;
using LiveAssistant.Common.Messages;
using LiveAssistant.Common.Types;
using LiveAssistant.Database;
using Realms;
using Message = LiveAssistant.Database.Message;

namespace LiveAssistant.ViewModels;

internal class SessionViewModel : ObservableObject
{
    public SessionViewModel()
    {
        PickInitialHost();

        Hosts.SubscribeForNotifications(delegate
        {
            OnPropertyChanged(nameof(HasHost));
            PickInitialHost();
        });

        // Handle events
        WeakReferenceMessenger.Default.Register<MarkEventMessage>(this, (_, m) =>
        {
            if (ActiveSession is null) return;
            Db.Default.Realm.Write(delegate
            {
                ActiveSession.Marks.Add(m.Value);
            });
        });

        WeakReferenceMessenger.Default.Register<CaptionEventMessage>(this, (_, m) =>
        {
            if (ActiveSession == null) return;
            Db.Default.Realm.Write(delegate
            {
                ActiveSession.Captions.Add(m.Value);
            });
        });

        WeakReferenceMessenger.Default.Register<HeartRateEventMessage>(this, (_, m) =>
        {
            if (ActiveSession == null) return;
            Db.Default.Realm.Write(delegate
            {
                ActiveSession.HeartRates.Add(m.Value);
            });
        });
    }

    private readonly AppSettings _appSettings = AppSettings.Get();
    public readonly IQueryable<Host> Hosts = Db.Default.Realm.All<Host>();
    public bool HasHost => Hosts.Any();

    private void PickInitialHost()
    {
        if (!HasHost || ActiveHost is not null) return;

        ActiveHost = Hosts.First();
        OnPropertyChanged(nameof(ActiveHost));
    }

    public Host? ActiveHost
    {
        get => _appSettings.ActiveHost;
        set
        {
            Db.Default.Realm.Write(delegate
            {
                _appSettings.ActiveHost = value;
            });
        }
    }

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            SetProperty(ref _isConnected, value);
            WeakReferenceMessenger.Default.Send(new SessionIsConnectedChangedMessage(value));

            if (value)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }
    }

    private Session? _activeSession;
    public Session? ActiveSession
    {
        get => _activeSession;
        private set
        {
            SetProperty(ref _activeSession, value);
        }
    }

    private IConnector? _connector;

    private void Start()
    {
        if (ActiveHost is null) return;

        Db.Default.Realm.Write(delegate
        {
            var session = new Session(
                (Platforms)ActiveHost.Platform,
                ActiveHost);
            Db.Default.Realm.Add(session);

            ActiveSession = session;
        });

        // Add handlers and connect
        _connector?.Disconnect();
        if (ActiveHost is null) return;
        _connector = (Platforms)ActiveHost.Platform switch
        {
            Platforms.Bilibili => new Common.Connectors.Bilibili.BilibiliConnector(ActiveHost),
            Platforms.Twitch => new TwitchConnector(ActiveHost),
            _ => null,
        };

        if (_connector is null) return;
        _connector.OnError += OnConnectorError;
        _connector.OnFatalError += OnConnectorFatalError;
        _connector.Entered += Entered;
        _connector.Followed += Followed;
        _connector.MessageReceived += MessageReceived;
        _connector.SuperChatReceived += SuperChatReceived;
        _connector.GiftReceived += GiftReceived;
        _connector.MembershipReceived += MembershipReceived;
        _connector.ViewersCountChanged += ViewersCountChanged;
        _connector?.Connect();
    }

    private void Stop()
    {
        var session = ActiveSession;
        UpdateEndTimeStamp();
        ActiveSession = null;

        if (session is not null
            && !session.Marks.Any()
            && !session.Audiences.Any()
            && !session.Messages.Any()
            && !session.SuperChats.Any()
            && !session.Gifts.Any()
            && !session.Memberships.Any()
            && !session.Captions.Any()
            && !session.Enters.Any()
            && !session.Follows.Any())
        {
            Db.Default.Realm.Write(delegate
            {
                Db.Default.Realm.Remove(session);
            });
        }

        // Remove handlers
        if (_connector is null) return;
        _connector.Disconnect();
        _connector.OnError -= OnConnectorError;
        _connector.OnFatalError -= OnConnectorFatalError;
        _connector.Entered -= Entered;
        _connector.MessageReceived -= MessageReceived;
        _connector.SuperChatReceived -= SuperChatReceived;
        _connector.GiftReceived -= GiftReceived;
        _connector.MembershipReceived -= MembershipReceived;
        _connector.ViewersCountChanged -= ViewersCountChanged;
    }

    private void UpdateEndTimeStamp()
    {
        if (ActiveSession is null) return;
        Db.Default.Realm.Write(delegate
        {
            ActiveSession.EndTimestamp = DateTimeOffset.Now;
        });
    }

    private void AddAudience(Audience? audience)
    {
        if (audience is null || ActiveSession is null) return;
        if (ActiveSession.Audiences.Any(au => au.Id == audience.Id)) { return; }
        Db.Default.Realm.Write(delegate
        {
            ActiveSession.Audiences.Add(audience);
        });
    }

    private void OnConnectorFatalError(object? sender, Exception e)
    {
        App.Current.MainQueue.TryEnqueue(delegate
        {
            WeakReferenceMessenger.Default.Send(new ShowInfoBarMessage(Helpers.GetExceptionInfoBar(e)));
            IsConnected = false;
        });
    }

    private static void OnConnectorError(object? sender, Exception e)
    {
        Debug.WriteLine(e);
    }

    private void Entered(object? sender, Enter enter)
    {
        UpdateEndTimeStamp();

        Db.Default.Realm.Write(delegate
        {
            Db.Default.Realm.Add(enter, true);
            ActiveSession?.Enters.Add(enter);
        });

        AddAudience(enter.Audience);

        WeakReferenceMessenger.Default.Send(new EnterEventMessage(enter));
    }

    private void Followed(object? sender, Follow follow)
    {
        UpdateEndTimeStamp();

        Db.Default.Realm.Write(delegate
        {
            Db.Default.Realm.Add(follow, true);
            ActiveSession?.Follows.Add(follow);
        });

        AddAudience(follow.Audience);

        WeakReferenceMessenger.Default.Send(new FollowEventMessage(follow));
    }

    private void MessageReceived(object? _, Message message)
    {
        UpdateEndTimeStamp();

        Db.Default.Realm.Write(delegate
        {
            Db.Default.Realm.Add(message, true);
            ActiveSession?.Messages.Add(message);
        });

        AddAudience(message.Sender);

        WeakReferenceMessenger.Default.Send(new MessageEventMessage(message));
    }

    private void SuperChatReceived(object? _, SuperChat superChat)
    {
        UpdateEndTimeStamp();

        Db.Default.Realm.Write(delegate
        {
            Db.Default.Realm.Add(superChat, true);
            ActiveSession?.SuperChats.Add(superChat);
        });

        AddAudience(superChat.Sender);

        WeakReferenceMessenger.Default.Send(new SuperChatEventMessage(superChat));
    }

    private void GiftReceived(object? _, Gift gift)
    {
        UpdateEndTimeStamp();

        Db.Default.Realm.Write(delegate
        {
            Db.Default.Realm.Add(gift, true);
            ActiveSession?.Gifts.Add(gift);
        });

        AddAudience(gift.Sender);

        WeakReferenceMessenger.Default.Send(new GiftEventMessage(gift));
    }

    private void MembershipReceived(object? _, Membership membership)
    {
        UpdateEndTimeStamp();

        Db.Default.Realm.Write(delegate
        {
            Db.Default.Realm.Add(membership, true);
            ActiveSession?.Memberships.Add(membership);
        });

        AddAudience(membership.Sender);

        WeakReferenceMessenger.Default.Send(new MembershipEventMessage(membership));
    }

    private void ViewersCountChanged(object? sender, ViewersCount viewers)
    {
        UpdateEndTimeStamp();

        Db.Default.Realm.Write(delegate
        {
            Db.Default.Realm.Add(viewers, true);
            ActiveSession?.ViewersCounts.Add(viewers);
        });

        WeakReferenceMessenger.Default.Send(new ViewersCountEventMessage(viewers));
    }
}

internal class SessionIsConnectedChangedMessage : ValueChangedMessage<bool>
{
    public SessionIsConnectedChangedMessage(bool value) : base(value) { }
}
