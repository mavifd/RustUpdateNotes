using Discord.Webhook;
using Discord.WebSocket;
using System.Collections.Generic;

namespace RustUpdateNotes.GlobalClass
{
    public static class Global
    {
        public static readonly string Token = "MTExMTAxNjg2MDc0NjUzMDgzNg.G5Tmp_.pk-mcC5NNneSCwWOZuvFwOzovem4rieLdLKT3k"; 

        public static DiscordSocketClient Client;

        public static DiscordWebhookClient DiscordLog = new DiscordWebhookClient("https://discord.com/api/webhooks/1243163905660817539/lBmr8EQHh1xvVu-32u1W_cjbdyneoB4Rd271orjMWrPiXY6BQTfpkSjCtfLpnJkYE_bE");

        public static readonly ulong MainDiscordSohbet = 1223037877911556110;
        public static readonly ulong Mavi = 170569747497222145;
        public static readonly ulong MainDiscordID = 1223037877911556107;

        public static Dictionary<ulong, List<ulong>> UpdateNoteChannels = new Dictionary<ulong, List<ulong>>();
        public static Dictionary<ulong, List<ulong>> UpdateDateChannels = new Dictionary<ulong, List<ulong>>();
        public static Dictionary<ulong, List<ulong>> UpdateTrackerChannels = new Dictionary<ulong, List<ulong>>();
        public static Dictionary<ulong, List<ulong>> StoreCheckerChannels = new Dictionary<ulong, List<ulong>>();
        public static Dictionary<ulong, List<ulong>> CommitFollowerChannels = new Dictionary<ulong, List<ulong>>();

        public static bool BotReady = false;
    }
}