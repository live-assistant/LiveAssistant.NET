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

using CommunityToolkit.HighPerformance;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Compression;
using System.IO.Pipelines;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LiveAssistant.Common.Connectors.Bilibili.Models;
using Timer = System.Timers.Timer;

namespace LiveAssistant.Common.Connectors.Bilibili;

internal class BilibiliTcpConnection : IDisposable
{
    public BilibiliTcpConnection(
        string server,
        int port,
        int roomId,
        string token)
    {
        _server = server;
        _port = port;
        _roomId = roomId;
        _token = token;

        _heartbeatTimer.Elapsed += OnHeartbeat;
    }

    private readonly string _server;
    private readonly int _port;
    private readonly int _roomId;
    private readonly string _token;

    private readonly Pipe _pipe = new();
    private Task? _readLoop;

    private readonly TcpClient _tcpClient = new();
    private readonly Timer _heartbeatTimer = new()
    {
        Interval = 30000,
        AutoReset = true,
    };

    public event EventHandler<Exception>? OnError;
    public event EventHandler<BilibiliDataBlock>? OnDataBlock;

    public async Task ConnectAsync()
    {
        try
        {
            await _tcpClient.ConnectAsync(_server, _port);
            if (await SendJoinChannel(_roomId, _token))
            {
                _heartbeatTimer.Start();
                _readLoop = ExecuteLoop();
                await _readLoop;
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            OnError?.Invoke(this, e);
        }
    }

    private async Task ExecuteLoop()
    {
        try
        {
            Task writing = FillPipeAsync(_tcpClient.Client, _pipe.Writer);
            Task reading = ReadPipeAsync(_pipe.Reader);

            await Task.WhenAll(reading, writing);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            OnError?.Invoke(this, e);
        }
    }

    private async Task FillPipeAsync(
        Socket socket,
        PipeWriter writer)
    {
        const int minimumBufferSize = 512;

        while (true)
        {
            try
            {
                var memory = writer.GetMemory(minimumBufferSize);
                var bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None);
                if (bytesRead == 0) break;

                writer.Advance(bytesRead);
            }
            catch (Exception e)
            {
                OnError?.Invoke(this, e);
                break;
            }

            FlushResult result = await writer.FlushAsync();
            if (result.IsCompleted)
            {
                break;
            }
        }

        await writer.CompleteAsync();
    }

    private async Task ReadPipeAsync(
        PipeReader reader)
    {
        while (true)
        {
            try
            {
                var result = await reader.ReadAsync();
                var buffer = result.Buffer;
                if (buffer.IsEmpty) continue;

                var header = BilibiliProtocol.FromBuffer(buffer.Slice(0, 16));
                if (header == null) continue;
                if (buffer.Length < header.Value.PacketLength) continue;

                var protocol = header.Value;

                var version = protocol.Version;
                switch (version)
                {
                    // Deflate
                    case 2 when protocol.Action == 5:
                    // Brotli
                    case 3 when protocol.Action == 5:
                    {
                        var move = version == 2 ? 2 : 0;
                        var data = buffer.Slice(16 + move, header.Value.PacketLength - 16 - move).ToArray();
                        var memory = new ReadOnlyMemory<byte>(data); // Update after .NET 7: https://github.com/dotnet/runtime/issues/58216

                        await using var deflate = new BrotliStream(memory.AsStream(), CompressionMode.Decompress);
                        var headerBuffer = new byte[16];

                        while (true)
                        {
                            if (await deflate.ReadAsync(headerBuffer) != 16) break;

                            var protocolIn = BilibiliProtocol.FromBuffer(new ReadOnlySequence<byte>(headerBuffer));
                            if (protocolIn == null) break;

                            var payloadLength = protocolIn.Value.PacketLength - 16;
                            var payloadBuffer = new byte[payloadLength];

                            if (await deflate.ReadAsync(payloadBuffer) != payloadLength) break;
                            OnDataBlock?.Invoke(this, new BilibiliDataBlock
                            {
                                Version = protocolIn.Value.Version,
                                Sequence = new ReadOnlySequence<byte>(payloadBuffer),
                            });
                        }
                        break;
                    }
                    default:
                    {
                        OnDataBlock?.Invoke(this, new BilibiliDataBlock
                        {
                            Version = protocol.Action,
                            Sequence = buffer.Slice(16),
                        });
                        break;
                    }
                }

                reader.AdvanceTo(buffer.Slice(protocol.PacketLength).Start);
            }
            catch (Exception e)
            {
                OnError?.Invoke(this, e);
                break;
            }
        }

        await reader.CompleteAsync();
        _tcpClient.Close();
    }

    private async void OnHeartbeat(object? sender, System.Timers.ElapsedEventArgs e)
    {
        await SendSocketDataAsync(2, "[object Object]");
    }

    Task SendSocketDataAsync(int action, string body)
    {
        return SendSocketDataAsync(0, 16, 1, action, 1, body);
    }

    async Task SendSocketDataAsync(int packetLength, short magic, short ver, int action, int param, string body)
    {
        var payload = Encoding.UTF8.GetBytes(body);
        if (packetLength == 0)
        {
            packetLength = payload.Length + 16;
        }

        var buffer = new byte[packetLength];
        using var ms = new MemoryStream(buffer);
        var b = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(buffer.Length));

        try
        {
            await ms.WriteAsync(b);
            b = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(magic));
            await ms.WriteAsync(b);
            b = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(ver));
            await ms.WriteAsync(b);
            b = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(action));
            await ms.WriteAsync(b);
            b = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(param));
            await ms.WriteAsync(b);
            if (payload.Length > 0)
            {
                await ms.WriteAsync(payload);
            }

            await _tcpClient.Client.SendAsync(buffer, SocketFlags.None);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            OnError?.Invoke(this, e);
        }
    }

    private async Task<bool> SendJoinChannel(int channelId, string token)
    {
        var packetModel = new
        {
            roomid = channelId,
            uid = 0,
            protover = 3,
            key = token,
            platform = "pc",
            type = 2
        };

        var payload = JsonSerializer.Serialize(packetModel, new JsonSerializerOptions
        {
            IncludeFields = true,
        });
        await SendSocketDataAsync(7, payload);
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _heartbeatTimer.Dispose();
        _readLoop?.Dispose();
        _tcpClient.Dispose();
    }
}
