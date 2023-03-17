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
using System.Runtime.CompilerServices;
using System.Speech.Recognition;
using System.Text;
using CommunityToolkit.Mvvm.Messaging;
using LiveAssistant.Common.Messages;
using LiveAssistant.Database;
using LiveAssistant.Pages;

namespace LiveAssistant.Extensions.LiveCaption;

public sealed partial class LiveCaptionExtension : INotifyPropertyChanged, IDisposable
{
    public LiveCaptionExtension()
    {
        InitializeComponent();

        _manager.IsRunningChanged += delegate
        {
            ApplyState();
        };

        ApplyState();

        // Setup system recognizer
        SetupSystemRecognizer();

        WeakReferenceMessenger.Default.Register<MainWindowClosedMessage>(this, delegate
        {
            Dispose();
        });
    }

    private readonly ExtensionSettingsManager _manager = new("live-caption");

    private void ApplyState()
    {
        if (_manager.IsRunning)
        {
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }
        else
        {
            _recognizer.RecognizeAsyncStop();
            CompleteSentence();
        }
    }

    // Volume
    private int _volume;
    public int Volume
    {
        get => _volume;
        private set => SetField(ref _volume, value);
    }

    // Result
    private readonly StringBuilder _builder = new();
    public string CurrentSentence => _builder.ToString();
    private void CompleteSentence()
    {
        if (_builder.Length == 0) return;
        App.Current.MainQueue.TryEnqueue(delegate
        {
            var caption = new Caption(
                StringContent.Create(_builder.ToString()),
                _time ?? DateTimeOffset.Now,
                DateTimeOffset.Now);
            WeakReferenceMessenger.Default.Send(new CaptionEventMessage(caption));
        });
        _builder.Clear();
        _time = null;
    }
    private DateTimeOffset? _time;

    // System
    private readonly SpeechRecognitionEngine _recognizer = new();
    private void SetupSystemRecognizer()
    {
        _recognizer.LoadGrammarAsync(new DictationGrammar());
        _recognizer.SetInputToDefaultAudioDevice();
        _recognizer.SpeechRecognized += OnSystemRecognizerRecognize;
        _recognizer.RecognizeCompleted += OnSystemRecognizerComplete;
        _recognizer.AudioStateChanged += OnSystemRecognizerAudioStateChange;
        _recognizer.AudioLevelUpdated += OnSystemRecognizerVolumeChange;
    }

    private void OnSystemRecognizerVolumeChange(object? sender, AudioLevelUpdatedEventArgs e)
    {
        Volume = e.AudioLevel;
    }

    private void OnSystemRecognizerAudioStateChange(object? sender, AudioStateChangedEventArgs e)
    {
        if (e.AudioState is not AudioState.Speech)
        {
            CompleteSentence();
        }
    }

    private void OnSystemRecognizerComplete(object? sender, RecognizeCompletedEventArgs e)
    {
        CompleteSentence();
        ApplyState();
    }

    private void OnSystemRecognizerRecognize(object? sender, SpeechRecognizedEventArgs e)
    {
        var result = e.Result;
        if (result.Confidence < 0.1) return;
        _time ??= DateTimeOffset.Now;
        _builder.Append($"{e.Result.Text} ");
        OnPropertyChanged(nameof(CurrentSentence));
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

    public void Dispose()
    {
        _recognizer.Dispose();
    }
}
