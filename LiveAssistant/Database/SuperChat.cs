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
using System.Linq;
using LiveAssistant.Common;
using LiveAssistant.Common.Types;
using LiveAssistant.Database.Interface;
using LiveAssistant.Protocols.Data.Models;
using Realms;
#pragma warning disable CS8618

namespace LiveAssistant.Database;

internal class SuperChat : RealmObject, IMonetization, IMessage, ITimeSpan
{
    // ReSharper disable MemberCanBePrivate.Global
    [PrimaryKey] public string Id { get; private set; }
    public Audience? Sender { get; private set; }
    public Sku Sku { get; private set; }
    public int Count { get; private set; } = 1;
    public StringContent? Note { get; private set; }
    [Indexed] public DateTimeOffset Timestamp { get; private set; }
    [Indexed] public int Platform { get; private set; }
    public StringContent Content { get; private set; }
    public IList<Emote> Emotes { get; }
    public string? Color { get; private set; }
    [Indexed] public DateTimeOffset StartTimestamp { get; private set; }
    [Indexed] public DateTimeOffset EndTimestamp { get; private set; }
    public bool IsFlagged { get; set; }
    // ReSharper restore MemberCanBePrivate.Global

    public SuperChat() { }

    public SuperChat(
        Platforms platform,
        string id,
        Sku sku,
        Audience sender,
        DateTimeOffset time,
        StringContent content,
        DateTimeOffset start,
        DateTimeOffset end,
        string? color = null,
        List<Emote>? emotes = null)
    {
        Platform = (int)platform;
        Id = Helpers.GetPlatformPrefixedId(
            platform,
            id,
            "superchat");
        Sku = sku;
        Sender = sender;
        Timestamp = time;
        Content = content;
        Color = color;
        StartTimestamp = start;
        EndTimestamp = end;
        Note = null;

        Emotes = new List<Emote>();
        emotes?.ForEach(emote =>
        {
            if (Emotes.Contains(emote)) return;
            Emotes.Add(emote);
        });
    }

    [Ignored]
    public SuperChatPayload Payload => new()
    {
        Id = Id,
        Timestamp = (int)Timestamp.ToUnixTimeMilliseconds(),
        Sender = Sender?.Payload,
        Sku = Sku.Payload,
        Content = Content.Payload,
        Color = Color,
        Emotes = Emotes.Select(e => e.Payload).ToArray(),
        Start = (int)StartTimestamp.ToUnixTimeMilliseconds(),
        End = (int)EndTimestamp.ToUnixTimeMilliseconds(),
    };
}
