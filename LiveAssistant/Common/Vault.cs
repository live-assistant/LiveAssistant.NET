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
using System.Diagnostics;
using Windows.Security.Credentials;

namespace LiveAssistant.Common;

internal class Vault
{
    private static readonly Vault Default = new();

    private Vault()
    {
        _instance = new PasswordVault();
    }

    private readonly PasswordVault _instance;

    public static string GetPassword(string name)
    {
        PasswordCredential? credential = null;

        try
        {
            credential = Default._instance.Retrieve(
                Constants.VaultResourceName,
                name);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }

        return credential?.Password ?? "";
    }

    public static void SetPassword(string name, string password)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        Default._instance.Add(new PasswordCredential(
            Constants.VaultResourceName,
            name,
            password));
    }
}
