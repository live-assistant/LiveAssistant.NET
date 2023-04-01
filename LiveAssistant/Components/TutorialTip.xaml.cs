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

using LiveAssistant.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;

namespace LiveAssistant.Components;

public sealed partial class TutorialTip : INotifyPropertyChanged
{
    public TutorialTip()
    {
        InitializeComponent();

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(ViewModel.ActiveTutorial) or nameof(ViewModel.Step))
            {
                OnPropertyChanged(nameof(IsOpen));
            }
        };
    }

    private TutorialViewModel ViewModel => App.Current.Services.GetService<TutorialViewModel>() ?? throw new NullReferenceException();

    public string Tutorial
    {
        get { return (string)GetValue(TutorialProperty); }
        set
        {
            SetValue(TutorialProperty, value);
            OnPropertyChanged(nameof(IsOpen));
        }
    }
    private static readonly DependencyProperty TutorialProperty =
        DependencyProperty.Register(nameof(Tutorial), typeof(string), typeof(TutorialTip), new PropertyMetadata(null));

    public string Step
    {
        get { return (string)GetValue(StepProperty); }
        set
        {
            SetValue(StepProperty, value);
            OnPropertyChanged(nameof(IsOpen));
        }
    }
    private static readonly DependencyProperty StepProperty =
        DependencyProperty.Register(nameof(Step), typeof(string), typeof(TutorialTip), new PropertyMetadata(null));

    public bool IsOpen => Tutorial == ViewModel.ActiveTutorial && Step == ViewModel.Step;

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    private static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(TutorialTip), new PropertyMetadata(null));

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }
    private static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(TutorialTip), new PropertyMetadata(null));

    public TeachingTipPlacementMode Placement
    {
        get => (TeachingTipPlacementMode)GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }
    private static readonly DependencyProperty PlacementProperty =
        DependencyProperty.Register(nameof(Placement), typeof(TeachingTipPlacementMode), typeof(TutorialTip), new PropertyMetadata(TeachingTipPlacementMode.Auto));

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
