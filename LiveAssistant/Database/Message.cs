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
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8618

namespace LiveAssistant.Database;

internal class Message : RealmObject, IMessage
{
    // ReSharper disable MemberCanBePrivate.Global
    [PrimaryKey] public string Id { get; private set; }
    [Indexed] public int Platform { get; private set; }
    [Indexed] public DateTimeOffset Timestamp { get; private set; }
    public StringContent Content { get; private set; }
    public IList<Emote> Emotes { get; }
    public string? Color { get; private set; }
    public Audience? Sender { get; private set; }
    public bool IsFlagged { get; set; }
    public float PriorityScore { get; set; }
    public Message? ReplyTo { get; private set; }
    // ReSharper restore MemberCanBePrivate.Global

    public Message() { }

    public Message(
        Platforms platform,
        string id,
        DateTimeOffset time,
        Audience sender,
        StringContent content,
        string? color = null,
        List<Emote>? emotes = null,
        Message? replyTo = null)
    {
        Platform = (int)platform;
        Id = GetId(platform, id);
        Timestamp = time;
        Sender = sender;
        Content = content;
        Color = color;
        ReplyTo = replyTo;

        Emotes = new List<Emote>();
        emotes?.ForEach(emote =>
        {
            if (Emotes.Contains(emote)) return;
            Emotes.Add(emote);
        });
    }

    public static string GetId(Platforms platform, string id) => Helpers.GetPlatformPrefixedId(platform, id, "message");

    public static Message FindById(Platforms platform, string id) => Db.Default.Realm.Find<Message>(GetId(platform, id));

    [Ignored]
    public MessagePayload Payload => new()
    {
        Id = Id,
        Timestamp = (int)Timestamp.ToUnixTimeMilliseconds(),
        Sender = Sender?.Payload,
        Content = Content.Payload,
        Color = Color,
        Emotes = Emotes.Select(e => e.Payload).ToArray(),
    };
}
