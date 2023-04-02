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
using Windows.UI;
using ISO._4217;
using LiveAssistant.Common.Types;
using LiveAssistant.Database;
using LiveAssistant.Database.Interface;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;
using EmbedIO;
using System.Collections.Generic;
using System.Linq;

namespace LiveAssistant.Common.Converters;

internal class IsNullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        return value == null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class IsNullToVisibilityInverseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        return value != null ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class BooleanInverseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        if (value is null) return true;
        return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class BooleanToVisibilityInverseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class IsSameValueToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value == parameter;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class IsNullToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class IsNullToBooleanInverseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        return value == null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class DateTimeOffsetToShortTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return ((DateTimeOffset)value).ToLocalTime().ToString("t");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class LocalizedFallbackStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var str = value as string;
        return string.IsNullOrEmpty(str) ? ((string)parameter).Localize() : str;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class IsStringNullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return string.IsNullOrEmpty((string)value) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class EnumStringComparisonToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value.ToString() == (string)parameter ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class ColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return new SolidColorBrush((Color)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}


// Non-common
internal class IsOnAirToStateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ? "ButtonDisconnect".Localize() : "ButtonConnect".Localize();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class PlatformIntToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return new ResourceLoader().GetString($"Platform{(Platforms)(int)value}");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class SkuToAmountStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var sku = (Sku)value;
        var exponent = CurrencyCodesResolver.GetExponentByCode(sku.Currency);
        return sku.Amount.ToString($"N{exponent}");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class HostToDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var host = (Host)value;
        return host.DisplayName?.String ?? host.Username?.String ?? host.ChannelId;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class PeopleInterfaceToDisplayNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var people = (IPeople)value;
        return people.DisplayName?.String ?? people.Username?.String ?? "TermAnonymous".Localize();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class WebServerStateToIsEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (WebServerState)value == WebServerState.Stopped;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

internal class DataTypesToSingleStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var list = (IList<string>)value;
        var localizedList = list.Select(t => $"SocketDataType{t}".Localize());
        return string.Join(", ", localizedList);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
