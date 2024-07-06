using Discord;
using Discord.WebSocket;
using RustUpdateNotes.ChannelClass;
using RustUpdateNotes.CommitClass;
using RustUpdateNotes.GlobalClass;
using RustUpdateNotes.LoggerClass;
using RustUpdateNotes.ResponderClass;
using RustUpdateNotes.SkinClass;
using RustUpdateNotes.UpdateClass;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RustUpdateNotes
{
    internal class Program
    {
        private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            Console.Title = "RustUpdateNotes";

            Logger.LogMessage("Starting...");

            var config = new DiscordSocketConfig { GatewayIntents = GatewayIntents.All };
            Global.Client = new DiscordSocketClient(config);

            Global.Client.Log += Logger.Log;
            Global.Client.JoinedGuild += OnJoinedGuild;
            Global.Client.LeftGuild += OnLeaveGuild;
            Global.Client.MessageReceived += Responder.MessageReceived;
            Global.Client.Ready += BotReady;
            Global.Client.SlashCommandExecuted += SlashCommandHandler;

            await Global.Client.LoginAsync(TokenType.Bot, Global.Token);
            await Global.Client.StartAsync();

            while (!Global.BotReady) { await Task.Delay(1000); }

            await Global.Client.SetCustomStatusAsync($"🔔 Rust Güncelleme Notları");

            Logger.LogMessage("Starting tasks...");

            _ = Task.Run(Channel.Channel_Runner);
            _ = Task.Run(Commit.Commit_Runner);
            _ = Task.Run(Skin.Skin_Runner);
            _ = Task.Run(Update.Update_Runner);
            _ = Task.Run(Responder.Responder_Runner);
            _ = Task.Run(Logger.AppLog_Runner);

            Logger.LogMessage("All done!");

            await Task.Delay(Timeout.Infinite);
        }

        /////////////// HELPER FUNCTIONS

        private async Task OnJoinedGuild(SocketGuild guild)
        {
            Logger.LogMessage($"**New guild:** {guild}");
            await Logger.DiscordMessage($"**New guild:** {guild}");
            await Channel.CheckChannels();
        }

        private async Task OnLeaveGuild(SocketGuild guild)
        {
            Logger.LogMessage($"**Guild leaved:** {guild}");
            await Logger.DiscordMessage($"**Guild leaved:** {guild}");
        }

        private async Task BotReady()
        {
            var _helpcommand = new SlashCommandBuilder()
            .WithName("destek")
            .WithDescription("Yardım gerekiyorsa veya bir sorunuz/isteğiniz varsa bu komutu seçiniz.");

            var guildlist = Global.Client.Guilds.ToList();
            foreach (var guild in guildlist)
            {
                try
                {
                    if (!await Logger.CheckBotPerms(guild)) { Logger.LogMessage($"Genel Yetki Yetersizliği - Komut | Guild: {guild.Name}"); continue; }

                    var currentcommands = await guild.GetApplicationCommandsAsync();
                    if (!currentcommands.Any())
                    {
                        Logger.LogMessage($"Komut ekleniyor... {guild}");
                        await guild.CreateApplicationCommandAsync(_helpcommand.Build());
                    }
                    else
                    {
                        Logger.LogMessage($"Komut zaten var. {guild}");
                        foreach (var command in currentcommands) { Logger.LogMessage($"Komut: {command.Name}"); }
                    }
                }
                catch (Exception)
                {
                    Logger.LogMessage($"Komut ekleme başarısız. {guild}");
                }
            }
            Global.BotReady = true;
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            switch (command.Data.Name)
            {
                case "destek":
                    EmbedBuilder embedBuilder = new EmbedBuilder();
                    embedBuilder.WithTitle("Rust Güncelleme Takipçisi");
                    embedBuilder.WithDescription("https://discord.com/invite/uFedWRP5tE\n\nDiscord sunucumuza katılarak iletişime geçebilirsiniz.");
                    embedBuilder.WithColor(Color.Blue);
                    embedBuilder.WithThumbnailUrl("https://yt3.googleusercontent.com/HPu-kTkwgN4mPxO6_PJThrtbPQEL_esHXjbPVp7bR5SF3H0HX_p6ub960hiH-D5WiDtPTosOXw=s176-c-k-c0x00ffffff-no-rj");
                    embedBuilder.AddField("Ping:", Global.Client.Latency, true);
                    await command.RespondAsync(embed: embedBuilder.Build()); //ephemeral: true //sadece kişinin kendisi görür
                    break;
            }
        }
    }
}