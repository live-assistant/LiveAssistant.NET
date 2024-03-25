//    Copyright (C) 2024 Live Assistant Bilibili Danmaku Forward plugin Authors
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

using BilibiliDM_PluginFramework;

namespace LiveAssistant.ExternalPlugin.BilibiliDanmakuForward
{
    public class Main: DMPlugin
    {
        public Main()
        {
            PluginName = "Live Assistant Forward";
            PluginDesc = "This plugin forwards Bilibili danmaku to Live Assistant desktop app.";
            PluginAuth = "Live Assistant";
            PluginVer = "v0.0.1";
            PluginCont = "hello@live-assistant.app";

            ReceivedDanmaku += OnReceivedDanmaku;
            ReceivedRoomCount += OnReceivedRoomCount;
        }

        private void OnReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void OnReceivedRoomCount(object sender, ReceivedRoomCountArgs e)
        {
            throw new System.NotImplementedException();
        }

        public override void Start()
        {
            base.Start();
            Log("Live Assistant Forward started");
        }

        public override void Stop()
        {
            base.Stop();
            Log("Live Assistant Forward stopped");
        }
    }
}
