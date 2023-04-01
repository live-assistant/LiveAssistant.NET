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
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveAssistant.Common;
using LiveAssistant.Extensions;

namespace LiveAssistant.ViewModels;

internal class TutorialViewModel : ObservableObject
{
    public TutorialViewModel()
    {
        if (!Intro)
        {
            StartTutorialCommand.Execute(nameof(Intro));
        }
    }

    private readonly ExtensionSettingsManager _manager = new("tutorial", new Dictionary<string, string>
    {
        { nameof(Intro), false.ToString() },
    });

    private string? _activeTutorial;
    public string? ActiveTutorial
    {
        get => _activeTutorial;
        set
        {
            SetProperty(ref _activeTutorial, value);
            OnPropertyChanged(nameof(StepList));
            OnPropertyChanged(nameof(TotalStepCount));
            OnPropertyChanged(nameof(StepIndex));
            OnPropertyChanged(nameof(IsLastStep));
            OnPropertyChanged(nameof(ActionButtonText));
            OnPropertyChanged(nameof(CloseButtonText));
        }
    }

    public List<string>? StepList
    {
        get
        {
            if (ActiveTutorial is null) return null;
            _steps.TryGetValue(ActiveTutorial, out var list);
            return list;
        }
    }

    public int TotalStepCount => StepList?.Count ?? 0;

    private string? _step;
    public string? Step
    {
        get => _step;
        set
        {
            SetProperty(ref _step, value);
            OnPropertyChanged(nameof(StepIndex));
            OnPropertyChanged(nameof(IsLastStep));
            OnPropertyChanged(nameof(ActionButtonText));
            OnPropertyChanged(nameof(CloseButtonText));
        }
    }

    public int StepIndex
    {
        get
        {
            if (StepList is null || Step is null) return 0;
            var index = StepList.IndexOf(Step);
            return index + 1;
        }
    }

    public bool IsLastStep => StepIndex == TotalStepCount;
    public string? ActionButtonText => IsLastStep ? null : (StepIndex is 0 ? "ButtonStart" : "ButtonNext").Localize();
    public string CloseButtonText => (IsLastStep ? "ButtonFinish" : "ButtonSkip").Localize();

    public RelayCommand<string> StartTutorialCommand => new(name =>
    {
        if (name is null) return;

        ActiveTutorial = name;
        Step = _steps[name].FirstOrDefault();
    });

    public RelayCommand<string> NextStepCommand => new(delegate
    {
        if (StepList is null || Step is null) return;

        var stepIndex = StepList.IndexOf(Step);
        if (stepIndex is -1) return;
        if (stepIndex == StepList.Count - 1)
        {
            ExitTutorialCommand.Execute(null);
        }
        else
        {
            Step = StepList[stepIndex + 1];
        }
    });

    public RelayCommand ExitTutorialCommand => new(delegate
    {
        if (ActiveTutorial is null) return;

        _manager.SaveSetting(ActiveTutorial, true.ToString());
        ActiveTutorial = null;
        Step = null;
    });

    private readonly Dictionary<string, List<string>> _steps = new()
    {
        {
            nameof(Intro),
            new List<string>
            {
                "Start",
            }
        },
    };

    private bool Intro
    {
        get => Convert.ToBoolean(_manager.Settings[nameof(Intro)]);
        set => _manager.SaveSetting(nameof(Intro), value.ToString());
    }
}
