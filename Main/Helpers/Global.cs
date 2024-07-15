using Discord.Webhook;
using Discord.WebSocket;
using System.Collections.Generic;

namespace RustUpdateNotes.GlobalClass
{
    public static class Global
    {
        //MTExMTAxNjg2MDc0NjUzMDgzNg.G5Tmp_.pk-mcC5NNneSCwWOZuvFwOzovem4rieLdLKT3k     ##  MAIN BOT TOKEN
        //MTI1Mjk3NTk2ODczODYxMTIyMQ.GPpLCI.NCh6GXsJ2096u8M9kbvAyQY-lEimBPADuQNWb8     ##  TEST BOT TOKEN

        public static readonly string Token = "MTExMTAxNjg2MDc0NjUzMDgzNg.G5Tmp_.pk-mcC5NNneSCwWOZuvFwOzovem4rieLdLKT3k"; // MAIN BOT TOKEN

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

        public static int Channel_Succes = 0;
        public static int Commit_Succes = 0;
        public static int Skin_Succes = 0;
        public static int Update_Succes = 0;
        public static int Responder_Succes = 0;

        public static int Channel_Fail = 0;
        public static int Commit_Fail = 0;
        public static int Skin_Fail = 0;
        public static int Update_Fail = 0;
        public static int Responder_Fail = 0;

        public static int AppRunTime = 0;

        public static bool BotReady = false;
    }
}