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
using Windows.System;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LiveAssistant.Protocols.Data.Models;

namespace LiveAssistant.Extensions.InputInfo;

internal static class Constants
{
    public const uint WmInput = 0x00FF;

    public static readonly Dictionary<VirtualKey, string> VirtualKeyToJavaScriptKeyMap = new()
    {
        { VirtualKey.A, "A" },
        { VirtualKey.B, "B" },
        { VirtualKey.C, "C" },
        { VirtualKey.D, "D" },
        { VirtualKey.E, "E" },
        { VirtualKey.F, "F" },
        { VirtualKey.G, "G" },
        { VirtualKey.H, "H" },
        { VirtualKey.I, "I" },
        { VirtualKey.J, "J" },
        { VirtualKey.K, "K" },
        { VirtualKey.L, "L" },
        { VirtualKey.M, "M" },
        { VirtualKey.N, "N" },
        { VirtualKey.O, "O" },
        { VirtualKey.P, "P" },
        { VirtualKey.Q, "Q" },
        { VirtualKey.R, "R" },
        { VirtualKey.S, "S" },
        { VirtualKey.T, "T" },
        { VirtualKey.U, "U" },
        { VirtualKey.V, "V" },
        { VirtualKey.W, "W" },
        { VirtualKey.X, "X" },
        { VirtualKey.Y, "Y" },
        { VirtualKey.Z, "Z" },
        { VirtualKey.Number0, "0" },
        { VirtualKey.Number1, "1" },
        { VirtualKey.Number2, "2" },
        { VirtualKey.Number3, "3" },
        { VirtualKey.Number4, "4" },
        { VirtualKey.Number5, "5" },
        { VirtualKey.Number6, "6" },
        { VirtualKey.Number7, "7" },
        { VirtualKey.Number8, "8" },
        { VirtualKey.Number9, "9" },
        { VirtualKey.NumberPad0, "0" },
        { VirtualKey.NumberPad1, "1" },
        { VirtualKey.NumberPad2, "2" },
        { VirtualKey.NumberPad3, "3" },
        { VirtualKey.NumberPad4, "4" },
        { VirtualKey.NumberPad5, "5" },
        { VirtualKey.NumberPad6, "6" },
        { VirtualKey.NumberPad7, "7" },
        { VirtualKey.NumberPad8, "8" },
        { VirtualKey.NumberPad9, "9" },
        { VirtualKey.F1, "F1" },
        { VirtualKey.F2, "F2" },
        { VirtualKey.F3, "F3" },
        { VirtualKey.F4, "F4" },
        { VirtualKey.F5, "F5" },
        { VirtualKey.F6, "F6" },
        { VirtualKey.F7, "F7" },
        { VirtualKey.F8, "F8" },
        { VirtualKey.F9, "F9" },
        { VirtualKey.F10, "F10" },
        { VirtualKey.F11, "F11" },
        { VirtualKey.F12, "F12" },
        { VirtualKey.Delete, "Backspace" },
        { VirtualKey.Tab, "Tab" },
        { VirtualKey.Enter, "Enter" },
        { VirtualKey.Shift, "Shift" },
        { VirtualKey.LeftShift, "Shift" },
        { VirtualKey.RightShift, "Shift" },
        { VirtualKey.Control, "Control" },
        { VirtualKey.LeftControl, "Control" },
        { VirtualKey.RightControl, "Control" },
        { VirtualKey.Menu, "Alt" },
        { VirtualKey.LeftMenu, "Alt" },
        { VirtualKey.RightMenu, "Alt" },
        { VirtualKey.Escape, "Escape" },
        { VirtualKey.Space, " " },
        { VirtualKey.Left, "ArrowLeft" },
        { VirtualKey.Up, "ArrowUp" },
        { VirtualKey.Right, "ArrowRight" },
        { VirtualKey.Down, "ArrowDown" },
    };

    public const ushort HidUsageGamePad = 0x05;
    public const ushort HidUsageJoystick = 0x04;

    public const int VendorIdMicrosoft = 1118;
    public static readonly List<int> ProductIdXboxController = new()
    {
        721,
        733,
        736,
        739,
        746,
        765,
        767,
    };
    public const double GamepadAxisMaxValue = 65536;
}

internal class MousePositionPayloadMessage : ValueChangedMessage<MousePositionPayload>
{
    public MousePositionPayloadMessage(MousePositionPayload value) : base(value) { }
}

internal class MouseButtonPayloadMessage : ValueChangedMessage<MouseButtonPayload>
{
    public MouseButtonPayloadMessage(MouseButtonPayload value) : base(value) { }
}

internal class KeyboardButtonPayloadMessage : ValueChangedMessage<KeyboardButtonPayload>
{
    public KeyboardButtonPayloadMessage(KeyboardButtonPayload value) : base(value) { }
}

internal class GamepadPayloadMessage : ValueChangedMessage<GamepadPayload>
{
    public GamepadPayloadMessage(GamepadPayload value) : base(value) { }
}
