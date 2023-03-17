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

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using TwitchLib.PubSub.Enums;

namespace LiveAssistant.Common;

internal static class Constants
{
    public const string VaultRealmName = "LiveAssistantData";
    public const string VaultUserNameRealmKey = "LiveAssistantKey";
    public const string VaultResourceName = "app.live-assitant.credentials";

    public const int CurrencyCodeCny = 156;
    public const int CurrencyCodeUsd = 840;

    public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        IncludeFields = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
        },
    };

    public const string ExtensionIdPrefix = "app.live-assistant.extensions";
    public const string ExtensionIdSocketServer = "socket-server";
    public const string ExtensionSettingKeySocketServerPort = "Port";
    public const string ExtensionSettingKeySocketServerPassword = "Password";

    public const string SecretNameIdToken = "IdToken";
    public const string SecretNameAccessToken = "AccessToken";
    public const string SecretNameTokenType = "TokenType";
    public const string SecretNameExpiresOn = "ExpiresOn";

    public static readonly Dictionary<int, float> BilibiliMembershipLevelToAmountMap = new()
    {
        { 1, 19998 },
        { 2, 1998 },
        { 3, 198 },
    };

    public static readonly Dictionary<int, string> BilibiliMembershipLevelToNameMap = new()
    {
        { 1, "总督" },
        { 2, "提督" },
        { 3, "舰长" },
    };

    public static readonly Dictionary<int, string> BilibiliMembershipLevelToImageMap = new()
    {
        { 1, "https://s1.hdslb.com/bfs/static/blive/blfe-live-room/static/img/icon-l-1.fde1190..png" },
        { 2, "https://s1.hdslb.com/bfs/static/blive/blfe-live-room/static/img/icon-l-2.6f68d77..png" },
        { 3, "https://s1.hdslb.com/bfs/static/blive/blfe-live-room/static/img/icon-l-3.402ac8f..png" },
    };

    public const string TwitchBitsSkuId = "bits";
    public const string TwitchBitsSkuDisplayName = "Bits";
    public const float TwitchBitUnitAmount = (float)0.01;

    public static readonly Dictionary<SubscriptionPlan, int> TwitchSubscriptionPlanToLevelMap = new()
    {
        { SubscriptionPlan.NotSet, 0 },
        { SubscriptionPlan.Prime, 1 },
        { SubscriptionPlan.Tier1, 1 },
        { SubscriptionPlan.Tier2, 2 },
        { SubscriptionPlan.Tier3, 3 },
    };

    public static readonly Dictionary<SubscriptionPlan, float> TwitchSubscriptionPlanToAmountMap = new()
    {
        { SubscriptionPlan.NotSet, 0 },
        { SubscriptionPlan.Prime, 1 },
        { SubscriptionPlan.Tier1, (float)4.9 },
        { SubscriptionPlan.Tier2, (float)9.99 },
        { SubscriptionPlan.Tier3, (float)24.99 },
    };
}
