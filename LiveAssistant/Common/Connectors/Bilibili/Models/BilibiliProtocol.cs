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
using System.Buffers;
// ReSharper disable MemberCanBePrivate.Global

namespace LiveAssistant.Common.Connectors.Bilibili.Models;

internal struct BilibiliProtocol
{
    /// <summary>
    /// Length of the message (protocol header + data length)
    /// </summary>
    public int PacketLength;

    /// <summary>
    /// Size of the message header (Fixed as 16 [sizeof(Protocol)])
    /// </summary>
    public short HeaderLength;

    public short Version;

    /// <summary>
    /// Message type
    /// </summary>
    public int Action;

    /// <summary>
    /// Fixed as 1
    /// </summary>
    public int Parameter;

    public static BilibiliProtocol? FromBuffer(ReadOnlySequence<byte> buffer)
    {
        if (buffer.Length < 16) { throw new ArgumentException(); }
        var reader = new SequenceReader<byte>(buffer);

        if (reader.TryReadBigEndian(out int packetLength) &&
            reader.TryReadBigEndian(out short headerLength) &&
            reader.TryReadBigEndian(out short version) &&
            reader.TryReadBigEndian(out int action) &&
            reader.TryReadBigEndian(out int parameter))
        {
            return new BilibiliProtocol
            {
                PacketLength = packetLength,
                HeaderLength = headerLength,
                Version = version,
                Action = action,
                Parameter = parameter
            };
        }

        return null;
    }
}
