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
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using HeyRed.Mime;
using LiveAssistant.Common.Types;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using PInvoke;
using static PInvoke.User32;
using Color = Windows.UI.Color;

namespace LiveAssistant.Common;

internal static class Helpers
{
    /// <summary>
    /// Set teh icon for the window title and task view
    /// </summary>
    /// <param name="windowHandle">IntPtr Window Handle</param>
    /// <param name="iconPath">Icon Path, only accepts .ico file</param>
    public static void SetWindowIcon(
        IntPtr windowHandle,
        string iconPath)
    {
        var module = Assembly.GetEntryAssembly()?.GetModules()[0];
        if (module is null) return;
        var smallIcon = LoadImage(
            Marshal.GetHINSTANCE(module),
            iconPath,
            ImageType.IMAGE_ICON,
            32,
            32,
            LoadImageFlags.LR_LOADFROMFILE);

        _ = SendMessage(
            windowHandle,
            WindowMessage.WM_SETICON,
            (IntPtr)1,
            smallIcon);

        var largeIcon = LoadImage(
            Marshal.GetHINSTANCE(module),
            iconPath,
            ImageType.IMAGE_ICON,
            48,
            48,
            LoadImageFlags.LR_LOADFROMFILE);

        _ = SendMessage(
            windowHandle,
            WindowMessage.WM_SETICON,
            IntPtr.Zero,
            largeIcon);
    }

    /// <summary>
    /// Get a unique key with given length
    /// </summary>
    /// <param name="size"></param>
    /// <returns>Unique key</returns>
    public static string GetUniqueKey(int size)
    {
        // ReSharper disable once StringLiteralTypo
        var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

        var data = new byte[4 * size];
        using (var crypto = RandomNumberGenerator.Create())
        {
            crypto.GetBytes(data);
        }
        StringBuilder result = new(size);
        for (var i = 0; i < size; i++)
        {
            var rnd = BitConverter.ToUInt32(data, i * 4);
            var idx = rnd % chars.Length;

            result.Append(chars[idx]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Get the scale of the current display.
    /// </summary>
    /// <param name="windowHandle">Handle of the target window</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static double GetScaleAdjustment(IntPtr windowHandle)
    {
        var wndId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        var displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
        var hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

        // Get DPI.
        var result = SHCore.GetDpiForMonitor(hMonitor, MONITOR_DPI_TYPE.MDT_DEFAULT, out var dpiX, out var _);
        if (result != 0)
        {
            throw new Exception("Could not get DPI for monitor.");
        }

        var scaleFactorPercent = (uint)(((long)dpiX * 100 + (96 >> 1)) / 96);
        return scaleFactorPercent / 100.0;
    }

    /// <summary>
    /// Get a unique platform-prefixed ID for Realm objects.
    /// </summary>
    /// <param name="platform">Platform</param>
    /// <param name="id">ID</param>
    /// <param name="prefix">Prefix</param>
    /// <returns></returns>
    public static string GetPlatformPrefixedId(
        Platforms platform,
        string id,
        string prefix)
    {
        return $"{prefix}.{(int)platform}.{id}";
    }

    /// <summary>
    /// Get the content type string from the file name or URL.
    /// </summary>
    /// <param name="path">The file path or URL</param>
    /// <returns></returns>
    public static string GetContentTypeByExtension(string path)
    {
        return MimeTypesMap.GetMimeType(path);
    }

    /// <summary>
    /// Get the full data URL.
    /// </summary>
    /// <param name="type">Content type</param>
    /// <param name="data">Base64 coded data string</param>
    /// <returns></returns>
    public static string GetDataUrl(
        string? type,
        string data)
    {
        if (type is null) throw new ArgumentNullException();
        return $"data:{type};base64,{data}";
    }

    /// <summary>
    /// Open a URL in the user's default web browser
    /// </summary>
    /// <param name="link"></param>
    public static void OpenLinkInBrowser(string link)
    {
        var info = new ProcessStartInfo(link)
        {
            UseShellExecute = true,
        };
        Process.Start(info);
    }

    /// <summary>
    /// Convert CSS 8-digits hex color to Windows.UI.Color.
    /// </summary>
    /// <param name="hex">CSS color</param>
    /// <returns></returns>
    public static Color ConvertHexColorToUiColor(string hex)
    {
        return Color.FromArgb(
            (byte)int.Parse(hex[6..8], NumberStyles.HexNumber),
            (byte)int.Parse(hex[..2], NumberStyles.HexNumber),
            (byte)int.Parse(hex[2..4], NumberStyles.HexNumber),
            (byte)int.Parse(hex[4..6], NumberStyles.HexNumber));
    }

    /// <summary>
    /// Convert Windows.UI.Color to CSS 8-digits color.
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public static string ConvertUiColorToHexColor(Color color)
    {
        return $"{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
    }

    public static InfoBar GetExceptionInfoBar(Exception e)
    {
        return new InfoBar
        {
            Title = "ExceptionInfoBarTitle".Localize(),
            Severity = InfoBarSeverity.Error,
            Message = e.Message,
        };
    }

    public static string GetExtensionId(string name) => $"{Constants.ExtensionIdPrefix}.{name}";
}
