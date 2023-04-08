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

using LiveAssistant.Common.Types;
using LiveAssistant.Database;
using LiveAssistant.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Globalization;
using System;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LiveAssistant.Common;
using LiveAssistant.Protocols.Overlay;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Realms;
using WinRT;

namespace LiveAssistant.Pages;

internal sealed partial class OverlayPage
{
    public OverlayPage()
    {
        InitializeComponent();

        ViewModel.OverlayChanged += OnOverlayChanged;

        ViewModel.Providers.SubscribeForNotifications(delegate
        {
            ProvidersMenu.Items.Clear();
            foreach (var provider in ViewModel.Providers)
            {
                ProvidersMenu.Items.Add(new MenuFlyoutItem
                {
                    Text = provider.Name,
                    Command = new RelayCommand(delegate
                    {
                        ViewModel.Provider = provider;
                    }),
                });
            }
        });
    }

    private void OnOverlayChanged(object? sender, Overlay? overlay)
    {
        if (overlay is null) return;

        FieldsPanel.Children.Clear();
        foreach (var field in overlay.Fields)
        {
            var type = Enum.Parse<OverlayFieldType>(field.Type, true);
            var key = field.Key;

            var defaultValue = overlay.SavedFields.TryGetValue(key, out string? savedField) ? savedField : field.DefaultValue;

            switch (type)
            {
                case OverlayFieldType.String:
                {
                    var textBox = new TextBox
                    {
                        Header = field.Name,
                        Text = defaultValue,
                    };
                    textBox.TextChanged += (input, _) =>
                    {
                        SendUpdate(key, ((TextBox)input).Text);
                    };
                    FieldsPanel.Children.Add(textBox);
                    break;
                }
                case OverlayFieldType.Number:
                case OverlayFieldType.Percentage:
                case OverlayFieldType.Degree:
                {
                    var numberBox = new NumberBox
                    {
                        Header = field.Name,
                        Value = Convert.ToDouble(defaultValue),
                        SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                    };
                    // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                    switch (type)
                    {
                        case OverlayFieldType.Percentage:
                            numberBox.Minimum = 0;
                            numberBox.Maximum = 100;
                            break;
                        case OverlayFieldType.Degree:
                            numberBox.Minimum = 0;
                            numberBox.Maximum = 360;
                            break;
                    }

                    numberBox.ValueChanged += (_, args) =>
                    {
                        SendUpdate(key, args.NewValue.ToString(CultureInfo.InvariantCulture));
                    };
                    FieldsPanel.Children.Add(numberBox);
                    break;
                }
                case OverlayFieldType.Bool:
                {
                    var toggleButton = new ToggleButton
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Content = field.Name,
                        IsChecked = Convert.ToBoolean(defaultValue),
                    };
                    toggleButton.Click += (button, _) =>
                        SendUpdate(key, ((ToggleButton)button).IsChecked.ToString()?.ToLowerInvariant() ?? "");
                    FieldsPanel.Children.Add(toggleButton);
                    break;
                }
                case OverlayFieldType.Color:
                {
                    var colorPicker = new ColorPicker
                    {
                        IsMoreButtonVisible = true,
                        IsAlphaEnabled = true,
                        Color = string.IsNullOrEmpty(defaultValue) ? Colors.White : Helpers.ConvertHexColorToUiColor(defaultValue),
                    };
                    colorPicker.ColorChanged += (_, args) =>
                    {
                        SendUpdate(key, Helpers.ConvertUiColorToHexColor(args.NewColor));
                    };
                    FieldsPanel.Children.Add(new DropDownButton
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        Content = field.Name,
                        Flyout = new Flyout
                        {
                            Content = colorPicker,
                        },
                    });
                    break;
                }
                case OverlayFieldType.Date:
                {
                    const string format = "yyyyMMdd";
                        var datePicker = new CalendarDatePicker
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        Header = field.Name,
                        Date = string.IsNullOrEmpty(defaultValue) ? DateTime.Now.Date :
                            DateTime.ParseExact(defaultValue, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal),
                    };
                    datePicker.DateChanged += (_, args) =>
                    {
                        SendUpdate(field.Key, (args.NewDate ?? DateTimeOffset.Now).UtcDateTime.ToString(format));
                    };
                    FieldsPanel.Children.Add(datePicker);
                    break;
                }
                case OverlayFieldType.Time:
                {
                    const string format = "hhmmss";
                    var picker = new TimePicker
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        Header = field.Name,
                        Time = string.IsNullOrEmpty(defaultValue) ? DateTimeOffset.Now.TimeOfDay :
                            DateTimeOffset.ParseExact(defaultValue, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal).TimeOfDay,
                    };
                    picker.TimeChanged += (_, args) =>
                    {
                        SendUpdate(field.Key, args.NewTime.ToString(format));
                    };
                    FieldsPanel.Children.Add(picker);
                    break;
                }
                case OverlayFieldType.Option:
                {
                    if (field.Options is null) break;

                    var options = field.Options.Select(option => new OverlayFieldOptionItem
                    {
                        Name = option.Value,
                        Value = option.Key,
                    }).ToList();
                    var comboBox = new ComboBox
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Header = field.Name,
                        SelectedIndex = options.FindIndex(o => o.Value == defaultValue),
                    };
                    foreach (var item in options)
                    {
                        comboBox.Items.Add(item);
                    }
                    comboBox.SelectionChanged += (_, args) =>
                    {
                        var item = args.AddedItems.FirstOrDefault().As<OverlayFieldOptionItem>();
                        SendUpdate(field.Key, item.Value);
                    };
                    FieldsPanel.Children.Add(comboBox);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private static void SendUpdate(string key, string value)
    {
        WeakReferenceMessenger.Default.Send(
            new OverlayExplorerUpdateQueryMessage(new Tuple<string, string>(key, value)));
    }

    private OverlayViewModel ViewModel => App.Current.Services.GetService<OverlayViewModel>() ?? throw new NullReferenceException();
}
