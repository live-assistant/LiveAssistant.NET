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
            OnPropertyChanged(nameof(Follows));
            OnPropertyChanged(nameof(IsFollowsEmpty));
            OnPropertyChanged(nameof(Messages));
            OnPropertyChanged(nameof(IsMessagesEmpty));
            OnPropertyChanged(nameof(SuperChats));
            OnPropertyChanged(nameof(IsSuperChatsEmpty));
            OnPropertyChanged(nameof(Gifts));
            OnPropertyChanged(nameof(IsGiftsEmpty));
            OnPropertyChanged(nameof(Memberships));
            OnPropertyChanged(nameof(IsMembershipsEmpty));

            _followToken?.Dispose();
            _followToken = value?.Follows.SubscribeForNotifications(delegate
            {
                OnPropertyChanged(nameof(Follows));
                OnPropertyChanged(nameof(IsFollowsEmpty));
            });

            _messageToken?.Dispose();
            _messageToken = value?.Messages.SubscribeForNotifications(delegate
            {
                OnPropertyChanged(nameof(Messages));
                OnPropertyChanged(nameof(IsMessagesEmpty));
            });

            _superChatToken?.Dispose();
            _superChatToken = value?.SuperChats.SubscribeForNotifications(delegate
            {
                OnPropertyChanged(nameof(SuperChats));
                OnPropertyChanged(nameof(IsSuperChatsEmpty));
            });

            _giftToken?.Dispose();
            _giftToken = value?.Gifts.SubscribeForNotifications(delegate
            {
                OnPropertyChanged(nameof(Gifts));
                OnPropertyChanged(nameof(IsGiftsEmpty));
            });

            _membershipToken?.Dispose();
            _membershipToken = value?.Memberships.SubscribeForNotifications(delegate
            {
                OnPropertyChanged(nameof(Memberships));
                OnPropertyChanged(nameof(IsMembershipsEmpty));
            });
        }
    }

    private static readonly DependencyProperty SessionProperty =
        DependencyProperty.Register(nameof(Session), typeof(Session), typeof(SessionViewer), new PropertyMetadata(null));

    private IEnumerable<Follow> Follows => Session?.Follows ?? new ObservableCollection<Follow>(Array.Empty<Follow>());
    private IDisposable? _followToken;
    public bool IsFollowsEmpty => !Follows.Any();

    private IEnumerable<Message> Messages => Session?.Messages ?? new ObservableCollection<Message>(Array.Empty<Message>());
    private IDisposable? _messageToken;
    public bool IsMessagesEmpty => !Messages.Any();

    private IEnumerable<SuperChat> SuperChats => Session?.SuperChats ?? new ObservableCollection<SuperChat>(Array.Empty<SuperChat>());
    private IDisposable? _superChatToken;
    public bool IsSuperChatsEmpty => !SuperChats.Any();

    private IEnumerable<Gift> Gifts => Session?.Gifts ?? new ObservableCollection<Gift>(Array.Empty<Gift>());
    private IDisposable? _giftToken;
    public bool IsGiftsEmpty => !Gifts.Any();

    private IEnumerable<Membership> Memberships => Session?.Memberships ?? new ObservableCollection<Membership>(Array.Empty<Membership>());
    private IDisposable? _membershipToken;
    public bool IsMembershipsEmpty => !Memberships.Any();

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
