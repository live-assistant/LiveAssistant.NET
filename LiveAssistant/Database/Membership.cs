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
using LiveAssistant.Common;
using LiveAssistant.Common.Types;
using LiveAssistant.Database.Interface;
using LiveAssistant.Protocols.Data.Models;
using MongoDB.Bson;
using Realms;
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8618

namespace LiveAssistant.Database;

internal class Membership : RealmObject, IMonetization, ITimeSpan
{
    // ReSharper disable MemberCanBePrivate.Global
    [PrimaryKey] public string Id { get; private set; }
    [Indexed] public int Platform { get; private set; }
    [Indexed] public DateTimeOffset Timestamp { get; private set; }
    public Sku Sku { get; private set; }
    public int Count { get; private set; }
    public Audience? Sender { get; private set; }
    public Audience? GiftedBy { get; private set; }
    public StringContent? Note { get; private set; }
    [Indexed] public DateTimeOffset StartTimestamp { get; private set; }
    [Indexed] public DateTimeOffset EndTimestamp { get; private set; }
    // ReSharper restore MemberCanBePrivate.Global

    public Membership() { }

    public Membership(
        Platforms platform,
        Sku sku,
        Audience sender,
        DateTimeOffset start,
        DateTimeOffset end,
        StringContent? note = null,
        string? id = null,
        int count = 1,
        Audience? giftedBy = null)
    {
        Platform = (int)platform;
        Id = Helpers.GetPlatformPrefixedId(
            platform,
            id ?? ObjectId.GenerateNewId().ToString(),
            "membership");
        Sku = sku;
        Timestamp = DateTimeOffset.Now;
        Sender = sender;
        Note = note;
        StartTimestamp = start;
        EndTimestamp = end;
        Count = count;
        GiftedBy = giftedBy;
    }

    [Ignored]
    public MembershipPayload Payload => new()
    {
        Id = Id,
        Timestamp = (int)Timestamp.ToUnixTimeMilliseconds(),
        Sender = Sender?.Payload,
        Sku = Sku.Payload,
        Count = Count,
        Start = (int)StartTimestamp.ToUnixTimeMilliseconds(),
        End = (int)EndTimestamp.ToUnixTimeMilliseconds(),
    };
}
