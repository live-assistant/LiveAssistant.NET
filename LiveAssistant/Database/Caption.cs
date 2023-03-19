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
using LiveAssistant.Database.Interface;
using LiveAssistant.Protocols.Data.Models;
using MongoDB.Bson;
using Realms;
// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable CS8618

namespace LiveAssistant.Database;

internal class Caption : RealmObject, ITimeSpan
{
    [PrimaryKey] public ObjectId Id { get; } = ObjectId.GenerateNewId();
    public DateTimeOffset StartTimestamp { get; private set; }
    public DateTimeOffset EndTimestamp { get; private set; }
    public StringContent Content { get; private set; }

    public Caption() { }

    public Caption(
        StringContent content,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        Content = content;
        StartTimestamp = start;
        EndTimestamp = end;
    }

    [Ignored]
    public CaptionPayload Payload => new()
    {
        Start = (int)StartTimestamp.ToUnixTimeMilliseconds(),
        End = (int)EndTimestamp.ToUnixTimeMilliseconds(),
        Content = Content.Payload,
    };
}
