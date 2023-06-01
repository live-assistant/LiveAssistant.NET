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
using CommunityToolkit.Mvvm.Messaging.Messages;
using LiveAssistant.Common;
using Microsoft.UI.Xaml.Data;

namespace LiveAssistant.Extensions.MediaInfo;

internal class RequireMediaInfoPayloadMessage : ValueChangedMessage<bool>
{
    public RequireMediaInfoPayloadMessage() : base(true) { }
}

internal class RequireMediaPlaybackPayloadMessage : ValueChangedMessage<bool>
{
    public RequireMediaPlaybackPayloadMessage() : base(true) { }
}

internal static class Constants
{
    internal static readonly Dictionary<string, string> AllowedMediaApps = new()
    {
        { "SpotifyAB.SpotifyMusic", "MediaAppNameSpotify".Localize() },
        { "AppleInc.AppleMusic", "MediaAppNameAppleMusic".Localize() },
    };
}

internal class SourceAppUserModelIdToNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var id = (string)value;
        foreach ((string? key, string? name) in Constants.AllowedMediaApps)
        {
            if (id.Contains(key)) return name;
        }
        return id;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
