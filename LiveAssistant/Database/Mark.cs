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
using LiveAssistant.Database.Interface;
using MongoDB.Bson;
using Realms;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace LiveAssistant.Database;

internal class Mark : RealmObject, ITimeBased
{
    // ReSharper disable MemberCanBePrivate.Global
    [PrimaryKey] public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public DateTimeOffset Timestamp { get; private set; }
    public int Type { get; private set; }
    // ReSharper restore MemberCanBePrivate.Global

    public Mark() { }

    private Mark(
        MarkType type)
    {
        Timestamp = DateTimeOffset.Now;
        Type = (int)type;
    }

    public static Mark Create(
        MarkType type)
    {
        var mark = new Mark(type);

        Db.Default.Realm.Write(delegate
        {
            Db.Default.Realm.Add(mark);
        });

        return mark;
    }
}

internal enum MarkType
{
    Highlight = 0,
}
