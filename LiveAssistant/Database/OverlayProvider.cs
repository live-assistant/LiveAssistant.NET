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
#pragma warning disable CS8618

namespace LiveAssistant.Database;

internal class OverlayProvider : RealmObject
{
    [PrimaryKey] public string BasePath { get; private set; }
    public string ConfigUrl { get; private set; }
    public string Name { get; private set; }
    public IList<Overlay> Overlays { get; }

    public OverlayProvider() { }

    private OverlayProvider(
        string configUrl,
        string basePath)
    {
        BasePath = basePath;
    }

    public static OverlayProvider Create(
        string configUrl,
        OverlayProviderPayload data)
    {
        var basePath = data.BasePath;
        var existing = Db.Default.Realm.Find<OverlayProvider>(basePath);

        var provider = new OverlayProvider(configUrl, basePath);

        Db.Default.Realm.Write(delegate
        {
            if (existing is null)
            {
                Db.Default.Realm.Add(provider);
            }

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
