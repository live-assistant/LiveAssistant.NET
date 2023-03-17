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
using LiveAssistant.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LiveAssistant.Pages;

internal sealed partial class RecorderPage
{
    public RecorderPage()
    {
        InitializeComponent();
    }

    private SessionViewModel SessionViewModel => App.Current.Services.GetService<SessionViewModel>() ?? throw new NullReferenceException();
}
