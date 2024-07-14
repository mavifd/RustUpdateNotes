using Discord.WebSocket;
using Discord;
using RustUpdateNotes.GlobalClass;
using RustUpdateNotes.LoggerClass;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RustUpdateNotes.CommandClass
{
    public static class Command
    {

        public static async Task BotReady()
        {
            Global.BotReady = true;
            await InitCommands();
        }

        public static async Task InitCommands()
        {
            var _helpcommand = new SlashCommandBuilder()
           .WithName("destek")
           .WithDescription("Yardım gerekiyorsa veya bir sorunuz/isteğiniz varsa bu komutu seçiniz.");

            var guildlist = Global.Client.Guilds.ToList();
            foreach (var guild in guildlist)
            {
                try
                {
                    if (!await Logger.CheckBotPerms(guild))
                    {
                        Logger.LogMessage($"Genel Yetki Yetersizliği - Komut | Guild: {guild.Name}");
                        continue;
                    }

                    var currentcommands = await guild.GetApplicationCommandsAsync();
                    if (!currentcommands.Any())
                    {
                        Logger.LogMessage($"Komut ekleniyor... {guild}");
                        await guild.CreateApplicationCommandAsync(_helpcommand.Build());
                    }
                    else
                    {
                        Logger.LogMessage($"Komut zaten var. {guild}");
                        foreach (var command in currentcommands)
                        {
                            Logger.LogMessage($"Komut: {command.Name}");
                        }
                    }
                }
                catch (Exception)
                {
                    Logger.LogMessage($"Komut ekleme başarısız. {guild}");
                    await Logger.DiscordMessage($"Komut ekleme başarısız. {guild}", true);
                }
            }
        }

        public static async Task SlashCommandHandler(SocketSlashCommand command)
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
