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
