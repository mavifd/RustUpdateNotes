using Discord;
using Newtonsoft.Json;
using RustUpdateNotes.GlobalClass;
using RustUpdateNotes.LoggerClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace RustUpdateNotes.CommitClass
{
    public static class Commit
    {
        private static readonly string commitApiUrl = "https://commits.facepunch.com/r/rust_reboot/?format=json";

        private static HashSet<string> storedCommits = new HashSet<string>();

        private static HttpClient httpClient = new HttpClient();

        public static async Task Commit_Runner()
        {
            while (true)
            {
                var maintask = CheckForNewCommits();
                var controltask = Task.Delay(TimeSpan.FromMinutes(5));
                var completedTask = await Task.WhenAny(maintask, controltask);
                if (completedTask == maintask)
                {
                    Global.CommitRunner_Succes++;
                }
                else
                {
                    Global.CommitRunner_Fail++;
                    Logger.LogMessage($"CommitRunner Timeout (5 minute)");
                    await Logger.DiscordMessage($"CommitRunner Timeout (5 minute)", true);
                }
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        public static async Task CheckForNewCommits()
        {
            try
            {
                var response = await httpClient.GetStringAsync(commitApiUrl);
                if (response == null) { Logger.LogMessage("[CommitTracker] response null."); await Logger.DiscordMessage($"[CommitTracker] response null.", true); return; }

                var commitData = JsonConvert.DeserializeObject<TheCommit>(response);
                if (commitData?.Results == null || !commitData.Results.Any()) { Logger.LogMessage("[CommitTracker] commitData null."); await Logger.DiscordMessage($"Commit data null.", true); return; }

                var newCommits = commitData.Results.Select(commit => commit.Message).ToList();
                if (newCommits == null || !newCommits.Any()) { Logger.LogMessage("[CommitTracker] newCommits null."); await Logger.DiscordMessage($"New commits null.", true); return; }

                if (storedCommits.Count == 0) { storedCommits.UnionWith(newCommits); await Logger.DiscordMessage($"Commit first run", true); return; }

                var differences = commitData.Results.Where(commit => newCommits.Contains(commit.Message) && !storedCommits.Contains(commit.Message)).OrderBy(commit => commit.Created).ToList();
                if (!differences.Any()) { Logger.LogMessage("[CommitTracker] diff null."); return; }

                foreach (var commit in differences)
                {
                    Logger.LogMessage($"[CommitTracker] Yeni Commit: {commit.Id}");
                    var commitlink = "https://commits.facepunch.com/" + commit.Id;
                    EmbedBuilder newEmbedBuilder = new EmbedBuilder()
                    .WithAuthor(commit.User.Name, commit.User.Avatar)
                    .WithTitle(commit.Branch)
                    .WithDescription(commit.Message)
                    .WithUrl(commitlink)
                    .WithColor(Color.Blue)
                    .WithFooter($"Change: {commit.Changeset} ({commit.Id}) • {DateTime.Now:dd/MM HH:mm}");

                    var guildlist = Global.CommitFollowerChannels.Keys.ToList();
                    foreach (var guildId in guildlist)
                    {
                        var guild = Global.Client.GetGuild(guildId);
                        if (guild == null) continue;
                        var channelids = Global.CommitFollowerChannels[guildId].ToList();
                        foreach (var channelId in channelids)
                        {
                            var channel = guild.GetTextChannel(channelId);
                            if (channel == null) continue;
                            if (!await Logger.CheckBotPerms(guild) || !await Logger.CheckChannelPerms(channel)) { Logger.LogMessage($"Commit Yetki Yetersizliği | Guild: {guild.Name}"); continue; };
                            await channel.SendMessageAsync("", false, newEmbedBuilder.Build());
                        }
                    }
                }
                storedCommits.UnionWith(newCommits);
                Logger.LogMessage($"[CommitTracker] Depolanan: {storedCommits.Count}");
            }
            catch (Exception ex)
            {
                Logger.LogMessage($"Error - Commit: {ex}");
                await Logger.DiscordMessage($"Error - Commit: {ex}", true);
            }
        }

        public class TheCommit
        {
            public List<CommitData> Results { get; set; }
        }

        public class CommitData
        {
            public int Id { get; set; }
            public string Repo { get; set; }
            public string Branch { get; set; }
            public string Changeset { get; set; }
            public DateTime Created { get; set; }
            public string Message { get; set; }
            public User User { get; set; }
        }

        public class User
        {
            public string Name { get; set; }
            public string Avatar { get; set; }
        }
    }
}