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
using Windows.Foundation;
using LiveAssistant.Common;
using LiveAssistant.Common.Types;
using LiveAssistant.Database.Interface;
using Realms;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnassignedGetOnlyAutoProperty
#pragma warning disable CS8618

namespace LiveAssistant.Database;

internal class Host : RealmObject, IVaultCredentials, IPeople, IStringable
{
    // ReSharper disable MemberCanBePrivate.Global
    [PrimaryKey] public string Id { get; private set; }
    [Indexed] public int Platform { get; private set; }
    public IDictionary<string, string> Secrets { get; }
    public DateTimeOffset AuthTime { get; set; }
    [Required] public string? UserId { get; private set; }
    public StringContent? Username { get; set; }
    public StringContent? DisplayName { get; set; } = null;
    public ImageContent? Avatar { get; set; }
    public string ChannelId { get; set; }
    // ReSharper restore MemberCanBePrivate.Global

    public Host() { }

    public Host(
        Platforms platform,
        string userId)
    {
        Id = Helpers.GetPlatformPrefixedId(
            platform,
            userId,
            "host");
        Platform = (int)platform;
        UserId = userId;
    }

    public override string ToString()
    {
        return DisplayName?.String ?? Username?.String ?? ChannelId;
    }
}
