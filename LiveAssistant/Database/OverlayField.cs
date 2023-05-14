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

using System.Collections.Generic;
using LiveAssistant.Common;
using LiveAssistant.Protocols.Overlay.Models;
using Realms;
using Swan;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
#pragma warning disable CS8618

namespace LiveAssistant.Database;

class OverlayField : RealmObject
{
    // ReSharper disable MemberCanBePrivate.Global
    [PrimaryKey] public string Id { get; private set; }
    public string Key { get; private set; }
    public string Type { get; private set; }
    public string Name { get; set; }
    public string? DefaultValue { get; set; }
    public IDictionary<string, string>? Options { get; }
    // ReSharper restore MemberCanBePrivate.Global

    public OverlayField() { }

    private OverlayField(
        string path,
        OverlayFieldPayload data)
    {
        Id = GetId(path, data);
        Key = data.Key;
        Type = data.Type.ToString();
    }

    private static string GetId(string path, OverlayFieldPayload data) => $"{path}/{data.Key}";

    public static OverlayField Create(
        string path,
        OverlayFieldPayload data)
    {
        var existing = Db.Default.Realm.Find<OverlayField>(GetId(path, data));

        var field = new OverlayField(path, data);

        if (existing is null)
        {
            Db.Default.Realm.Add(field);
        }

        (existing ?? field).Name = data.Name;
        (existing ?? field).DefaultValue = data.DefaultValue;
        (existing ?? field).Options?.Clear();
        data.Options?.ForEach((value, name) => (existing ?? field).Options?.Add(value, name));

        return existing ?? field;
    }
}
