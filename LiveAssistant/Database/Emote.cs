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
using LiveAssistant.Database.Interface;
using LiveAssistant.Protocols.Data.Models;
using Realms;
#pragma warning disable CS8618

namespace LiveAssistant.Database;

internal class Emote : RealmObject, IPlatformSpecific
{
    // ReSharper disable MemberCanBePrivate.Global
    [PrimaryKey] public string Id { get; private set; }
    public int Platform { get; private set; }
    public string Keyword { get; private set; }
    public ImageContent Image { get; private set; }
    // ReSharper restore MemberCanBePrivate.Global

    public Emote() { }

    private Emote(
        Platforms platform,
        string keyword,
        string image,
        string? id = null)
    {
        Id = GetId(platform, keyword, id: id);
        Platform = (int)platform;
        Keyword = keyword;
        Image = ImageContent.Create(platform, image);
    }

    private static string GetId(
        Platforms platform,
        string keyword,
        string? id = null) => Helpers.GetPlatformPrefixedId(platform, id ?? keyword, "emote");

    public static Emote Create(
        Platforms platform,
        string keyword,
        string image,
        string? id = null)
    {
        var existing = Db.Default.Realm.Find<Emote>(GetId(platform, keyword, id: id));

        var emote = new Emote(
            platform,
            keyword,
            image,
            id: id);

        if (existing == null)
        {
            Db.Default.Realm.Write(delegate
            {
                Db.Default.Realm.Add(emote);
            });
        }

        return existing ?? emote;
    }

    public EmotePayload Payload => new()
    {
        Id = Id,
        Keyword = Keyword,
        Image = Image.Payload
    };
}
