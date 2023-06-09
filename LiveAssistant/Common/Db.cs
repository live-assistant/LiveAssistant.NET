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

// ReSharper disable RedundantUsingDirective

using System.IO;
using System.Text;
using Realms;

namespace LiveAssistant.Common;

internal class Db
{
    public static readonly Db Default = new();

    private Db()
    {
        var key = Vault.GetPassword(Constants.VaultUserNameRealmKey);

        if (string.IsNullOrEmpty(key))
        {
            key = Helpers.GetUniqueKey(64);
            Vault.SetPassword(Constants.VaultUserNameRealmKey, key);
        }

        if (!Directory.Exists(Constants.DocumentsBaseFolderPath))
        {
            Directory.CreateDirectory(Constants.DocumentsBaseFolderPath);
        }

        var config = new RealmConfiguration($"{Constants.DocumentsBaseFolderPath}{Constants.VaultRealmName}.realm")
        {
            SchemaVersion = 0,
            ShouldDeleteIfMigrationNeeded = true,
#if !DEBUG
            EncryptionKey = new UTF8Encoding().GetBytes(key),
#endif
            MigrationCallback = (migration, oldVersion) =>
            {

            },
        };

        Realm = Realm.GetInstance(config);
    }

    public readonly Realm Realm;
}
