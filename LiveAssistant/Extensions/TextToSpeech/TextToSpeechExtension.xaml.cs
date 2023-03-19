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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Media.SpeechSynthesis;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.Runtime;
using CommunityToolkit.Mvvm.Messaging;
using LiveAssistant.Common;
using LiveAssistant.Common.Messages;
using NAudio.Wave;
using WinRT;
using NAudio.CoreAudioApi;
using LiveAssistant.Pages;

namespace LiveAssistant.Extensions.TextToSpeech;

public sealed partial class TextToSpeechExtension : INotifyPropertyChanged, IDisposable
{
    public TextToSpeechExtension()
    {
        InitializeComponent();

        _manager.IsRunningChanged += delegate
        {
            ApplyState();
        };
        ApplyState();

        Engine = _engineOptions.FirstOrDefault(o => (int)o.Engine == Convert.ToInt32(_manager.Settings[nameof(Engine)]));

        _devicesManager.DevicesChanged += delegate
        {
            AudioDevice ??= _devicesManager.Devices.FirstOrDefault();
            OnPropertyChanged(nameof(AudioDevice));
        };

        // Init synth voice picker
        if (SynthVoice == null)
        {
            SynthVoice = _synthVoices[0];
        }

        // Amazon Polly
        SetupAmazonPolly();

        WeakReferenceMessenger.Default.Register<MainWindowClosedMessage>(this, delegate
        {
            Dispose();
        });
    }

    private readonly ExtensionSettingsManager _manager = new("tts", new Dictionary<string, string>
    {
        { nameof(Engine), ((int)(AudioEngines.SystemSpeechSynthesis)).ToString() },
        { nameof(AudioDevice), "" },
        { nameof(SynthVoice), "" },
        { nameof(PollyKey), "" },
        { nameof(PollySecret), "" },
        { nameof(PollyVoice), "" },
    });

    private void ApplyState()
    {
        if (_manager.IsRunning)
        {
            if (WeakReferenceMessenger.Default.IsRegistered<MessageEventMessage>(this)) return;
            WeakReferenceMessenger.Default.Register<MessageEventMessage>(this, OnMessage);
        }
        else
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            _queue.Clear();
        }
    }

    private void OnMessage(object recipient, MessageEventMessage e)
    {
        App.Current.MainQueue.TryEnqueue(delegate
        {
            var message = e.Value;
            _queue.Add(new TextToSpeechItem
            {
                Content = message.Content.String,
                Time = message.Timestamp,
                SenderId = message.Sender?.Id,
                SenderName = message.Sender?.Username?.String,
            });
            Play();
        });
    }

    // Audio device
    private readonly NAudioDevicesManager _devicesManager = new(DataFlow.Render);
    private MMDevice? AudioDevice
    {
        get => _devicesManager.Devices.FirstOrDefault(d => d.ID == _manager.Settings[nameof(AudioDevice)]);
        set
        {
            _manager.SaveSetting(nameof(AudioDevice), value?.ID ?? "");
            ApplyState();
        }
    }

    // Engine
    private EngineOption _engine;
    private EngineOption Engine
    {
        get => _engine;
        set
        {
            SetField(ref _engine, value);
            _manager.SaveSetting(nameof(Engine), ((int)value.Engine).ToString());
        }
    }
    private readonly IEnumerable<EngineOption> _engineOptions = new[]
    {
        new EngineOption
        {
            Engine = AudioEngines.SystemSpeechSynthesis,
            DisplayName = "ExtensionTTSEngineNameSystem".Localize(),
        },
        new EngineOption
        {
            Engine = AudioEngines.AmazonPolly,
            DisplayName = "ExtensionTTSEngineNameAmazonPolly".Localize(),
        },
    };

    // Player
    private bool _isPlaying;
    private readonly IList<TextToSpeechItem> _queue = new List<TextToSpeechItem>();
    private TextToSpeechItem? _lastMessage;

    private void Play()
    {
        App.Current.MainQueue.TryEnqueue(async delegate
        {
            if (!_manager.IsRunning || !_queue.Any() || _isPlaying || AudioDevice is null) return;
            _isPlaying = true;

            var message = _queue.First();
            var shouldReadName = true;

            if (_lastMessage.HasValue)
            {
                var span = message.Time.Subtract(_lastMessage.Value.Time);
                shouldReadName = (span.Seconds > 20 || message.SenderId != _lastMessage.Value.SenderId) && message.SenderName is not null;
            }

            var text = shouldReadName ? string.Format(
                "ExtensionTTSTemplateMessage".Localize(),
                message.SenderName,
                message.Content) : message.Content;

            _lastMessage = message;
            if (_queue.Any()) _queue.RemoveAt(0);

            var engine = Engine.Engine;

            try
            {
                IWaveProvider provider = engine switch
                {
                    AudioEngines.SystemSpeechSynthesis => await GetSystemSynthStream(text),
                    AudioEngines.AmazonPolly => await GetAwsPollyStream(text),
                    _ => throw new ArgumentOutOfRangeException()
                };

                var audioOut = new WasapiOut(_devicesManager.GetDevice(AudioDevice.ID), AudioClientShareMode.Shared, true, 0);
                audioOut.Init(provider);
                audioOut.PlaybackStopped += (sender, _) =>
                {
                    _isPlaying = false;
                    Play();
                    sender.As<WasapiOut>().Dispose();
                };
                audioOut.Play();
            }
            catch (Exception e)
            {
                _isPlaying = false;
                Debug.WriteLine(e);
                WeakReferenceMessenger.Default.Send(new ShowInfoBarMessage(Helpers.GetExceptionInfoBar(e)));
            }
        });
    }

    // Synth
    private readonly SpeechSynthesizer _synth = new();
    private readonly ObservableCollection<VoiceInformation> _synthVoices = new(SpeechSynthesizer.AllVoices);
    private VoiceInformation? SynthVoice
    {
        get => _synthVoices.FirstOrDefault(v => v.Id == _manager.Settings[nameof(SynthVoice)]);
        set
        {
            _manager.SaveSetting(nameof(SynthVoice), value?.Id ?? "");
            _synth.Voice = value;
        }
    }

    private async Task<IWaveProvider> GetSystemSynthStream(string text)
    {
        var randomAccessStream = await _synth.SynthesizeTextToStreamAsync(text);
        return new WaveFileReader(randomAccessStream.AsStream());
    }

    // AWS Polly
    private AmazonPollyClient? _pollyClient;

    private readonly ObservableCollection<Voice> _awsVoices = new();
    private bool HasAwsVoices => _awsVoices.Any();

    private async void SetupAmazonPolly()
    {
        if (string.IsNullOrEmpty(PollyKey) || string.IsNullOrEmpty(PollySecret)) return;

        try
        {
            _pollyClient = new AmazonPollyClient(
                new BasicAWSCredentials(PollyKey, PollySecret),
                new AmazonPollyConfig
                {
                    RetryMode = RequestRetryMode.Adaptive,
                });

            var response = await _pollyClient.DescribeVoicesAsync(new DescribeVoicesRequest());
            _awsVoices.Clear();
            response.Voices.ForEach(v => _awsVoices.Add(v));
            PollyVoice ??= _awsVoices[0];
            OnPropertyChanged(nameof(PollyVoice));
            OnPropertyChanged(nameof(HasAwsVoices));
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            WeakReferenceMessenger.Default.Send(new ShowInfoBarMessage(Helpers.GetExceptionInfoBar(e)));
        }
    }

    private string PollyKey
    {
        get => _manager.Settings[nameof(PollyKey)];
        set
        {
            _manager.SaveSetting(nameof(PollyKey), value);
            SetupAmazonPolly();
        }
    }
    private string PollySecret
    {
        get => _manager.Settings[nameof(PollySecret)];
        set
        {
            _manager.SaveSetting(nameof(PollySecret), value);
            SetupAmazonPolly();
        }
    }

    private Voice? PollyVoice
    {
        get => _awsVoices.FirstOrDefault(v => v.Id == _manager.Settings[nameof(PollyVoice)]);
        set
        {
            _manager.SaveSetting(nameof(PollyVoice), value?.Id ?? "");
        }
    }

    private async Task<IWaveProvider> GetAwsPollyStream(string text)
    {
        var voice = PollyVoice;
        if (voice is null || _pollyClient is null) throw new NullReferenceException();

        var response = await _pollyClient.SynthesizeSpeechAsync(new SynthesizeSpeechRequest
        {
            VoiceId = voice.Id,
            Engine = voice.SupportedEngines.Contains("nueral") ? "neural" : "standard",
            Text = text,
            OutputFormat = OutputFormat.Mp3,
        });

        var audioStream = response.AudioStream;
        var stream = new MemoryStream();
        await audioStream.CopyToAsync(stream);
        stream.Seek(0, SeekOrigin.Begin);
        return new Mp3FileReader(stream);
    }

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
        _devicesManager.Dispose();
        _synth.Dispose();
        _pollyClient?.Dispose();
    }
}

internal struct TextToSpeechItem
{
    public string Content;
    public DateTimeOffset Time;
    public string? SenderId;
    public string? SenderName;
}
