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

using Windows.Foundation;
using LiveAssistant.Common;
using LiveAssistant.Protocols.Data.Models;
using Realms;

#pragma warning disable CS8618

namespace LiveAssistant.Database;

internal class StringContent : RealmObject, IStringable
{
    // ReSharper disable MemberCanBePrivate.Global
    [PrimaryKey] public string String { get; private set; }
    [Indexed] public string? Language { get; set; }
    public string? Pronunciation { get; set; }
    [Indexed] public string? Translation { get; set; }
    public string? TranslationLanguage { get; set; }
    // ReSharper restore MemberCanBePrivate.Global

    private StringContent() { }

    private StringContent(
        string content)
    {
        String = content;
    }

    public static StringContent Create(string content)
    {
        return Db.Default.Realm.Find<StringContent>(content) ?? new StringContent(content);
    }

    [Ignored]
    public StringContentPayload Payload => new()
    {
        String = String,
        Language = Language,
        Pronunciation = Pronunciation,
        Translation = Translation,
        TranslationLanguage = TranslationLanguage,
    };

    public override string ToString()
    {
        return String;
    }
}
