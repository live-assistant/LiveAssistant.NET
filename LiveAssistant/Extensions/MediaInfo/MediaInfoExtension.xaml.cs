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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Media;
using Windows.Media.Control;
using Windows.Storage.Streams;
using CommunityToolkit.Mvvm.Messaging;
using LiveAssistant.Common;
using LiveAssistant.Common.Messages;
using LiveAssistant.Protocols.Data.Models;

namespace LiveAssistant.Extensions.MediaInfo;

public sealed partial class MediaInfoExtension : INotifyPropertyChanged
{
    public MediaInfoExtension()
    {
        InitializeComponent();

        // Init manager
        SetupManager();

        WeakReferenceMessenger.Default.Register<RequireMediaInfoMessage>(this, delegate
        {
            SendPayload();
        });
    }

    private readonly ExtensionSettingsManager _manager = new("media-info");

    // Session manager
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private readonly ObservableCollection<GlobalSystemMediaTransportControlsSession> _sessions = new();
    private bool IsSessionsEmpty => !_sessions.Any();

    private GlobalSystemMediaTransportControlsSession? _session;
    public GlobalSystemMediaTransportControlsSession? Session
    {
        get => _session;
        set
        {
            SetField(ref _session, value);
            SetupSession();
        }
    }

    private GlobalSystemMediaTransportControlsSessionMediaProperties? _media;
    private GlobalSystemMediaTransportControlsSessionMediaProperties? Media
    {
        get => _media;
        set => SetField(ref _media, value);
    }
    private string _thumbnailBase64 = "";
    private string ThumbnailBase64
    {
        get => _thumbnailBase64;
        set => SetField(ref _thumbnailBase64, value);
    }

    private GlobalSystemMediaTransportControlsSessionPlaybackInfo? _playback;
    private GlobalSystemMediaTransportControlsSessionPlaybackInfo? Playback
    {
        get => _playback;
        set => SetField(ref _playback, value);
    }

    private GlobalSystemMediaTransportControlsSessionTimelineProperties? _timeline;
    private GlobalSystemMediaTransportControlsSessionTimelineProperties? Timeline
    {
        get => _timeline;
        set
        {
            SetField(ref _timeline, value);
            OnPropertyChanged(nameof(TimelineProgress));
        }
    }
    private double TimelineProgress
    {
        get
        {
            if (Timeline == null) return 0;
            var duration = Timeline.EndTime - Timeline.StartTime;
            if (duration == TimeSpan.Zero) return 0;
            return Timeline.Position * 100 / duration;
        }
    }

    private async void SetupManager()
    {
        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _sessionManager.SessionsChanged += delegate
            {
                SetupSessions();
            };
            SetupSessions();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            WeakReferenceMessenger.Default.Send(new ShowInfoBarMessage(Helpers.GetExceptionInfoBar(e)));
        }
    }

    private void SetupSessions()
    {
        if (_sessionManager is null) return;

        // Setup new
        App.Current.MainQueue.TryEnqueue(delegate
        {
            _sessions.Clear();
            foreach (var session in _sessionManager.GetSessions())
            {
                _sessions.Add(session);
            }
            OnPropertyChanged(nameof(_sessions));
            OnPropertyChanged(nameof(IsSessionsEmpty));

            if (Session is not null && !_sessions.Contains(Session)) Session = null;
            Session ??= _sessions.FirstOrDefault();
        });
    }

    private void SetupSession()
    {
        if (Session is null) return;

        Session.MediaPropertiesChanged += delegate
        {
            UpdateMedia();
        };
        Session.PlaybackInfoChanged += delegate
        {
            UpdatePlayback();
        };
        Session.TimelinePropertiesChanged += delegate
        {
            UpdateTimeline();
        };

        UpdateMedia();
        UpdatePlayback();
        UpdateTimeline();
    }

    private void SendPayload()
    {
        if (!_manager.IsRunning) return;

        WeakReferenceMessenger.Default.Send(new MediaInfoPayloadMessage(new MediaInfoPayload
        {
            Type = (Media?.PlaybackType ?? MediaPlaybackType.Unknown).ToString().ToCamelCase(),
            Title = Media?.Title ?? "",
            Album = Media?.AlbumTitle ?? "",
            Cover = new ImageContentPayload
            {
                DataUrl = ThumbnailBase64,
            },
            TrackCount = Media?.AlbumTrackCount ?? 0,
            TrackNumber = Media?.TrackNumber ?? 1,
            Artist = Media?.Artist ?? "",
            Genres = Media?.Genres.ToArray() ?? Array.Empty<string>(),
            Status = (Playback?.PlaybackStatus ?? GlobalSystemMediaTransportControlsSessionPlaybackStatus.Stopped).ToString().ToCamelCase(),
            RepeatMode = (Playback?.AutoRepeatMode ?? MediaPlaybackAutoRepeatMode.None).ToString().ToCamelCase(),
            Shuffle = Playback?.IsShuffleActive ?? false,
            Rate = Playback?.PlaybackRate ?? 1,
            Duration = (int)(Timeline?.EndTime ?? TimeSpan.Zero - Timeline?.StartTime ?? TimeSpan.Zero).TotalMilliseconds,
            Position = (int)(Timeline?.Position ?? TimeSpan.Zero - Timeline?.StartTime ?? TimeSpan.Zero).TotalMilliseconds,
        }));
    }

    private void UpdateMedia()
    {
        App.Current.MainQueue.TryEnqueue(async delegate
        {
            try
            {
                var media = await _session?.TryGetMediaPropertiesAsync();
                if (media != Media)
                {
                    Media = media;

                    var thumbnail = Media.Thumbnail;
                    if (thumbnail != null)
                    {
                        // Set base64
                        var stream = await thumbnail.OpenReadAsync();
                        var buffer = new byte[stream.Size];
                        await stream.ReadAsync(buffer.AsBuffer(), (uint)stream.Size, InputStreamOptions.None);
                        ThumbnailBase64 = Helpers.GetDataUrl("image/jpeg", Convert.ToBase64String(buffer));
                        stream.Dispose();

                        // Set image source
                        var imageStream = await thumbnail.OpenReadAsync();
                        _ = ThumbnailBitmapImage.SetSourceAsync(imageStream);
                    }
                    else
                    {
                        ThumbnailBase64 = "";
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                WeakReferenceMessenger.Default.Send(new ShowInfoBarMessage(Helpers.GetExceptionInfoBar(e)));
            }

            SendPayload();
        });
    }

    private void UpdatePlayback()
    {
        App.Current.MainQueue.TryEnqueue(delegate
        {
            try
            {
                Playback = _session?.GetPlaybackInfo();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                WeakReferenceMessenger.Default.Send(new ShowInfoBarMessage(Helpers.GetExceptionInfoBar(e)));
            }

            SendPayload();
        });
    }

    private void UpdateTimeline()
    {
        App.Current.MainQueue.TryEnqueue(delegate
        {
            try
            {
                Timeline = _session?.GetTimelineProperties();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                WeakReferenceMessenger.Default.Send(new ShowInfoBarMessage(Helpers.GetExceptionInfoBar(e)));
            }

            SendPayload();
        });
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
}
