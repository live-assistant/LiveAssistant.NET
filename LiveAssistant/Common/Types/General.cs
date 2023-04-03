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

namespace LiveAssistant.Common.Types;

internal enum Platforms
{
    Bilibili = 0,
    Twitch = 1,
    YouTube = 2,

    Test = 200,

    Aws = 100,
    GoogleCloud = 101,
    Azure = 102,
    AlibabaCloud = 103,
    TencentCloud = 104,
}

internal enum AudienceTypes
{
    General = 0,
    ChannelModerator = 1,
    Host = 2,
    PlatformModerator = 3,
    PlatformStaff = 4,
}

internal struct ComboBoxItemValueSet<TV> : IStringable
{
    public string Name;
    public TV Value;

    public override string ToString()
    {
        return Name;
    }
}

internal struct OverlayFieldOptionItem : IStringable
{
    public string Name;
    public string Value;

    public override string ToString()
    {
        return Name;
    }
}
