// Copyright (c) 2023 LiveAssistant.Protocols.Data Authors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace LiveAssistant.Protocols.Data.Models;

public struct GamepadPayload
{
    public double LeftX;
    public double LeftY;
    public double RightX;
    public double RightY;
    public double LeftTrigger;
    public double RightTrigger;
    public bool Up;
    public bool Down;
    public bool Left;
    public bool Right;
    public bool LeftThumbstick;
    public bool RightThumbstick;
    public bool LeftShoulder;
    public bool RightShoulder;
    public bool North;
    public bool South;
    public bool East;
    public bool West;
    public bool Home;
    public bool UtilLeft;
    public bool UtilRight;
}
