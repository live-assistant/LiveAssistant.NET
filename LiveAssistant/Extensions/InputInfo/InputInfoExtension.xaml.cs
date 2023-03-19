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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Messaging;
using Linearstar.Windows.RawInput;
using Linearstar.Windows.RawInput.Native;
using LiveAssistant.Common;
using LiveAssistant.Common.Messages;
using LiveAssistant.Pages;
using LiveAssistant.Protocols.Data.Models;
using PInvoke;
using Vortice.XInput;
using WinUIEx.Messaging;
using static PInvoke.User32;
using VirtualKey = Windows.System.VirtualKey;

namespace LiveAssistant.Extensions.InputInfo;

public sealed partial class InputInfoExtension : INotifyPropertyChanged
{
    public InputInfoExtension()
    {
        InitializeComponent();

        _manager.IsRunningChanged += delegate
        {
            ApplyState();
        };
    }

    private readonly ExtensionSettingsManager _manager = new("input-info", new Dictionary<string, string>
    {
        { nameof(IsMouseEnabled), true.ToString() },
        { nameof(IsKeyboardEnabled), true.ToString() },
        { nameof(IsGamePadEnabled), true.ToString() },
    });

    private void ApplyState()
    {
        if (_manager.IsRunning)
        {
            WeakReferenceMessenger.Default.Send(new MainWindowAddRawInputHandlerMessage(OnWindowMessageReceived));
            XInput.SetReporting(true);
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new MainWindowRemoveRawInputHandlerMessage(OnWindowMessageReceived));
            XInput.SetReporting(false);
        }
    }

    // Settings
    public bool IsMouseEnabled
    {
        get => Convert.ToBoolean(_manager.Settings[nameof(IsMouseEnabled)]);
        set
        {
            _manager.SaveSetting(nameof(IsMouseEnabled), value.ToString());
        }
    }

    public bool IsKeyboardEnabled
    {
        get => Convert.ToBoolean(_manager.Settings[nameof(IsKeyboardEnabled)]);
        set
        {
            _manager.SaveSetting(nameof(IsKeyboardEnabled), value.ToString());
        }
    }

    public bool IsGamePadEnabled
    {
        get => Convert.ToBoolean(_manager.Settings[nameof(IsGamePadEnabled)]);
        set
        {
            _manager.SaveSetting(nameof(IsGamePadEnabled), value.ToString());
        }
    }

    // Raw input
    private void OnWindowMessageReceived(object? _, WindowMessageEventArgs args)
    {
        try
        {
            var message = args.Message;
            if (message.MessageId != Constants.WmInput) return;

            var inputData = RawInputData.FromHandle(message.LParam);

            switch (inputData)
            {
                case RawInputMouseData mouse:
                    OnMouseMove(mouse);
                    break;
                case RawInputKeyboardData keyboard:
                    OnKeyDown(keyboard);
                    break;
                case RawInputHidData hid:
                    switch (hid.Device.UsageAndPage.Usage)
                    {
                        case Constants.HidUsageGamePad:
                            OnGamePad(hid);
                            break;
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            WeakReferenceMessenger.Default.Send(new ShowInfoBarMessage(Helpers.GetExceptionInfoBar(e)));
        }
    }

    // Mouse
    private readonly double _screenWidth = GetSystemMetrics(SystemMetric.SM_CXFULLSCREEN);
    private readonly double _screenHeight = GetSystemMetrics(SystemMetric.SM_CYFULLSCREEN);

    private void OnMouseMove(RawInputMouseData data)
    {
        if (!IsMouseEnabled) return;

        var mouse = data.Mouse;
        if (mouse.LastX != 0 || mouse.LastY != 0)
        {
            GetCursorPos(out POINT point);
            var x = point.x / _screenWidth;
            var y = point.y / _screenHeight;

            WeakReferenceMessenger.Default.Send(new MousePositionPayloadMessage(new MousePositionPayload
            {
                X = x,
                Y = y,
            }));
        }

        var payload = new MouseButtonPayload();
        var buttons = mouse.Buttons;
        switch (buttons)
        {
            case RawMouseButtonFlags.LeftButtonDown:
                payload.Left = true;
                break;
            case RawMouseButtonFlags.LeftButtonUp:
                payload.Left = false;
                break;
            case RawMouseButtonFlags.RightButtonDown:
                payload.Right = true;
                break;
            case RawMouseButtonFlags.RightButtonUp:
                payload.Right = false;
                break;
            case RawMouseButtonFlags.MiddleButtonDown:
                payload.Middle = true;
                break;
            case RawMouseButtonFlags.MiddleButtonUp:
                payload.Middle = false;
                break;
            case RawMouseButtonFlags.Button4Down:
                payload.Four = true;
                break;
            case RawMouseButtonFlags.Button4Up:
                payload.Four = false;
                break;
            case RawMouseButtonFlags.Button5Down:
                payload.Five = true;
                break;
            case RawMouseButtonFlags.Button5Up:
                payload.Five = false;
                break;
            case RawMouseButtonFlags.MouseWheel:
            case RawMouseButtonFlags.MouseHorizontalWheel:
            case RawMouseButtonFlags.None:
            default:
                break;
        }

        WeakReferenceMessenger.Default.Send(new MouseButtonPayloadMessage(payload));
    }

    // Keyboard
    private void OnKeyDown(RawInputKeyboardData data)
    {
        if (!IsKeyboardEnabled) return;

        var keyboard = data.Keyboard;
        var virtualKey = (VirtualKey)keyboard.VirutalKey;
        Constants.VirtualKeyToJavaScriptKeyMap.TryGetValue(virtualKey, out string? key);

        if (key == null) return;
        var payload = new KeyboardButtonPayload
        {
            Key = key,
            IsDown = !keyboard.Flags.HasFlag(RawKeyboardFlags.Up),
        };

        WeakReferenceMessenger.Default.Send(new KeyboardButtonPayloadMessage(payload));
    }

    // Game pad
    private void OnGamePad(RawInputHidData data)
    {
        if (!IsGamePadEnabled) return;

        var device = data.Device;
        var vendor = device.VendorId;
        var product = device.ProductId;
        var report = data.Hid.RawData;
        var rawButtons = data.ButtonSetStates.First().ButtonSet.GetStates(report).Select(b => b.IsActive)
            .ToArray();

        var payload = new GamepadPayload();
        // XInput
        if (XInput.GetState(0, out var state))
        {
            var gamepad = state.Gamepad;
            var buttons = gamepad.Buttons;

            payload.LeftX = (gamepad.LeftThumbX + Constants.GamepadAxisMaxValue / 2) / Constants.GamepadAxisMaxValue;
            payload.LeftY = Math.Abs((gamepad.LeftThumbY + Constants.GamepadAxisMaxValue / 2) / Constants.GamepadAxisMaxValue - 1);
            payload.RightX = (gamepad.RightThumbX + Constants.GamepadAxisMaxValue / 2) / Constants.GamepadAxisMaxValue;
            payload.RightY = Math.Abs((gamepad.RightThumbY + Constants.GamepadAxisMaxValue / 2) / Constants.GamepadAxisMaxValue - 1);
            payload.LeftTrigger = (double)gamepad.LeftTrigger / 255;
            payload.RightTrigger = (double)gamepad.RightTrigger / 255;
            payload.Up = buttons.HasFlag(GamepadButtons.DPadUp);
            payload.Down = buttons.HasFlag(GamepadButtons.DPadDown);
            payload.Left = buttons.HasFlag(GamepadButtons.DPadLeft);
            payload.Right = buttons.HasFlag(GamepadButtons.DPadRight);
            payload.LeftThumbstick = buttons.HasFlag(GamepadButtons.LeftThumb);
            payload.RightThumbstick = buttons.HasFlag(GamepadButtons.RightThumb);
            payload.LeftShoulder = buttons.HasFlag(GamepadButtons.LeftShoulder);
            payload.RightShoulder = buttons.HasFlag(GamepadButtons.RightShoulder);
            payload.North = buttons.HasFlag(GamepadButtons.Y);
            payload.South = buttons.HasFlag(GamepadButtons.A);
            payload.West = buttons.HasFlag(GamepadButtons.X);
            payload.East = buttons.HasFlag(GamepadButtons.B);
            payload.Home = rawButtons[10];
            payload.UtilLeft = buttons.HasFlag(GamepadButtons.Back);
            payload.UtilRight = buttons.HasFlag(GamepadButtons.Start);
        }
        else
        {
            // XBOX controllers
            if (vendor == Constants.VendorIdMicrosoft && Constants.ProductIdXboxController.Contains(product))
            {
                // Values
                var values = data.ValueSetStates.Select(v => (double)v.CurrentValues.FirstOrDefault(0)).ToArray();
                var trigger = values.ElementAtOrDefault(4);
                var dPad = (int)values.ElementAtOrDefault(5);
                var leftTrigger = (trigger - Constants.GamepadAxisMaxValue / 2) / Constants.GamepadAxisMaxValue;
                var rightTrigger = (Constants.GamepadAxisMaxValue / 2 - trigger) / Constants.GamepadAxisMaxValue;

                // Buttons
                payload.LeftX = values.ElementAtOrDefault(1) / Constants.GamepadAxisMaxValue;
                payload.LeftY = values.ElementAtOrDefault(0) / Constants.GamepadAxisMaxValue;
                payload.RightX = values.ElementAtOrDefault(3) / Constants.GamepadAxisMaxValue;
                payload.RightY = values.ElementAtOrDefault(2) / Constants.GamepadAxisMaxValue;
                payload.LeftTrigger = Math.Max(0, leftTrigger) * 2;
                payload.RightTrigger = Math.Max(0, rightTrigger) * 2;
                payload.Up = dPad == 1;
                payload.Down = dPad == 3;
                payload.Left = dPad == 5;
                payload.Right = dPad == 7;
                payload.LeftThumbstick = rawButtons[8];
                payload.RightThumbstick = rawButtons[9];
                payload.LeftShoulder = rawButtons[4];
                payload.RightShoulder = rawButtons[5];
                payload.North = rawButtons[3];
                payload.South = rawButtons[0];
                payload.West = rawButtons[2];
                payload.East = rawButtons[1];
                payload.Home = rawButtons[10];
                payload.UtilLeft = rawButtons[6];
                payload.UtilRight = rawButtons[7];
            }
        }

        WeakReferenceMessenger.Default.Send(new GamepadPayloadMessage(payload));
    }

    // VM
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
