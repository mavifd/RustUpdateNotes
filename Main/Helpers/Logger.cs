using Discord;
using Discord.WebSocket;
using RustUpdateNotes.GlobalClass;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RustUpdateNotes.LoggerClass
{
    public static class Logger
    {

        public static async Task AppLog_Runner()
        {
            while (true)
            {
                try
                {
                    await DiscordMessage($"**Runtime:** {(Global.AppRunTime * 30) / 60}" +
                   $"\n **Channel_Runner:** {Global.ChannelRunner_Succes} - {Global.ChannelRunner_Fail}" +
                   $"\n **Commit_Runner:** {Global.CommitRunner_Succes} - {Global.CommitRunner_Fail}" +
                   $"\n **Skin_Runner:** {Global.SkinRunner_Succes} - {Global.SkinRunner_Fail}" +
                   $"\n **Update_Runner:** {Global.UpdateRunner_Succes} - {Global.UpdateRunner_Fail}" +
                   $"\n **UpdateMessage_Runner:** {Global.UpdateMessageRunner_Succes} - {Global.UpdateMessageRunner_Fail}");
                    Global.AppRunTime++;
                }
                catch (Exception ex)
                {
                    await DiscordMessage($"{ex}");
                }

                await Task.Delay(TimeSpan.FromMinutes(30));
            }
        }

        public static Task Log(LogMessage arg)
        {
            string logEntry = $"{DateTime.Now} | [DiscordConnection] {arg}";
            Console.WriteLine(logEntry);
            return Task.CompletedTask;
        }

        public static void LogMessage(string message)
        {
            string logEntry = $"{DateTime.Now} | {message}";
            Console.WriteLine(logEntry);
        }

        public static async Task DiscordMessage(string message, bool ping = false)
        {
            string logEntry = "";
            if (ping) logEntry = $"{DateTime.Now} | <@{Global.Mavi}> | {message}";
            else logEntry = $"{DateTime.Now} | {message}";
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
    }
}