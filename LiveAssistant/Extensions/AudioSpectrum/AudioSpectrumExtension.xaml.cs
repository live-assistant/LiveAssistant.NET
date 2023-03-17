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
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using CommunityToolkit.Mvvm.Messaging;
using FftSharp;
using FftSharp.Windows;
using LiveAssistant.Common;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Transform = FftSharp.Transform;
using LiveAssistant.Common.Messages;
using LiveAssistant.Pages;

namespace LiveAssistant.Extensions.AudioSpectrum;

public sealed partial class AudioSpectrumExtension : INotifyPropertyChanged, IDisposable
{
    public AudioSpectrumExtension()
    {
        InitializeComponent();

        _manager.IsRunningChanged += delegate
        {
            ApplyState();
        };

        _outputDevicesManager.DevicesChanged += delegate
        {
            OutputDevice ??= _outputDevicesManager.Devices.FirstOrDefault();
            OnPropertyChanged(nameof(OutputDevice));
        };

        _inputDevicesManager.DevicesChanged += delegate
        {
            InputDevice ??= _inputDevicesManager.Devices.FirstOrDefault();
            OnPropertyChanged(nameof(InputDevice));
        };

        WeakReferenceMessenger.Default.Register<MainWindowClosedMessage>(this, delegate
        {
            Dispose();
        });
    }

    private ExtensionSettingsManager _manager = new("audio-spectrum", new Dictionary<string, string>
    {
        { nameof(OutputDevice), "" },
        { nameof(InputDevice), "" },
    });

    private void ApplyState()
    {
        try
        {
            _outputMax.Clear();
            _outputMax.Add(MaxInitialValue);
            _inputMax.Clear();
            _inputMax.Add(MaxInitialValue);

            if (_manager.IsRunning)
            {
                if (_outputCapture is not null)
                {
                    _outputCapture.StopRecording();
                    _outputCapture.DataAvailable -= OnOutputAvailable;
                    _outputCapture.Dispose();
                }

                if (_inputCapture is not null)
                {
                    _inputCapture.StopRecording();
                    _inputCapture.DataAvailable -= OnInputAvailable;
                    _inputCapture.Dispose();
                }

                Array.Clear(_outputPoints);
                Array.Clear(_inputPoints);

                _outputCapture = new WasapiLoopbackCapture(OutputDevice);
                _inputCapture = new WasapiCapture(InputDevice);

                _outputCapture.DataAvailable += OnOutputAvailable;
                _outputCapture.StartRecording();

                _inputCapture.DataAvailable += OnInputAvailable;
                _inputCapture.StartRecording();
            }
            else
            {
                if (_outputCapture is not null)
                {
                    _outputCapture.StopRecording();
                    _outputCapture.DataAvailable -= OnOutputAvailable;
                    _outputCapture.Dispose();
                }

                if (_inputCapture is not null)
                {
                    _inputCapture.StopRecording();
                    _inputCapture.DataAvailable -= OnInputAvailable;
                    _inputCapture.Dispose();
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            WeakReferenceMessenger.Default.Send(new ShowInfoBarMessage(Helpers.GetExceptionInfoBar(e)));
        }
    }

    // Devices
    private readonly NAudioDevicesManager _outputDevicesManager = new(DataFlow.Render);
    public MMDevice? OutputDevice
    {
        get => _outputDevicesManager.Devices.FirstOrDefault(d => d.ID == _manager.Settings[nameof(OutputDevice)]);
        set
        {
            _manager.SaveSetting(nameof(OutputDevice), value?.ID ?? "");
            ApplyState();
        }
    }

    private readonly NAudioDevicesManager _inputDevicesManager = new(DataFlow.Capture);
    public MMDevice? InputDevice
    {
        get => _inputDevicesManager.Devices.FirstOrDefault(d => d.ID == _manager.Settings[nameof(InputDevice)]);
        set
        {
            _manager.SaveSetting(nameof(InputDevice), value?.ID ?? "");
            ApplyState();
        }
    }

    private const double MaxInitialValue = 0.005;
    private const int MaxLengthLimit = 32;

    // Capture output
    private WasapiLoopbackCapture? _outputCapture;
    private WaveFormat? OutputFormat => _outputCapture?.WaveFormat;
    private readonly List<double> _outputMax = new();

    private const int PointsCount = 192;
    private readonly double[] _outputPoints = new double[PointsCount];

    private void OnOutputAvailable(object? sender, WaveInEventArgs e)
    {
        OnData(e.Buffer, true);
    }

    // Capture input
    private WasapiCapture? _inputCapture;
    private WaveFormat? InputFormat => _inputCapture?.WaveFormat;
    private readonly List<double> _inputMax = new();

    private readonly double[] _inputPoints = new double[PointsCount];

    private void OnInputAvailable(object? sender, WaveInEventArgs e)
    {
        OnData(e.Buffer, false);
    }

    // Update data points
    private readonly Blackman _window = new();
    private void OnData(byte[] buffer, bool isOutput)
    {
        var format = isOutput ? OutputFormat : InputFormat;
        if (format is null) return;
        var target = isOutput ? _outputPoints : _inputPoints;

        var values = new double[format.SampleRate / 10];
        var bytesPerSamplePerChannel = format.BitsPerSample / 8;
        var bytesPerSample = bytesPerSamplePerChannel * format.Channels;
        var bufferSampleCount = buffer.Length / bytesPerSample;

        if (bufferSampleCount > values.Length)
        {
            bufferSampleCount = values.Length;
        }

        for (var i = 0; i < bufferSampleCount; i++)
        {
            values[i] = bytesPerSamplePerChannel switch
            {
                2 when format.Encoding is WaveFormatEncoding.Pcm => BitConverter.ToInt16(buffer, i * bytesPerSample),
                4 when format.Encoding is WaveFormatEncoding.Pcm => BitConverter.ToInt32(buffer, i * bytesPerSample),
                4 when format.Encoding is WaveFormatEncoding.IeeeFloat => BitConverter.ToSingle(buffer, i * bytesPerSample),
                _ => throw new NotSupportedException(),
            };
        }

        var paddedValues = Pad.ZeroPad(values);
        var magnitudes = Transform.FFTmagnitude(paddedValues);
        var windowed = _window.Apply(magnitudes);
        var rangedMagnitudes = magnitudes.Take(windowed.Length / 4).ToArray();
        var maxMagnitude = rangedMagnitudes.Max();

        var downSample = rangedMagnitudes.Length / PointsCount;
        if (maxMagnitude < 0.000001)
        {
            for (var i = 0; i < PointsCount; i++)
            {
                target[i] = 0;
            }
        }
        else
        {
            for (var i = 0; i < PointsCount; i++)
            {
                var average = rangedMagnitudes.ToList().GetRange(i * downSample, downSample).Average();
                var max = Math.Max(MaxInitialValue, isOutput ? _outputMax.ToArray().Max() : _inputMax.ToArray().Max());
                target[i] = Math.Clamp(average / max * 2, 0, 1);
            }
        }

        App.Current.MainQueue.TryEnqueue(delegate
        {
            if (isOutput)
            {
                _outputMax.Add(maxMagnitude);
                if (_outputMax.Count > MaxLengthLimit) _outputMax.RemoveAt(0);
                WeakReferenceMessenger.Default.Send(new OutputAudioSpectrumPayloadMessage(_outputPoints));
            }
            else
            {
                _inputMax.Add(maxMagnitude);
                if (_inputMax.Count > MaxLengthLimit) _inputMax.RemoveAt(0);
                WeakReferenceMessenger.Default.Send(new InputAudioSpectrumPayloadMessage(_inputPoints));
            }

            _canvas?.Invalidate();
        });
    }

    // Chart
    private CanvasControl? _canvas;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_canvas is not null) return;

        _canvas = new CanvasControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        _canvas.Draw += Draw;
        CanvasWrapper.Child = _canvas;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _canvas?.RemoveFromVisualTree();
        _canvas = null;
    }

    private void Draw(CanvasControl canvas, CanvasDrawEventArgs args)
    {
        var session = args.DrawingSession;
        var outputColor = ((SolidColorBrush)App.Current.Resources["AccentFillColorDefaultBrush"]).Color;
        var inputColor = ((SolidColorBrush)App.Current.Resources["TextFillColorSecondaryBrush"]).Color;

        var barWidth = canvas.ActualWidth / PointsCount;
        var chartWidth = canvas.ActualWidth;
        var chartHeight = canvas.ActualHeight;
        const int minHeight = 1;

        for (var i = 0; i < PointsCount; i++)
        {
            var outputHeight = Math.Max(minHeight, _outputPoints[i] * chartHeight - minHeight);
            session.FillRectangle(
                new Rect(
                    x: barWidth * i,
                    y: chartHeight - outputHeight,
                    width: barWidth,
                    height: outputHeight),
                color: outputColor);

            var inputHeight = Math.Max(minHeight, _inputPoints[i] * chartHeight - minHeight);
            session.FillRectangle(
                new Rect(
                    x: chartWidth - barWidth * i,
                    y: 0,
                    width: barWidth,
                    height: inputHeight),
                color: inputColor);
        }
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
        _outputDevicesManager.Dispose();
        _inputDevicesManager.Dispose();
        _outputCapture?.Dispose();
        _inputCapture?.Dispose();
    }
}
