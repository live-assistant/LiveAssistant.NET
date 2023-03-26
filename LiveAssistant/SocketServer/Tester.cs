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
using System.Linq;
using System.Timers;
using CommunityToolkit.Mvvm.Messaging;
using LiveAssistant.Common;
using LiveAssistant.Common.Messages;
using LiveAssistant.Common.Types;
using LiveAssistant.Database;
using NLipsum.Core;

namespace LiveAssistant.SocketServer;

internal class Tester
{
    public Tester()
    {
        _enterTimer.Elapsed += OnEnter;
        _followTimer.Elapsed += OnFollow;
        _messageTimer.Elapsed += OnMessage;
        _superChatTimer.Elapsed += OnSuperChat;
        _giftTimer.Elapsed += OnGift;
        _membershipTimer.Elapsed += OnMembership;
        _viewersCountTimer.Elapsed += OnViewersCount;
        _captionTimer.Elapsed += OnCaption;
        _heartRateTimer.Elapsed += OnHeartRate;
    }

    private readonly Random _random = new();
    private readonly LipsumGenerator _generator = new();

    private string RandomId() => $"{DateTimeOffset.Now.Ticks}{_random.Next()}{Helpers.GetUniqueKey(24)}";

    private Platforms RandomPlatform() => (Platforms)_random.Next(0, 2);

    private readonly string[] _diceBearStyles =
    {
        "open-peeps",
        "lorelei-neutral",
        "shapes",
        "thumbs",
    };

    private ImageContent RandomAvatar(
        Platforms platform)
    {
        var style = _random.Next(0, _diceBearStyles.Length - 1);
        var seed = _generator.RandomWord();

        return ImageContent.Create(
            platform,
            $"https://api.dicebear.com/5.x/{_diceBearStyles[style]}/svg?seed={seed}");
    }

    private Audience RandomAudience(
        Platforms platform)
    {
        var audience = new Audience(
            platform,
            RandomId())
        {
            Username = StringContent.Create(string.Join(" ", _generator.GenerateWords(2))),
            Avatar = RandomAvatar(platform),
            Badges =
            {
                Badge.Create(
                    platform,
                    RandomId(),
                    level: _random.Next(1, 20)),
            },
            Type = _random.Next(0, 4),
        };

        return audience;
    }

    private Sku RandomSku(
        Platforms platform)
    {
        var sku = new Sku(
            platform,
            RandomId(),
            _random.Next(0, 5))
        {
            DisplayName = StringContent.Create(string.Join(" ", _generator.GenerateWords(2))),
            Image = RandomAvatar(platform),
            Amount = (float)_random.NextDouble() * 100,
        };

        return sku;
    }

    // Enter
    private readonly Timer _enterTimer = new();
    private void OnEnter(object? sender, ElapsedEventArgs e)
    {
        var platform = RandomPlatform();

        App.Current.MainQueue.TryEnqueue(delegate
        {
            var enter = new Enter(
                platform,
                RandomAudience(platform));

            WeakReferenceMessenger.Default.Send(new EnterEventMessage(enter));
        });

        _enterTimer.Interval = _random.Next(4000, 10000);
        _enterTimer.Start();
    }

    // Follow
    private readonly Timer _followTimer = new();
    private void OnFollow(object? sender, ElapsedEventArgs e)
    {
        var platform = RandomPlatform();

        App.Current.MainQueue.TryEnqueue(delegate
        {
            var enter = new Follow(
                platform,
                RandomAudience(platform));

            WeakReferenceMessenger.Default.Send(new FollowEventMessage(enter));
        });

        _followTimer.Interval = _random.Next(10000, 30000);
        _followTimer.Start();
    }

    // Message
    private readonly Timer _messageTimer = new();
    private void OnMessage(object? _, ElapsedEventArgs e)
    {
        var platform = RandomPlatform();

        App.Current.MainQueue.TryEnqueue(delegate
        {
            var message = new Message(
                platform,
                RandomId(),
                DateTimeOffset.Now,
                RandomAudience(platform),
                StringContent.Create(_generator.GenerateSentences(1).FirstOrDefault() ?? ""));

            WeakReferenceMessenger.Default.Send(new MessageEventMessage(message));
        });

        _messageTimer.Interval = _random.Next(250, 5000);
        _messageTimer.Start();
    }

    // Super chat
    private readonly Timer _superChatTimer = new();
    private void OnSuperChat(object? sender, ElapsedEventArgs e)
    {
        var platform = RandomPlatform();

        App.Current.MainQueue.TryEnqueue(delegate
        {
            var superChat = new SuperChat(
                platform,
                RandomId(),
                RandomSku(platform),
                RandomAudience(platform),
                DateTimeOffset.Now,
                StringContent.Create(string.Join(" ", _generator.GenerateSentences(2))),
                DateTimeOffset.Now,
                DateTimeOffset.Now.AddSeconds(_random.Next(30, 30000)));

            WeakReferenceMessenger.Default.Send(new SuperChatEventMessage(superChat));
        });

        _superChatTimer.Interval = _random.Next(3000, 10000);
        _superChatTimer.Start();
    }

    // Gift
    private readonly Timer _giftTimer = new();
    private void OnGift(object? sender, ElapsedEventArgs e)
    {
        var platform = RandomPlatform();

        App.Current.MainQueue.TryEnqueue(delegate
        {
            var gift = new Gift(
                platform,
                DateTimeOffset.Now,
                RandomSku(platform),
                note: StringContent.Create(_generator.GenerateSentences(1).FirstOrDefault() ?? ""),
                id: RandomId(),
                count: _random.Next(1, 100),
                sender: RandomAudience(platform));

            WeakReferenceMessenger.Default.Send(new GiftEventMessage(gift));
        });

        _giftTimer.Interval = _random.Next(1000, 5000);
        _giftTimer.Start();
    }

    // Membership
    private readonly Timer _membershipTimer = new();
    private void OnMembership(object? sender, ElapsedEventArgs e)
    {
        var platform = RandomPlatform();
        var count = _random.Next(1, 12);

        App.Current.MainQueue.TryEnqueue(delegate
        {
            var membership = new Membership(
                platform,
                RandomSku(platform),
                id: RandomId(),
                sender: RandomAudience(platform),
                note: StringContent.Create(_generator.GenerateSentences(1).FirstOrDefault() ?? ""),
                start: DateTimeOffset.Now,
                end: DateTimeOffset.Now.AddMonths(count),
                count: count);

            WeakReferenceMessenger.Default.Send(new MembershipEventMessage(membership));
        });

        _membershipTimer.Interval = _random.Next(10000, 30000);
        _membershipTimer.Start();
    }

    // Viewers count
    private readonly Timer _viewersCountTimer = new()
    {
        Interval = 50000,
        AutoReset = true,
    };
    private void OnViewersCount(object? sender, ElapsedEventArgs e)
    {
        App.Current.MainQueue.TryEnqueue(delegate
        {
            WeakReferenceMessenger.Default.Send(new ViewersCountEventMessage(new ViewersCount(
                RandomPlatform(),
                _random.Next(90, 150),
                DateTimeOffset.Now)));
        });
    }

    // Caption
    private readonly Timer _captionTimer = new();
    private void OnCaption(object? sender, ElapsedEventArgs e)
    {
        var duration = _random.Next(2000, 3500);

        App.Current.MainQueue.TryEnqueue(delegate
        {
            WeakReferenceMessenger.Default.Send(new CaptionEventMessage(new Caption(
                StringContent.Create(_generator.GenerateSentences(1).FirstOrDefault() ?? ""),
                DateTimeOffset.Now,
                DateTimeOffset.Now.AddSeconds(duration))));
        });

        _captionTimer.Interval = duration + _random.Next(500, 1000);
        _captionTimer.Start();
    }

    // Heart rate
    private readonly Timer _heartRateTimer = new()
    {
        Interval = 2500,
        AutoReset = true,
    };
    private void OnHeartRate(object? sender, ElapsedEventArgs e)
    {
        App.Current.MainQueue.TryEnqueue(delegate
        {
            WeakReferenceMessenger.Default.Send(new HeartRateEventMessage(new HeartRate(_random.Next(60, 90), DateTimeOffset.Now)));
        });
    }

    public void Start()
    {
        _enterTimer.Start();
        _followTimer.Start();
        _messageTimer.Start();
        _superChatTimer.Start();
        _giftTimer.Start();
        _membershipTimer.Start();
        _viewersCountTimer.Start();
        _captionTimer.Start();
        _heartRateTimer.Start();
    }

    public void Stop()
    {
        _enterTimer.Stop();
        _followTimer.Stop();
        _messageTimer.Stop();
        _superChatTimer.Stop();
        _giftTimer.Stop();
        _membershipTimer.Stop();
        _viewersCountTimer.Stop();
        _captionTimer.Stop();
        _heartRateTimer.Stop();
    }
}
