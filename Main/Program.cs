using Discord;
using Discord.WebSocket;
using RustUpdateNotes.ChannelClass;
using RustUpdateNotes.CommandClass;
using RustUpdateNotes.CommitClass;
using RustUpdateNotes.GlobalClass;
using RustUpdateNotes.LoggerClass;
using RustUpdateNotes.ResponderClass;
using RustUpdateNotes.SkinClass;
using RustUpdateNotes.UpdateClass;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RustUpdateNotes
{
    internal class Program
    {
        private static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        private async Task MonitorTask(Func<Task> taskFunction)
        {
            while (true)
            {
                try
                {
                    await taskFunction();
                }
                catch (Exception ex)
                {
                    Logger.LogMessage($"Fonksiyon Hatası: {taskFunction.Method.Name}, yeniden başlatılıyor... Hata: {ex.Message}");
                    await Logger.DiscordMessage($"Fonksiyon Hatası: {taskFunction.Method.Name}, yeniden başlatılıyor... Hata: {ex.Message}");
                    await Task.Delay(5000);
                }
            }
        }

        public async Task MainAsync()
        {
            Console.Title = "Rust Update Notes";

            Console.OutputEncoding = Encoding.UTF8;

            Logger.LogMessage("Connecting to Discord...");

            Global.Client = new DiscordSocketClient(new DiscordSocketConfig { GatewayIntents = GatewayIntents.All });

            Global.Client.Log += Logger.Log;
            Global.Client.JoinedGuild += Channel.OnJoinedGuild;
            Global.Client.MessageReceived += Responder.MessageReceived;
            Global.Client.Ready += Command.BotReady;
            Global.Client.SlashCommandExecuted += Command.SlashCommandHandler;

            await Global.Client.LoginAsync(TokenType.Bot, Global.Token);
            await Global.Client.StartAsync();

            while (!Global.BotReady) { await Task.Delay(100); }

            await Global.Client.SetCustomStatusAsync($"🔔 Rust Güncelleme Notları");

            Logger.LogMessage("Starting Tasks...");

            _ = MonitorTask(() => Channel.Channel_Runner());
            _ = MonitorTask(() => Commit.Commit_Runner());
            _ = MonitorTask(() => Skin.Skin_Runner());
            _ = MonitorTask(() => Update.Update_Runner());
            _ = MonitorTask(() => Responder.Responder_Runner());
            _ = MonitorTask(() => Logger.AppLog_Runner());

            Logger.LogMessage("All done!");

            await Task.Delay(Timeout.Infinite);
        }
    }
}