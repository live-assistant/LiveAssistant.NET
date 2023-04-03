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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using LiveAssistant.Common;
using LiveAssistant.Database;
using Microsoft.UI.Xaml;
using Realms;

namespace LiveAssistant.Components;

internal sealed partial class SessionViewer : INotifyPropertyChanged
{
    public SessionViewer()
    {
        InitializeComponent();
    }

    public Session? Session
    {
        get => (Session)GetValue(SessionProperty);
        set
        {
            SetValue(SessionProperty, value);

            SetupDurationTimer();

            OnPropertyChanged(nameof(Follows));
            OnPropertyChanged(nameof(IsFollowsEmpty));
            OnPropertyChanged(nameof(NewFollowsCount));

            OnPropertyChanged(nameof(Messages));
            OnPropertyChanged(nameof(IsMessagesEmpty));
            OnPropertyChanged(nameof(MessagesCount));

            OnPropertyChanged(nameof(SuperChats));
            OnPropertyChanged(nameof(IsSuperChatsEmpty));
            OnPropertyChanged(nameof(SuperChatsCount));

            OnPropertyChanged(nameof(Gifts));
            OnPropertyChanged(nameof(IsGiftsEmpty));
            OnPropertyChanged(nameof(GiftsCount));

            OnPropertyChanged(nameof(Memberships));
            OnPropertyChanged(nameof(IsMembershipsEmpty));
            OnPropertyChanged(nameof(NewMembersCount));

            OnPropertyChanged(nameof(ViewersCount));

            _followToken?.Dispose();
            _followToken = value?.Follows.SubscribeForNotifications(delegate
            {
                OnPropertyChanged(nameof(Follows));
                OnPropertyChanged(nameof(IsFollowsEmpty));
                OnPropertyChanged(nameof(NewFollowsCount));
            });

            _messageToken?.Dispose();
            _messageToken = value?.Messages.SubscribeForNotifications(delegate
            {
                OnPropertyChanged(nameof(Messages));
                OnPropertyChanged(nameof(IsMessagesEmpty));
                OnPropertyChanged(nameof(MessagesCount));
            });

            _superChatToken?.Dispose();
            _superChatToken = value?.SuperChats.SubscribeForNotifications(delegate
            {
                OnPropertyChanged(nameof(SuperChats));
                OnPropertyChanged(nameof(IsSuperChatsEmpty));
                OnPropertyChanged(nameof(SuperChatsCount));
            });

            _giftToken?.Dispose();
            _giftToken = value?.Gifts.SubscribeForNotifications(delegate
            {
                OnPropertyChanged(nameof(Gifts));
                OnPropertyChanged(nameof(IsGiftsEmpty));
                OnPropertyChanged(nameof(GiftsCount));
            });

            _membershipToken?.Dispose();
            _membershipToken = value?.Memberships.SubscribeForNotifications(delegate
            {
                OnPropertyChanged(nameof(Memberships));
                OnPropertyChanged(nameof(IsMembershipsEmpty));
                OnPropertyChanged(nameof(NewMembersCount));
            });

            _viewersCountToken?.Dispose();
            _viewersCountToken = value?.ViewersCounts.SubscribeForNotifications(delegate
            {
                OnPropertyChanged(nameof(ViewersCount));
            });
        }
    }
    private static readonly DependencyProperty SessionProperty =
        DependencyProperty.Register(nameof(Session), typeof(Session), typeof(SessionViewer), new PropertyMetadata(null));

    public bool IsRecording
    {
        private get { return (bool)GetValue(IsRecordingProperty); }
        set
        {
            SetValue(IsRecordingProperty, value);
            OnPropertyChanged(nameof(ViewersCountHeading));
            SetupDurationTimer();
        }
    }
    private static readonly DependencyProperty IsRecordingProperty =
        DependencyProperty.Register(nameof(IsRecording), typeof(bool), typeof(SessionViewer), new PropertyMetadata(false));

    private Timer? _durationTimer;
    private void SetupDurationTimer()
    {
        if (IsRecording && Session is not null)
        {
            _durationTimer = new Timer
            {
                Interval = 1000,
                AutoReset = true,
            };
            _durationTimer.Elapsed += delegate
            {
                App.Current.MainQueue.TryEnqueue(UpdateDuration);
            };
            _durationTimer.Start();
        }
        else
        {
            _durationTimer?.Stop();
            _durationTimer?.Dispose();
            _durationTimer = null;
        }

        UpdateDuration();
    }
    private void UpdateDuration()
    {
        TimeSpan duration;
        if (Session is null) duration = TimeSpan.Zero;
        else if (IsRecording) duration = DateTimeOffset.Now - Session.StartTimestamp;
        else duration = Session.EndTimestamp - Session.StartTimestamp;

        Duration = duration.ToString(@"hh\:mm\:ss");
    }
    private string _duration = "";
    private string Duration
    {
        get => _duration;
        set => SetField(ref _duration, value);
    }

    private IEnumerable<Follow> Follows => Session?.Follows ?? new ObservableCollection<Follow>(Array.Empty<Follow>());
    private IDisposable? _followToken;
    public bool IsFollowsEmpty => !Follows.Any();
    private int NewFollowsCount => Follows.Count();

    private IEnumerable<Message> Messages => Session?.Messages ?? new ObservableCollection<Message>(Array.Empty<Message>());
    private IDisposable? _messageToken;
    public bool IsMessagesEmpty => !Messages.Any();
    private int MessagesCount => Messages.Count();

    private double _messageBubbleMaxWidth;
    public double MessageBubbleMaxWidth
    {
        get => _messageBubbleMaxWidth;
        private set => SetField(ref _messageBubbleMaxWidth, value);
    }

    private void OnMessagesListSizeChange(object _, SizeChangedEventArgs e)
    {
        MessageBubbleMaxWidth = Math.Min(560, e.NewSize.Width - 72);
    }

    private IEnumerable<SuperChat> SuperChats => Session?.SuperChats ?? new ObservableCollection<SuperChat>(Array.Empty<SuperChat>());
    private IDisposable? _superChatToken;
    public bool IsSuperChatsEmpty => !SuperChats.Any();
    private int SuperChatsCount => SuperChats.Count();

    private IEnumerable<Gift> Gifts => Session?.Gifts ?? new ObservableCollection<Gift>(Array.Empty<Gift>());
    private IDisposable? _giftToken;
    public bool IsGiftsEmpty => !Gifts.Any();
    private int GiftsCount => Gifts.Count();

    private IEnumerable<Membership> Memberships => Session?.Memberships ?? new ObservableCollection<Membership>(Array.Empty<Membership>());
    private IDisposable? _membershipToken;
    public bool IsMembershipsEmpty => !Memberships.Any();
    private int NewMembersCount => Memberships.Count();

    private IDisposable? _viewersCountToken;
    private int ViewersCount
    {
        get
        {
            var counts = Session?.ViewersCounts;
            return IsRecording
                ? (counts?.LastOrDefault()?.Count ?? 0)
                : (counts?.Any() ?? false) ? counts.Select(v => v.Count).Max() : 0;
        }
    }
    private string ViewersCountHeading => (IsRecording ? "SessionViewerSummaryViewersCount" : "SessionViewerSummaryPeakViewersCount").Localize();

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
