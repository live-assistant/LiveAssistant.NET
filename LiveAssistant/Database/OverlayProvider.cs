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
// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8618

namespace LiveAssistant.Database;

internal class OverlayProvider : RealmObject
{
    // ReSharper disable MemberCanBePrivate.Global
    [PrimaryKey] public string ProductId { get; private set; }
    public string BasePath { get; private set; }
    public string? ConfigUrl { get; private set; }
    public string Name { get; private set; }
    public int ProtocolVersion { get; private set; }
    public bool IsPackage { get; private set; }
    public IList<Overlay> Overlays { get; }
    // ReSharper restore MemberCanBePrivate.Global

    public OverlayProvider() { }

    private OverlayProvider(
        string id,
        string basePath)
    {
        ProductId = id;
        BasePath = basePath;
    }

    public static OverlayProvider Create(
        bool isPackage,
        OverlayProviderPayload data,
        string? configUrl = null)
    {
        var basePath = data.BasePath;
        var id = data.ProductId;
        var existing = Db.Default.Realm.Find<OverlayProvider>(id);

        var provider = new OverlayProvider(id, basePath);

        Db.Default.Realm.Write(delegate
        {
            if (existing is null)
            {
                Db.Default.Realm.Add(provider);
            }

            (existing ?? provider).ProtocolVersion = data.ProtocolVersion;
            (existing ?? provider).IsPackage = isPackage;
            (existing ?? provider).ConfigUrl = configUrl;
            (existing ?? provider).Name = data.Name;
            (existing ?? provider).Overlays.Clear();
            foreach (var overlayData in data.Overlays)
            {
                (existing ?? provider).Overlays.Add(Overlay.Create(basePath, overlayData));
            }
        });

        return existing ?? provider;
    }
}
