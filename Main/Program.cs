using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RustUpdateNotes.ChannelClass;
using RustUpdateNotes.CommitClass;
using RustUpdateNotes.GlobalClass;
using RustUpdateNotes.LoggerClass;
using RustUpdateNotes.ResponderClass;
using RustUpdateNotes.SkinClass;
using RustUpdateNotes.UpdateClass;
using RustUpdateNotes.CommandClass;


namespace RustUpdateNotes
{
    internal class Program
    {
        private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            Console.Title = "Rust Update Notes";

            Logger.LogMessage("Connecting to Discord...");

            Global.Client = new DiscordSocketClient(new DiscordSocketConfig { GatewayIntents = GatewayIntents.All });

            Global.Client.Log += Logger.Log;
            Global.Client.JoinedGuild += OnJoinedGuild;
            Global.Client.MessageReceived += Responder.MessageReceived;
            Global.Client.Ready += Command.BotReady;
            Global.Client.SlashCommandExecuted += Command.SlashCommandHandler;

            await Global.Client.LoginAsync(TokenType.Bot, Global.Token);
            await Global.Client.StartAsync();

            while (!Global.BotReady) { await Task.Delay(100); }

            await Global.Client.SetCustomStatusAsync($"🔔 Rust Güncelleme Notları");

            Logger.LogMessage("Starting Tasks...");

            _ = Task.Run(Channel.Channel_Runner);
            _ = Task.Run(Commit.Commit_Runner);
            _ = Task.Run(Skin.Skin_Runner);
            _ = Task.Run(Update.Update_Runner);
            _ = Task.Run(Responder.Responder_Runner);
            _ = Task.Run(Logger.AppLog_Runner);

            Logger.LogMessage("All done!");

            await Task.Delay(Timeout.Infinite);
        }

        private async Task OnJoinedGuild(SocketGuild guild)
        {
            await Channel.CheckChannels();
        }
    }
}