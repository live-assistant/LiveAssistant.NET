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
using CommunityToolkit.Mvvm.Messaging;
using LiveAssistant.Common;
using LiveAssistant.Database;
using LiveAssistant.Extensions;
using Realms;

namespace LiveAssistant.ViewModels;

internal class TutorialViewModel : ObservableObject
{
    public TutorialViewModel()
    {
        if (!Intro && !_hosts.Any()) Start(nameof(Intro));
    }

    private readonly IQueryable<Host> _hosts = Db.Default.Realm.All<Host>();

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

    public List<StepData>? StepList
    {
        get
        {
            if (ActiveTutorial is null) return null;
            AllSteps.TryGetValue(ActiveTutorial, out var list);
            return list;
        }
    }

    public int TotalStepCount => StepList?.Count ?? 0;

    private StepData? _step;
    public StepData? Step
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
            var index = StepList.IndexOf((StepData)Step);
            return index + 1;
        }
    }

    public bool IsLastStep => StepIndex == TotalStepCount;
    public string? ActionButtonText => IsLastStep ? null : StepIndex is 1 ? "ButtonStart".Localize() : Step?.IsNextEnabled ?? true ? "ButtonNext".Localize() : null;
    public string? CloseButtonText => StepIndex is 1 ? "ButtonSkip".Localize() : IsLastStep ? "ButtonFinish".Localize() : null;

    private void Start(string name)
    {
        ActiveTutorial = name;
        var firstStep = AllSteps[name].FirstOrDefault();
        firstStep.PreAction?.Invoke();
        Step = firstStep;
    }

    public RelayCommand<string> NextStepCommand => new(delegate
    {
        if (StepList is null || Step is null) return;

        Step?.PostAction?.Invoke();
        var index = StepIndex - 1;
        if (index is -1) return;
        if (index == StepList.Count - 1)
        {
            SkipCommand.Execute(null);
        }
        else
        {
            Step = StepList[StepIndex];
            Step?.PreAction?.Invoke();
        }
    });

    public RelayCommand SkipCommand => new(delegate
    {
        if (ActiveTutorial is null) return;

        _manager.SaveSetting(ActiveTutorial, true.ToString());
        ActiveTutorial = null;
        Step = null;
    });

    private Dictionary<string, List<StepData>> AllSteps => new()
    {
        {
            nameof(Intro),
            new List<StepData>
            {
                new()
                {
                    Id = "Start",
                },
                new()
                {
                    Id = "AddHost",
                    IsNextEnabled = false,
                    PreAction = delegate
                    {
                        _hosts.SubscribeForNotifications((hosts, changes, error) =>
                        {
                            if (changes?.InsertedIndices.Any() ?? false) NextStepCommand.Execute(null);
                        });
                    },
                },
                new()
                {
                    Id = "Record",
                    IsNextEnabled = false,
                    PreAction = delegate
                    {
                        WeakReferenceMessenger.Default.Register<SessionIsConnectedChangedMessage>(this, (_, message) =>
                        {
                            if (message.Value) NextStepCommand.Execute(null);
                        });
                    },
                },
                new()
                {
                    Id = "Panels",
                },
                new()
                {
                    Id = "Summary",
                },
                new()
                {
                    Id = "End",
                },
            }
        },
    };

    private bool Intro
    {
        get => Convert.ToBoolean(_manager.Settings[nameof(Intro)]);
        set => _manager.SaveSetting(nameof(Intro), value.ToString());
    }
}

public struct StepData
{
    public string Id;
    public bool? IsNextEnabled;
    public Action? PreAction;
    public Action? PostAction;
}
