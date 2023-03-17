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

using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using EmbedIO.WebSockets;
using System.Threading.Tasks;
using System.Web;
using LiveAssistant.Common;

namespace LiveAssistant.SocketServer;

internal class SocketServerModule : WebSocketModule
{
    public SocketServerModule(string urlPath, bool enableConnectionWatchdog) : base(urlPath, enableConnectionWatchdog) { }

    public string? Password = null;

    protected override Task OnClientConnectedAsync(IWebSocketContext context)
    {
        var args = HttpUtility.ParseQueryString(context.RequestUri.Query);
        var types = args[Protocols.Data.Constants.QueryNameDataType];
        if (args[Protocols.Data.Constants.QueryNameAuthorization] == Password
            && !string.IsNullOrEmpty(types))
        {
            App.Current.MainQueue.TryEnqueue(delegate
            {
                WeakReferenceMessenger.Default.Send(new NewSocketClientMessage(
                    new SocketClient(context, types.Split(",").Select(t => t.ToCamelCase()).ToList())));
            });
        }
        else
        {
            context.WebSocket.CloseAsync();
        }

        return Task.CompletedTask;
    }

    protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
    {
        App.Current.MainQueue.TryEnqueue(delegate
        {
            WeakReferenceMessenger.Default.Send(new RemoveSocketClientMessage(context));
        });

        return Task.CompletedTask;
    }

    protected override Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result)
    {
        return Task.CompletedTask;
    }
}
