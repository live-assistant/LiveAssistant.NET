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
using LiveAssistant.Common.Types;
using LiveAssistant.Database.Interface;
using MongoDB.Bson;
using Realms;
// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable CollectionNeverQueried.Global
#pragma warning disable CS8618

namespace LiveAssistant.Database;

internal class Session : RealmObject, IPlatformSpecific, ITimeSpan
{
    [PrimaryKey] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    [Indexed] public int Platform { get; private set; }
    public IList<string> TitleList { get; }
    public Host Host { get; private set; }
    public string CoverImageUrl { get; set; }
    public byte[] CoverImageByteArray { get; set; }
    [Indexed] public DateTimeOffset StartTimestamp { get; private set; }
    [Indexed] public DateTimeOffset EndTimestamp { get; set; }
    public IList<Audience> Audiences { get; }
    public IList<Message> Messages { get; }
    public IList<SuperChat> SuperChats { get; }
    public IList<Gift> Gifts { get; }
    public IList<Membership> Memberships { get; }
    public IList<Caption> Captions { get; }
    public IList<HeartRate> HeartRates { get; }
    public IList<ViewersCount> ViewersCounts { get; }
    public IList<Enter> Enters { get; }
    public IList<Follow> Follows { get; }

    public Session() { }

    public Session(
        Platforms platform,
        Host host)
    {
        Platform = (int)platform;
        StartTimestamp = DateTimeOffset.Now;
        EndTimestamp = DateTimeOffset.Now;
        Host = host;
    }
}
