using Discord;
using Discord.WebSocket;
using RustUpdateNotes.CommitClass;
using RustUpdateNotes.GlobalClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RustUpdateNotes.LoggerClass
{
    public static class Logger
    {

        public static async Task AppLog_Runner()
        {
            int AppRunTime = 0;
            while (true)
            {
                try
                {
                    EmbedBuilder embedBuilder = new EmbedBuilder()
               .WithTitle(":shield:  **Rust Update Notes**  :shield:")
               .WithDescription($"{AppRunTime} saattir çalışıyor.\n")
               .WithThumbnailUrl("https://yt3.googleusercontent.com/HPu-kTkwgN4mPxO6_PJThrtbPQEL_esHXjbPVp7bR5SF3H0HX_p6ub960hiH-D5WiDtPTosOXw=s176-c-k-c0x00ffffff-no-rj")
               .WithFooter($"{DateTime.Now:dd/MM HH:mm}")
               .WithColor(Color.Blue);
                    await Global.DiscordLog.SendMessageAsync(text: "", embeds: new Embed[] { embedBuilder.Build() });
                    AppRunTime++;
                }
                catch (Exception ex)
                {
                    await DiscordMessage($"AppLog_Runner send message failed. {ex}");
                }

                await Task.Delay(TimeSpan.FromHours(1));
            }
        }

        public static Task Log(LogMessage arg)
        {
            string logEntry = $"{DateTime.Now:dd/MM HH:mm} | [DiscordConnection] {arg}";
            Console.WriteLine(logEntry);
            return Task.CompletedTask;
        }

        public static void LogMessage(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string callerFilePath = "")
        {
            string prefix = string.Empty;
            ConsoleColor prefixColor = ConsoleColor.Gray;

            int pr = GetPrefixFromClass(callerFilePath);
            switch (pr)
            {
                case 1:
                    prefix = "[Channel] ";
                    prefixColor = ConsoleColor.Cyan;
                    break;
                case 2:
                    prefix = "[Command] ";
                    prefixColor = ConsoleColor.Green;
                    break;
                case 3:
                    prefix = "[Commit] ";
                    prefixColor = ConsoleColor.Yellow;
                    break;
                case 4:
                    prefix = "[Responder] ";
                    prefixColor = ConsoleColor.Magenta;
                    break;
                case 5:
                    prefix = "[Skin] ";
                    prefixColor = ConsoleColor.Blue;
                    break;
                case 6:
                    prefix = "[Update] ";
                    prefixColor = ConsoleColor.White;
                    break;
            }

            string logEntry = $"{DateTime.Now:dd/MM HH:mm} | ";
            Console.Write(logEntry);
            Console.ForegroundColor = prefixColor;
            Console.Write(prefix);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);
        }

        public static async Task DiscordMessage(string message, bool ping = false)
        {
            string logEntry = $"{DateTime.Now:dd/MM HH:mm} | {message}";
            if (ping) logEntry = $"{DateTime.Now:dd/MM HH:mm} | <@{Global.Mavi}> | {message}";
            await Global.DiscordLog.SendMessageAsync(logEntry);
        }

        public static async Task<bool> CheckBotPerms(SocketGuild guild)
        {
            var requiredPermissions = new[] {
               GuildPermission.AttachFiles, GuildPermission.EmbedLinks, GuildPermission.ManageChannels,GuildPermission.ManageWebhooks,GuildPermission.MentionEveryone,
               GuildPermission.ReadMessageHistory, GuildPermission.SendMessages,GuildPermission.ViewChannel,GuildPermission.UseApplicationCommands};
            var botUser = guild.CurrentUser;
            return await Task.FromResult(requiredPermissions.All(permission => botUser.GuildPermissions.Has(permission)));
        }

        public static async Task<bool> CheckChannelPerms(SocketTextChannel channel)
        {
            var requiredPermissions = new[] {
                ChannelPermission.AttachFiles, ChannelPermission.EmbedLinks, ChannelPermission.ManageChannels,ChannelPermission.ManageWebhooks,ChannelPermission.MentionEveryone,
                 ChannelPermission.ReadMessageHistory, ChannelPermission.SendMessages,ChannelPermission.ViewChannel,ChannelPermission.UseApplicationCommands};
            var permissions = channel.Guild.CurrentUser.GetPermissions(channel);
            return await Task.FromResult(requiredPermissions.All(permission => permissions.Has(permission)));
        }

        private static int GetPrefixFromClass(string callerFilePath)
        {
            if (callerFilePath.Contains("Channel.cs"))
            {
                return 1;
            }
            else if (callerFilePath.Contains("Commands.cs"))
            {
                return 2;
            }
            else if (callerFilePath.Contains("Commit.cs"))
            {
                return 3;
            }
            else if (callerFilePath.Contains("Responder.cs"))
            {
                return 4;
            }
            else if (callerFilePath.Contains("Skin.cs"))
            {
                return 5;
            }
            else if (callerFilePath.Contains("Update.cs"))
            {
                return 6;
            }
            else
            {
                return 0;
            }
        }
    }
}