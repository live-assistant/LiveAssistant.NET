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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using Windows.Storage;
using Windows.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Json.Schema;
using Json.Schema.Serialization;
using LiveAssistant.Common;
using LiveAssistant.Common.Messages;
using LiveAssistant.Database;
using LiveAssistant.Pages;
using LiveAssistant.Protocols.Overlay.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Realms;

namespace LiveAssistant.ViewModels;

internal class OverlayViewModel : ObservableObject, IDisposable
{
    public OverlayViewModel()
    {
        // Make sure overlay packages folder exists
        if (!Directory.Exists(Constants.OverlayPackagesFolderPath))
        {
            Directory.CreateDirectory(Constants.OverlayPackagesFolderPath);
        }

        Providers.SubscribeForNotifications(delegate
        {
            OnPropertyChanged(nameof(IsProvidersEmpty));
            PickInitialProvider();
        });

        Provider = Providers.FirstOrDefault();

        _serverSettings.Settings.SubscribeForNotifications(delegate
        {
            OnPropertyChanged(nameof(PreviewUri));
        });

        WeakReferenceMessenger.Default.Register<ShouldAddNewOverlayProviderMessage>(this, (_, message) =>
        {
            _ = LoadProvider(message.Value);
        });

        WeakReferenceMessenger.Default.Register<ShouldAddNewOverlayPackageMessage>(this, (_, message) =>
        {
            _ = AddPackage(message.Value);
        });

        WeakReferenceMessenger.Default.Register<OverlayExplorerUpdateQueryMessage>(this, (_, message) =>
        {
            (string? key, string? value) = message.Value;
            _queries[key] = value;
            OnPropertyChanged(nameof(PreviewUri));

            if (Overlay is null) return;
            Db.Default.Realm.Write(delegate
            {
                Overlay.SavedFields[key] = value;
            });
        });

        WeakReferenceMessenger.Default.Register<MainWindowClosedMessage>(this, delegate
        {
            Dispose();
        });
    }

    private readonly ExtensionSettings _serverSettings = ExtensionSettings.Get(Helpers.GetExtensionId(Constants.ExtensionIdServer));

    public readonly IQueryable<OverlayProvider> Providers = Db.Default.Realm.All<OverlayProvider>();
    public bool IsProvidersEmpty => !Providers.Any();

    public RelayCommand AddProviderCommand => new(delegate
    {
        var input = new TextBox
        {
            PlaceholderText = "OverlayExplorerAddProviderDialogUrlTextBoxPlaceholderText".Localize(),
        };

        var dialog = new ContentDialog
        {
            Title = "OverlayExplorerAddProviderDialogTitle".Localize(),
            Content = input,
            CloseButtonText = "ButtonCancel".Localize(),
            PrimaryButtonText = "ButtonAdd".Localize(),
            DefaultButton = ContentDialogButton.Primary,
        };

        dialog.PrimaryButtonClick += async (_, args) =>
        {
            dialog.IsEnabled = false;
            var deferral = args.GetDeferral();

            try
            {
                await LoadProvider(input.Text);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                WeakReferenceMessenger.Default.Send(new ShowInfoBarMessage(Helpers.GetExceptionInfoBar(e)));
            }

            deferral.Complete();
        };

        WeakReferenceMessenger.Default.Send(new ShowContentDialogMessage(dialog));
    });

    public RelayCommand<OverlayProvider> ReloadProviderCommand => new(async delegate(OverlayProvider? provider)
    {
        if (provider?.ConfigUrl is null) return;

        try
        {
            await LoadProvider(provider.ConfigUrl);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            WeakReferenceMessenger.Default.Send(new ShowInfoBarMessage(Helpers.GetExceptionInfoBar(e)));
        }
    });

    public RelayCommand<OverlayProvider> RemoveProviderCommand => new(delegate (OverlayProvider? provider)
    {
        if (provider is null) return;
        if (Equals(Provider, provider))
        {
            Overlay = null;
            Provider = null;
        }

        if (provider.IsPackage)
        {
            Directory.Delete($"{Constants.OverlayPackagesFolderPath}\\{provider.ProductId}", true);
        }

        Db.Default.Realm.Write(delegate
        {
            foreach (var overlay in provider.Overlays)
            {
                if (overlay is null) continue;

                foreach (var field in overlay.Fields)
                {
                    if (field is null) continue;

                    Db.Default.Realm.Remove(field);
                }

                Db.Default.Realm.Remove(overlay);
            }

            Db.Default.Realm.Remove(provider);
        });
    });

    private readonly HttpClient _httpClient = new();

    private static readonly JsonSerializerOptions ProviderJsonSerializerOptions = new JsonSerializerOptions
    {
        IncludeFields = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new ValidatingJsonConverter
            {
                OutputFormat = OutputFormat.List,
            },
            new JsonStringEnumConverter(),
        },
    };

    private static OverlayProviderPayload ParseOverlayProviderPayload(string source)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<OverlayProviderPayload>(source, ProviderJsonSerializerOptions) ?? throw new NullReferenceException();
            return payload;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            throw new Exception("OverlayExceptionParseProvider".Localize());
        }
    }

    private async Task LoadProvider(string url)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(new Uri(url));
            var payload = ParseOverlayProviderPayload(response);
            if (string.IsNullOrEmpty(payload.BasePath))
            {
                throw new Exception("OverlayExceptionParseProvider".Localize());
            }

            OverlayProvider.Create(false, payload, configUrl: url);
        }
        catch (Exception e)
        {
            WeakReferenceMessenger.Default.Send(new ShowInfoBarMessage(Helpers.GetExceptionInfoBar(e)));
        }
    }

    private static async Task AddPackage(IStorageItem pack)
    {
        try
        {
            if (Directory.Exists(Constants.OverlayPackagesTempFolderPath))
            {
                Directory.Delete(Constants.OverlayPackagesTempFolderPath, true);
            }
            Directory.CreateDirectory(Constants.OverlayPackagesTempFolderPath);

            // Extract to temp folder
            ZipFile.ExtractToDirectory(pack.Path, Constants.OverlayPackagesTempFolderPath);
            var configString = await File.ReadAllTextAsync(Constants.OverlayPackagesTempConfigFilePath);

            // Get provider
            var payload = ParseOverlayProviderPayload(configString);
            payload.BasePath = $"http://localhost:{{port}}/{payload.ProductId}/{payload.BasePath}";

            // Move package
            var packFolderPath = $"{Constants.OverlayPackagesFolderPath}\\{payload.ProductId}\\";
            if (Directory.Exists(packFolderPath))
            {
                Directory.Delete(packFolderPath, true);
            }
            Directory.Move(Constants.OverlayPackagesTempStaticFolderPath, packFolderPath);
            OverlayProvider.Create(true, payload);

            // Clear temp
            Directory.Delete(Constants.OverlayPackagesTempFolderPath, true);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            WeakReferenceMessenger.Default.Send(new ShowInfoBarMessage(Helpers.GetExceptionInfoBar(e)));
        }
    }

    // Provider
    private OverlayProvider? _provider;
    public OverlayProvider? Provider
    {
        get => _provider;
        set
        {
            SetProperty(ref _provider, value);
            OnPropertyChanged(nameof(Overlays));
            OnPropertyChanged(nameof(IsOverlaysEmpty));
            Overlay = null;
        }
    }

    private void PickInitialProvider()
    {
        if (IsProvidersEmpty || Provider is not null) return;

        Provider = Providers.First();
    }

    // Overlay
    public IEnumerable<Overlay> Overlays => Provider?.Overlays ?? new ObservableCollection<Overlay>(Array.Empty<Overlay>());
    public bool IsOverlaysEmpty => !Overlays.Any();

    private Overlay? _overlay;
    public Overlay? Overlay
    {
        get => _overlay;
        set
        {
            SetProperty(ref _overlay, value);
            OverlayChanged?.Invoke(this, value);
            OnPropertyChanged(nameof(IsFieldsEmpty));

            _queries.Clear();
            if (value is not null)
            {
                var backgroundColor = value.PreviewBackgroundColor;
                BackgroundColor = string.IsNullOrEmpty(backgroundColor) ? Colors.White : Helpers.ConvertHexColorToUiColor(backgroundColor);

                foreach ((string? key, string? v) in value.SavedFields)
                {
                    _queries[key] = v;
                }
            }

            OnPropertyChanged(nameof(PreviewUri));
        }
    }
    public event EventHandler<Overlay?>? OverlayChanged;

    // Fields
    private readonly Dictionary<string, string> _queries = new();
    public bool IsFieldsEmpty => !Overlay?.Fields.Any() ?? true;

    // Preview
    public Uri PreviewUri
    {
        get
        {
            if (Provider is null || Overlay is null) return new Uri("about:blank");

            var passwordTemplate = $"{{{Constants.ExtensionSettingKeySocketServerPassword.ToLower()}}}";
            var password = _serverSettings.Settings[Constants.ExtensionSettingKeySocketServerPassword];

            var basePath = Provider.BasePath
                .Replace(Constants.OverlayPreviewUriPortTemplate, Constants.ServerPort.ToString())
                .Replace(passwordTemplate, password);

            var queryValues = _queries.Select(pair => $"{pair.Key}={HttpUtility.UrlEncode(pair.Value.ReplaceLineEndings("\n"))}").ToList();

            queryValues.Add($"password={password}");
            queryValues.Add($"port={Constants.ServerPort}");

            var query = string.Join("&", queryValues);
            var uri = new Uri($"{basePath}/{Overlay.Path}?{query}");

            return uri;
        }
    }

    public RelayCommand CopyUrlCommand => new(delegate
    {
        Clipboard.SetText(PreviewUri.ToString());
    });

    private Color _backgroundColor = Colors.White;
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            SetProperty(ref _backgroundColor, value);

            if (Overlay is null) return;
            Db.Default.Realm.Write(delegate
            {
                Overlay.PreviewBackgroundColor = Helpers.ConvertUiColorToHexColor(value);
            });
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

internal class ShouldAddNewOverlayProviderMessage : ValueChangedMessage<string>
{
    public ShouldAddNewOverlayProviderMessage(string value) : base(value) { }
}

internal class OverlayExplorerUpdateQueryMessage : ValueChangedMessage<Tuple<string, string>>
{
    public OverlayExplorerUpdateQueryMessage(Tuple<string, string> value) : base(value) { }
}

internal class ShouldAddNewOverlayPackageMessage : ValueChangedMessage<IStorageItem>
{
    public ShouldAddNewOverlayPackageMessage(IStorageItem value) : base(value) { }
}
