﻿//    Copyright (C) 2023  Live Assistant official Windows app Authors
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
using System.Linq;
using LiveAssistant.Common;
using LiveAssistant.Protocols.Overlay.Models;
using Realms;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable CollectionNeverQueried.Global
#pragma warning disable CS8618

namespace LiveAssistant.Database;

internal class Overlay : RealmObject
{
    // ReSharper disable MemberCanBePrivate.Global
    [PrimaryKey] public string Id { get; private set; }
    public string Path { get; private set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public string? Description { get; set; }
    public IList<OverlayField> Fields { get; }
    public IDictionary<string, string> SavedFields { get; }
    public int? MinWidth { get; set; }
    public int? MaxWidth { get; set; }
    public int? MinHeight { get; set; }
    public int? MaxHeight { get; set; }
    public string? PreviewBackgroundColor { get; set; }
    // ReSharper restore MemberCanBePrivate.Global

    public Overlay() { }

    private Overlay(
        string basePath,
        OverlayPayload data)
    {
        Id = GetId(basePath, data);
        Path = data.Path;
    }

    private static string GetId(string basePath, OverlayPayload data) => $"{basePath}/{data.Path}";

    public static Overlay Create(
        string basePath,
        OverlayPayload data)
    {
        var id = GetId(basePath, data);
        var existing = Db.Default.Realm.Find<Overlay>(id);

        var overlay = new Overlay(basePath, data);

        if (existing is null)
        {
            Db.Default.Realm.Add(overlay);
        }

        (existing ?? overlay).Name = data.Name;
        (existing ?? overlay).Category = data.Category;
        (existing ?? overlay).Description = data.Description;

        (existing ?? overlay).Fields.Clear();
        foreach (var fieldData in data.Fields)
        {
            (existing ?? overlay).Fields.Add(OverlayField.Create(id, fieldData));
        }

        foreach ((string? key, string? _) in (existing ?? overlay).SavedFields)
        {
            if ((existing ?? overlay).Fields.All(f => f.Key != key))
            {
                (existing ?? overlay).SavedFields.Remove(key);
            }
        }

        (existing ?? overlay).MinWidth = data.MinWidth;
        (existing ?? overlay).MaxWidth = data.MaxWidth;
        (existing ?? overlay).MinHeight = data.MinHeight;
        (existing ?? overlay).MaxHeight = data.MaxHeight;
        (existing ?? overlay).PreviewBackgroundColor ??= data.PreviewBackgroundColor;

        return existing ?? overlay;
    }
}
