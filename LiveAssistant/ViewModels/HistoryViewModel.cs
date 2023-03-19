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
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveAssistant.Common;
using LiveAssistant.Database;
using Realms;

namespace LiveAssistant.ViewModels;

internal class HistoryViewModel : ObservableObject
{
    public HistoryViewModel()
    {
        SelectedDate = DateTime.Now;

        _allSessions.SubscribeForNotifications(delegate
        {
            OnPropertyChanged(nameof(IsAllSessionsEmpty));
            OnPropertyChanged(nameof(MinDate));
            OnPropertyChanged(nameof(MaxDate));
        });
    }

    private DateTimeOffset? _selectedDate;
    public DateTimeOffset? SelectedDate
    {
        get => _selectedDate;
        set
        {
            var valueWithFallback = value ?? DateTime.Now;
            if (valueWithFallback == _selectedDate) return;

            SetProperty(ref _selectedDate, valueWithFallback);
            Sessions.Clear();
            var end = valueWithFallback.AddHours(24);
            var sessions = _allSessions.Where(s => s.StartTimestamp >= SelectedDate && s.StartTimestamp < end);
            foreach (var session in sessions)
            {
                Sessions.Add(session);
            }
            OnPropertyChanged(nameof(Sessions));
            OnPropertyChanged(nameof(IsSessionsEmpty));
        }
    }

    private readonly IQueryable<Session> _allSessions = Db.Default.Realm.All<Session>();
    public bool IsAllSessionsEmpty => !_allSessions.Any();
    public DateTime MinDate => _allSessions.FirstOrDefault()?.StartTimestamp.DateTime ?? DateTime.Now;
    public DateTime MaxDate => _allSessions.LastOrDefault()?.EndTimestamp.DateTime ?? DateTime.Now;

    public readonly ObservableCollection<Session> Sessions = new();
    public bool IsSessionsEmpty => !Sessions.Any();

    private Session? _activeSession;
    public Session? ActiveSession
    {
        get => _activeSession;
        set => SetProperty(ref _activeSession, value);
    }
}
