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
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LiveAssistant.Common;
using LiveAssistant.Common.Messages;
using LiveAssistant.Database;
using LiveAssistant.Protocols.Data.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LiveAssistant.Extensions.KaraokeStation;

public sealed partial class KaraokeStationExtension : INotifyPropertyChanged
{
    public KaraokeStationExtension()
    {
        InitializeComponent();

        KeywordTextBox.PlaceholderText = DefaultKeyword;

        // Handle the karaoke request message
        WeakReferenceMessenger.Default.Register<MessageEventMessage>(this, OnMessage);

        WeakReferenceMessenger.Default.Register<RequireKaraokeStationMessage>(this, delegate
        {
            SendPayload();
        });
    }

    private readonly ExtensionSettingsManager _manager = new("karaoke-station", new Dictionary<string, string>
    {
        { nameof(TriggerKeyword), DefaultKeyword },
        { nameof(MinimumAudienceLevel), 0.ToString() },
        { nameof(MinimumInterval), 120.ToString() },
        { nameof(RequiresWearingBadge), false.ToString() },
    });

    private static string DefaultKeyword => "ExtensionKaraokeStationKeywordDefault".Localize();
    private string TriggerKeyword
    {
        get => _manager.Settings[nameof(TriggerKeyword)];
        set
        {
            _manager.SaveSetting(nameof(TriggerKeyword), value);
            SendPayload();
        }
    }
    private string RealKeyword => string.IsNullOrEmpty(TriggerKeyword) ? DefaultKeyword : TriggerKeyword;

    private int MinimumAudienceLevel
    {
        get => Convert.ToInt32(_manager.Settings[nameof(MinimumAudienceLevel)]);
        set
        {
            _manager.SaveSetting(nameof(MinimumAudienceLevel), value.ToString());
            SendPayload();
        }
    }

    private int MinimumInterval
    {
        get => Convert.ToInt32(_manager.Settings[nameof(MinimumInterval)]);
        set
        {
            _manager.SaveSetting(nameof(MinimumInterval), value.ToString());
            SendPayload();
        }
    }

    private bool RequiresWearingBadge
    {
        get => Convert.ToBoolean(_manager.Settings[nameof(RequiresWearingBadge)]);
        set
        {
            _manager.SaveSetting(nameof(RequiresWearingBadge), value.ToString());
            SendPayload();
        }
    }

    private void SendPayload()
    {
        if (!_manager.IsEnabled) return;

        WeakReferenceMessenger.Default.Send(new KaraokeStationPayloadMessage(new KaraokeStationPayload
        {
            TriggerKeyword = RealKeyword,
            MinimumInterval = MinimumInterval,
            MinimumAudienceLevel = MinimumAudienceLevel,
            RequiresWearingBadge = RequiresWearingBadge,
            List = _list.Select(item => new KaraokeStationItemPayload
            {
                Name = item.Name,
                Audience = item.Audience?.Payload,
            }).ToArray(),
        }));
    }

    // Handle message
    private void OnMessage(object recipient, MessageEventMessage e)
    {
        var message = e.Value;
        var content = message.Content;
        if (!content.String.StartsWith(RealKeyword + " ")) return;

        var sender = message.Sender;
        if ((sender?.Level ?? 0) < MinimumAudienceLevel) return;
        if (IsAudienceLimitedByInterval(sender)) return;

        _list.Add(new KaraokeItem
        {
            Name = content.String.Replace(RealKeyword, ""),
            Audience = sender,
        });
        OnPropertyChanged(nameof(IsListEmpty));

        SendPayload();
    }

    private readonly ObservableCollection<KaraokeItem> _list = new();
    private bool IsListEmpty => !_list.Any();

    private readonly Dictionary<string, DateTimeOffset?> _timeList = new();

    private string? _song;
    private string? Song
    {
        get => _song;
        set
        {
            SetField(ref _song, value);
            OnPropertyChanged(nameof(IsSongEmpty));
        }
    }
    private bool IsSongEmpty => string.IsNullOrEmpty(Song);

    private void OnSongChanged(object sender, TextChangedEventArgs e)
    {
        Song = ((TextBox)sender).Text;
        OnPropertyChanged(nameof(IsSongEmpty));
    }

    private RelayCommand AddSongCommand => new(delegate
    {
        if (string.IsNullOrEmpty(Song)) return;

        _list.Add(new KaraokeItem
        {
            Name = Song,
        });
        OnPropertyChanged(nameof(IsListEmpty));
        Song = null;

        SendPayload();
    });

    private void OnClickRemoveItem(object sender, RoutedEventArgs _)
    {
        var button = (Button)sender;
        _list.Remove((KaraokeItem)button.DataContext);
        OnPropertyChanged(nameof(IsListEmpty));

        SendPayload();
    }

    private bool IsAudienceLimitedByInterval(Audience? audience)
    {
        if (audience is null) return false;

        var id = audience.Id;
        if (!_timeList.ContainsKey(id))
        {
            _timeList[id] = DateTimeOffset.Now;
            return false;
        }

        var result = _timeList[id] + TimeSpan.FromSeconds(MinimumInterval) > DateTimeOffset.Now;
        if (!result)
        {
            _timeList[id] = DateTimeOffset.Now;
        }
        return result;
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
}

internal struct KaraokeItem
{
    public string Name;
    public Audience? Audience;
}
