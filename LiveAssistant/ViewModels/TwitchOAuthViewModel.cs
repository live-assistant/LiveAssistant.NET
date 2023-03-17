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
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LiveAssistant.Common;
using LiveAssistant.Common.Types;
using LiveAssistant.Database;
using TwitchLib.Api;
using StringContent = LiveAssistant.Database.StringContent;

namespace LiveAssistant.ViewModels;

internal class TwitchOAuthViewModel : ObservableObject
{
    public TwitchOAuthViewModel()
    {
        WeakReferenceMessenger.Default.Register<RequestTwitchOAuthMessage>(this, delegate
        {
            PrepareLogin();
        });

        WeakReferenceMessenger.Default.Register<CompleteTwitchOAuthMessage>(this, (_, message) =>
        {
            CompleteLogin(message.Value);
        });
    }

    private string? _nonce;
    private string? _state;
    private void PrepareLogin()
    {
        _nonce = Helpers.GetUniqueKey(8);
        _state = Helpers.GetUniqueKey(8);
        var scopes = string.Join("+",
            "openid",
            "bits:read",
            "channel:read:charity",
            "channel:read:goals",
            "channel:read:hype_train",
            "channel:read:polls",
            "channel:read:predictions",
            "channel:read:redemptions",
            "channel:read:subscriptions",
            "channel:read:vips",
            "chat:read");

        var url = $"{Env.TwitchAuthority}/authorize?claims=exp+picture+preferred_username&client_id={Env.TwitchClientId}&nonce={_nonce}&state={_state}&redirect_uri={Env.TwitchRedirectUrl}&response_type=token+id_token&scope={scopes}";

        Helpers.OpenLinkInBrowser(url);
    }

    private async void CompleteLogin(string fragment)
    {
        var queries = HttpUtility.ParseQueryString(fragment.Replace("#", "?"));
        var error = queries.Get("error");
        if (error != null) return;

        var state = queries.Get("state");
        if (state != _state) return;

        var accessToken = queries.Get("access_token");
        var idToken = queries.Get("id_token");
        var tokenType = queries.Get("token_type");
        if (accessToken == null || idToken == null || tokenType == null) return;

        var api = new TwitchAPI
        {
            Settings =
            {
                ClientId = Env.TwitchClientId,
                AccessToken = accessToken,
            },
        };

        var validation = await api.Auth.ValidateAccessTokenAsync(accessToken);
        if (validation.ClientId != Env.TwitchClientId) return;

        var users = await api.Helix.Users.GetUsersAsync(null, null, accessToken);
        var user = users.Users.FirstOrDefault();
        if (user == null) return;

        var userId = user.Id;
        var channels = await api.Helix.Channels.GetChannelInformationAsync(userId, accessToken);
        var channelData = channels.Data.FirstOrDefault();
        if (channelData == null) return;

        App.Current.MainQueue.TryEnqueue(delegate
        {
            var host = new Host(
                Platforms.Twitch,
                user.Id)
            {
                Avatar = ImageContent.Create(
                    Platforms.Twitch,
                    user.ProfileImageUrl),
                AuthTime = DateTimeOffset.Now,
                Username = StringContent.Create(channelData.BroadcasterName),
                ChannelId = channelData.BroadcasterName,
                Secrets =
                {
                    { Constants.SecretNameIdToken, idToken },
                    { Constants.SecretNameAccessToken, accessToken },
                    { Constants.SecretNameTokenType, tokenType },
                    { Constants.SecretNameExpiresOn, (DateTimeOffset.Now.ToUnixTimeSeconds() + validation.ExpiresIn).ToString() },
                },
            };

            Db.Default.Realm.Write(delegate
            {
                Db.Default.Realm.Add(host, true);
            });
        });
    }
}

internal class RequestTwitchOAuthMessage : ValueChangedMessage<bool>
{
    public RequestTwitchOAuthMessage() : base(true) { }
}

internal class CompleteTwitchOAuthMessage : ValueChangedMessage<string>
{
    public CompleteTwitchOAuthMessage(string value) : base(value) { }
}
