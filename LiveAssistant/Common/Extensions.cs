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

using Microsoft.Windows.ApplicationModel.Resources;

namespace LiveAssistant.Common;

internal static class Extensions
{
    private static readonly ResourceLoader Loader = new();

    /// <summary>
    /// Return the localized strong of the given key.
    /// </summary>
    /// <param name="str"></param>
    /// <returns>Localized string</returns>
    public static string Localize(this string str)
    {
        return Loader.GetString(str);
    }

    public static string ToCamelCase(this string str)
    {
        return char.ToLowerInvariant(str[0]) + str[1..];
    }
}
