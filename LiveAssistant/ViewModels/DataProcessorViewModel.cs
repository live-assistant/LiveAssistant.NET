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
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LiveAssistant.Common;
using LiveAssistant.Common.Messages;
using LiveAssistant.Common.Types;
using LiveAssistant.Database;
using Realms;

namespace LiveAssistant.ViewModels;

internal class DataProcessorViewModel : ObservableObject
{
    public DataProcessorViewModel()
    {
        _images.SubscribeForNotifications(OnImageContentNotification);
    }

    private void OnImageContentNotification(IRealmCollection<ImageContent> images, ChangeSet? changes, Exception error)
    {
        if (changes is null || !changes.InsertedIndices.Any()) return;

        foreach (var index in changes.InsertedIndices)
        {
            var image = images[index];
            if (image.Platform is (int)Platforms.Twitch || image.Bytes != null) return;
            var url = image.Url;

            Task.Run(async delegate
            {
                try
                {
                    var bytes = await _client.GetByteArrayAsync(url);
                    App.Current.MainQueue.TryEnqueue(delegate
                    {
                        Db.Default.Realm.Write(delegate
                        {
                            image.Bytes = bytes;
                        });
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    WeakReferenceMessenger.Default.Send(new ShowInfoBarMessage(Helpers.GetExceptionInfoBar(e)));
                }
            });
        }
    }

    private readonly HttpClient _client = new()
    {
        DefaultRequestHeaders =
        {
            { "Accept", "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8" },
            { "Accept-Encoding", "gzip, deflate, br" },
            { "Accept-Language", "en-US,en;q=0.9" },
            { "Cache-Control", "no-cache" },
            { "Connection", "keep-alive" },
            { "DNT", "1" },
            { "Pragma", "no-cache" },
            { "Sec-Fetch-Dest", "image" },
            { "Sec-Fetch-Mode", "no-cors" },
            { "Sec-Fetch-Site", "cross-site" },
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36" },
        },
    };
    private readonly IQueryable<ImageContent> _images = Db.Default.Realm.All<ImageContent>();
}
