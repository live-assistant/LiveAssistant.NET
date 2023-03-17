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
// ReSharper disable UnassignedGetOnlyAutoProperty
#pragma warning disable CS8618

namespace LiveAssistant.Database;

internal class Audience : RealmObject, IPlatformSpecific, IPeople
{
    [PrimaryKey] public string Id { get; private set; }
    [Indexed] public string UserId { get; private set; }
    [Indexed] public int Platform { get; private set; }
    public StringContent? Username { get; set; }
    public StringContent? DisplayName { get; set; }
    public ImageContent? Avatar { get; set; }
    public IList<Badge> Badges { get; }
    public int Type { get; set; } = (int)AudienceTypes.General;

    public Audience() { }

    public Audience(
        Platforms platform,
        string userId)
    {
        Id = GetId(platform, userId);
        Platform = (int)platform;
        UserId = userId;
    }

    private static string GetId(Platforms platform, string id) => Helpers.GetPlatformPrefixedId(platform, id, "audience");

    public static Audience Create(
        Platforms platform,
        string userId,
        string? userName = null,
        string? displayName = null,
        string? avatar = null,
        List<Badge>? badges = null,
        AudienceTypes? type = null)
    {
        var existing = Db.Default.Realm.Find<Audience>(GetId(platform, userId));

        var audience = new Audience(
            platform,
            userId);

        Db.Default.Realm.Write(delegate
        {
            if (existing == null) Db.Default.Realm.Add(audience);

            if (userName != null) (existing ?? audience).Username = StringContent.Create(userName);
            if (displayName != null) (existing ?? audience).DisplayName = StringContent.Create(displayName);
            if (avatar != null) (existing ?? audience).Avatar = ImageContent.Create(
                platform,
                avatar);
            if (badges?.Any() ?? false)
            {
                (existing ?? audience).Badges.Clear();
                badges.ForEach(badge => (existing ?? audience).Badges.Add(badge));
            }
            if (type != null) (existing ?? audience).Type = (int)type.Value;
        });

        return existing ?? audience;
    }

    [Ignored]
    public AudiencePayload Payload => new()
    {
        Id = Id,
        Username = Username?.Payload,
        Avatar = Avatar?.Payload,
        Badges = Badges.Select(badge => badge.Payload).ToArray(),
        Level = Level,
        IsModerator = (AudienceTypes)Type is AudienceTypes.ChannelModerator,
        IsMember = IsMember,
    };

    [Ignored] public bool IsMember
    {
        get
        {
            if (!IsManaged) return false;
            var memberships = Db.Default.Realm.All<Membership>().Where(m => m.Sender == this);
            return memberships.Any(m => m.EndTimestamp > DateTimeOffset.Now);
        }
    }

    [Ignored] public int Level => Badges.Any() ? Badges.Max(badge => badge.Level) : 0;
}
