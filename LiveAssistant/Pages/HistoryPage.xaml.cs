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
using LiveAssistant.Common;
using LiveAssistant.Database;
using LiveAssistant.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinRT;

namespace LiveAssistant.Pages;

internal sealed partial class HistoryPage
{
    public HistoryPage()
    {
        InitializeComponent();
    }

    public HistoryViewModel HistoryViewModel => App.Current.Services.GetService<HistoryViewModel>() ?? throw new NullReferenceException();

    private readonly IQueryable<Session> _sessions = Db.Default.Realm.All<Session>();

    private void OnSelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
    {
        var dates = sender.SelectedDates;
        HistoryViewModel.SelectedDate = dates.FirstOrDefault();
    }

    private void OnCalendarViewDayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
    {
        if (args.InRecycleQueue) return;

        // Set sessions count
        var item = args.Item;
        var start = item.Date;
        var end = start.AddHours(24).Subtract(TimeSpan.FromSeconds(1));
        var sessionsInDay = _sessions.Where(s => s.StartTimestamp >= start && s.StartTimestamp < end);
        item.SetDensityColors(sessionsInDay.ToList().Select(_ => App.Current.Resources["AccentFillColorDefaultBrush"].As<SolidColorBrush>().Color));
        item.IsBlackout = !sessionsInDay.Any();
    }
}