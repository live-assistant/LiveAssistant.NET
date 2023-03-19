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
using Windows.Storage;
using LiveAssistant.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace LiveAssistant.Pages;

public sealed partial class SettingsPage
{
    public SettingsPage()
    {
        InitializeComponent();

        foreach ((string? name, string? url) in _licenses)
        {
            PackagesList.Children.Add(new HyperlinkButton
            {
                Content = name,
                NavigateUri = new Uri(url),
            });
        }

        WriteTexts();
    }

    private AppSettingsViewModel _settingsViewModel = App.Current.Services.GetService<AppSettingsViewModel>() ?? throw new NullReferenceException();

    private readonly Dictionary<string, string> _licenses = new()
    {
        { "AWSSDK.Polly", "http://aws.amazon.com/apache2.0/" },
        { "CommunityToolkit.HighPerformance", "https://licenses.nuget.org/MIT" },
        { "CommunityToolkit.Mvvm", "https://licenses.nuget.org/MIT" },
        { "EmbedIO", "https://raw.githubusercontent.com/unosquare/embedio/master/LICENSE" },
        { "FftSharp", "https://licenses.nuget.org/MIT" },
        { "ISO.4217.CurrencyCodes", "https://raw.githubusercontent.com/maisak/iso.resolvers/master/LICENSE" },
        { "LiveAssistant.Protocols", "https://licenses.nuget.org/MIT" },
        { "Microsoft.Extensions.DependencyInjection", "https://licenses.nuget.org/MIT" },
        { "Microsoft.Graphics.Win2D", "https://raw.githubusercontent.com/microsoft/Win2D/reunion_master/LICENSE.txt" },
        { "Microsoft.WindowsAppSDK", "https://raw.githubusercontent.com/microsoft/WindowsAppSDK/main/LICENSE-CODE" },
        { "MimeTypesMap", "https://raw.githubusercontent.com/hey-red/MimeTypesMap/master/LICENSE" },
        { "NAudio", "https://www.nuget.org/packages/NAudio/2.1.0/License" },
        { "NLipsum", "https://opensource.org/licenses/mit" },
        { "PInvoke.Hid", "https://licenses.nuget.org/MIT" },
        { "PInvoke.SHCore", "https://licenses.nuget.org/MIT" },
        { "PInvoke.User32", "https://licenses.nuget.org/MIT" },
        { "RawInput.Sharp", "https://licenses.nuget.org/Zlib" },
        { "Realm", "https://licenses.nuget.org/Apache-2.0" },
        { "Sentry", "https://licenses.nuget.org/MIT" },
        { "System.IO.Pipelines", "https://licenses.nuget.org/MIT" },
        { "System.Speech", "https://licenses.nuget.org/MIT" },
        { "TwitchLib", "https://licenses.nuget.org/MIT" },
        { "Unosquare.Swan", "https://raw.githubusercontent.com/unosquare/swan/master/LICENSE" },
        { "Vortice.XInput", "https://licenses.nuget.org/MIT" },
        { "Websocket.Client", "https://licenses.nuget.org/MIT" },
        { "WinUIEx", "https://licenses.nuget.org/Apache-2.0" },
    };

    private async void WriteTexts()
    {
        var licenseFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///LICENSE"));
        var license = await FileIO.ReadTextAsync(licenseFile);
        LicenseTextBlock.Text = license;

        var noticeFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///NOTICE.txt"));
        var notice = await FileIO.ReadTextAsync(noticeFile);
        NoticeTextBlock.Text = notice;
    }
}
