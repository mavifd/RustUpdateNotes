using Discord.Webhook;
using Discord.WebSocket;
using System.Collections.Generic;

namespace RustUpdateNotes.GlobalClass
{
    public static class Global
    {
        public static readonly string Token = "-";

        public static DiscordSocketClient Client;

        public static DiscordWebhookClient DiscordLog = new DiscordWebhookClient("-");

        public static readonly ulong MainDiscordSohbet = 0;
        public static readonly ulong Mavi = 0;
        public static readonly ulong MainDiscordID = 0;

        public static Dictionary<ulong, List<ulong>> UpdateNoteChannels = new Dictionary<ulong, List<ulong>>();
        public static Dictionary<ulong, List<ulong>> UpdateDateChannels = new Dictionary<ulong, List<ulong>>();
        public static Dictionary<ulong, List<ulong>> UpdateTrackerChannels = new Dictionary<ulong, List<ulong>>();
        public static Dictionary<ulong, List<ulong>> StoreCheckerChannels = new Dictionary<ulong, List<ulong>>();
        public static Dictionary<ulong, List<ulong>> CommitFollowerChannels = new Dictionary<ulong, List<ulong>>();

        public static int DayTimeHour = 19; //18 YAZ, 19 KIŞ.

        public static bool BotReady = false;
    }
}
