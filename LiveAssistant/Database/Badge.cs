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

using LiveAssistant.Common;
using LiveAssistant.Common.Types;
using LiveAssistant.Protocols.Data.Models;
using Realms;
#pragma warning disable CS8618

namespace LiveAssistant.Database;

internal class Badge : RealmObject
{
    // ReSharper disable MemberCanBePrivate.Global
    [PrimaryKey] public string Id { get; private set; }
    public int Platform { get; private set; }
    public string? DisplayName { get; set; }
    public ImageContent? Image { get; set; } = null;
    public int Level { get; private set; } = 0;
    public string? Color { get; set; } = null;
    // ReSharper restore MemberCanBePrivate.Global

    private static string GetId(Platforms platform, string id, int level) => Helpers.GetPlatformPrefixedId(platform, $"{id}.{level}", "badge");

    public Badge() { }

    private Badge(
        Platforms platform,
        string id,
        int level = 0)
    {
        Id = GetId(platform, id, level);
        Platform = (int)platform;
        Level = level;
    }

    public static Badge Create(
        Platforms platform,
        string id,
        int level = 0,
        string? displayName = null,
        string? image = null,
        string? color = null)
    {
        var existing = Db.Default.Realm.Find<Badge>(GetId(platform, id, level));
        var badge = new Badge(
            platform,
            id,
            level);

        Db.Default.Realm.Write(delegate
        {
            if (existing == null) Db.Default.Realm.Add(badge);

            if (displayName != null) (existing ?? badge).DisplayName = displayName;
            if (image != null) (existing ?? badge).Image = ImageContent.Create(platform, image);
            if (color != null) (existing ?? badge).Color = color;
        });

        return existing ?? badge;
    }

    [Ignored]
    public BadgePayload Payload => new()
    {
        DisplayName = DisplayName,
        Level = Level,
        Image = Image?.Payload,
        Color = Color,
    };
}
