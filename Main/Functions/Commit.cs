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

        private static List<int> storedCommits = new List<int>();
        private static List<int> sentCommits = new List<int>();

        private static HttpClient httpClient = new HttpClient();

        public static async Task Commit_Runner()
        {
            while (true)
            {
                var maintask = CheckForNewCommits();
                var controltask = Task.Delay(TimeSpan.FromMinutes(5));
                var completedTask = await Task.WhenAny(maintask, controltask);
                if (completedTask != maintask)
                {
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
                if (response == null)
                {
                    Logger.LogMessage($"response null.");
                    await Logger.DiscordMessage($"response null.", true);
                    return;
                }

                var commitData = JsonConvert.DeserializeObject<TheCommit>(response);
                if (commitData?.Results == null || !commitData.Results.Any())
                {
                    Logger.LogMessage($"commitData null.");
                    await Logger.DiscordMessage($"commitData null.", true);
                    return;
                }

                var newCommits = commitData.Results.Select(commit => commit.Id).ToList();
                if (newCommits == null || !newCommits.Any())
                {
                    Logger.LogMessage($"newCommits null.");
                    await Logger.DiscordMessage($"newCommits null.", true);
                    return;
                }

                if (storedCommits.Count == 0)
                {
                    storedCommits.AddRange(newCommits);
                    return;
                }

                Logger.LogMessage($"Latests: " +
                       $"{commitData.Results[0].Id}/{commitData.Results[0].Changeset} | " +
                       $"Stored: {storedCommits.Count} (N:{newCommits.Count}) - Sended: {sentCommits.Count}");

                var differences = commitData.Results
                .Where(commit => !storedCommits.Contains(commit.Id) && !sentCommits.Contains(commit.Id))
                .OrderBy(commit => commit.Created)
                .ToList();

                if (!differences.Any())
                {
                    return;
                }

                foreach (var commit in differences)
                {
                    Color commitcolor = Color.Blue; //merge into
                    if (commit.Message.IndexOf("merge", StringComparison.OrdinalIgnoreCase) >= 0 && commit.Message.IndexOf("into", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        commitcolor = Color.Green;
                    } //merge from
                    else if (commit.Message.IndexOf("merge", StringComparison.OrdinalIgnoreCase) >= 0 && commit.Message.IndexOf("from", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        commitcolor = Color.Green;
                    } //only merge
                    else if (commit.Message.IndexOf("merge", StringComparison.OrdinalIgnoreCase) >= 0 && (commit.Message.IndexOf("into", StringComparison.OrdinalIgnoreCase) == 0 || commit.Message.IndexOf("from", StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        commitcolor = Color.DarkPurple;
                    }

                    Logger.LogMessage($"New Commit: {commit.Changeset}");

                    var commitlink = "https://commits.facepunch.com/" + commit.Id;
                    EmbedBuilder newEmbedBuilder = new EmbedBuilder()
                    .WithTitle(commit.Branch)
                    .WithDescription(commit.Message)
                    .WithUrl(commitlink)
                    .WithColor(commitcolor)
                    .WithFooter($"Change: {commit.Changeset} ({commit.Id}) • {commit.Created.AddHours(3):dd/MM HH:mm}");

                    string authorName = commit.User.Name;
                    string authorAvatar = commit.User.Avatar;

                    if (!string.IsNullOrWhiteSpace(authorAvatar) &&
                        Uri.TryCreate(authorAvatar, UriKind.Absolute, out var avatarUri) &&
                        (avatarUri.Scheme == Uri.UriSchemeHttp || avatarUri.Scheme == Uri.UriSchemeHttps))
                    {
                        newEmbedBuilder.WithAuthor(authorName, authorAvatar);
                    }
                    else
                    {
                        newEmbedBuilder.WithAuthor(authorName);
                    }

                    var guildlist = Global.CommitFollowerChannels.Keys.ToList();
                    foreach (var guildId in guildlist)
                    {
                        var guild = Global.Client.GetGuild(guildId);
                        if (guild == null)
                        {
                            continue;
                        }
                        var channelids = Global.CommitFollowerChannels[guildId].ToList();
                        foreach (var channelId in channelids)
                        {
                            var channel = guild.GetTextChannel(channelId);
                            if (channel == null)
                            {
                                continue;
                            }
                            if (!await Logger.CheckBotPerms(guild) || !await Logger.CheckChannelPerms(channel))
                            {
                                Logger.LogMessage($"Commit Yetki Yetersizliği | Guild: {guild.Name}");
                                continue;
                            }
                            ;
                            await channel.SendMessageAsync("", false, newEmbedBuilder.Build());
                        }
                    }
                    sentCommits.Add(commit.Id);
                    storedCommits.Add(commit.Id);
                }
                Logger.LogMessage($"Stored: {storedCommits.Count} - Sended: {sentCommits.Count}");
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