using Discord;
using Discord.WebSocket;
using RustUpdateNotes.GlobalClass;
using RustUpdateNotes.LoggerClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RustUpdateNotes.ResponderClass
{
    public static class Responder
    {
        private static readonly List<string> updateKeys = new List<string> { "wipe", "güncelleme", "global", "update" };
        private static long nextUpdateTimeStamp = 0;

        public static async Task Responder_Runner()
        {

            while (true)
            {
                var maintask = CheckUpdateDate();
                var controltask = Task.Delay(TimeSpan.FromMinutes(5));
                var completedTask = await Task.WhenAny(maintask, controltask);
                if (completedTask == maintask)
                {
                    Global.UpdateMessageRunner_Succes++;
                }
                else
                {
                    Global.UpdateMessageRunner_Fail++;
                    Logger.LogMessage($"UpdateMessageRunner Timeout (5 minute)");
                    await Logger.DiscordMessage($"UpdateMessageRunner Timeout (5 minute)", true);
                }
                await Task.Delay(TimeSpan.FromHours(1));
            }
        }

        public static async Task CheckUpdateDate()
        {
            try
            {
                int DayTimeHour = 18; //18 YAZ, 19 KIŞ.
                DateTime Today = DateTime.Today.AddHours(DayTimeHour); ;
                long TodayTimeStamp = ((DateTimeOffset)Today).ToUnixTimeSeconds();

                DateTime Current = DateTime.Today;
                while (Current.Day != 1) { Current = Current.AddDays(-1); }
                Current = Current.AddDays((4 - (int)Current.DayOfWeek + 7) % 7);
                Current = Current.AddHours(DayTimeHour);

                long FirstThursdayTimeStamp = ((DateTimeOffset)Current).ToUnixTimeSeconds();
                if (TodayTimeStamp > FirstThursdayTimeStamp)
                {
                    DateTime NextMonthFirstThursday = new DateTime(Today.Year, Today.Month, 1).AddMonths(1);
                    while (NextMonthFirstThursday.DayOfWeek != DayOfWeek.Thursday) { NextMonthFirstThursday = NextMonthFirstThursday.AddDays(1); }
                    NextMonthFirstThursday = NextMonthFirstThursday.AddHours(DayTimeHour);
                    nextUpdateTimeStamp = new DateTimeOffset(NextMonthFirstThursday, TimeSpan.Zero).ToUnixTimeSeconds();
                }
                else
                {
                    DateTime ThisMonthFirstThursday = new DateTime(Today.Year, Today.Month, 1);
                    while (ThisMonthFirstThursday.DayOfWeek != DayOfWeek.Thursday) { ThisMonthFirstThursday = ThisMonthFirstThursday.AddDays(1); }
                    ThisMonthFirstThursday = ThisMonthFirstThursday.AddHours(DayTimeHour);
                    nextUpdateTimeStamp = new DateTimeOffset(ThisMonthFirstThursday, TimeSpan.Zero).ToUnixTimeSeconds();
                }
                DateTimeOffset LocalTimeOffset = DateTimeOffset.FromUnixTimeSeconds(nextUpdateTimeStamp);
                DateTime LocalTime = LocalTimeOffset.LocalDateTime;
                Logger.LogMessage($"[Responder] Sonraki Güncelleme Tarihi: {LocalTime}");

                EmbedBuilder embedBuilder = new EmbedBuilder()
                .WithTitle(":information_source:  **Güncelleme Bilgisi**  :information_source:")
                .WithDescription("Her ayın ilk perşembesi (Yaz Dönemi 21:00 - Kış Dönemi 22:00) gelen güncelleme ile tüm sunuculara **Zorunlu Harita Sıfırlaması** atılır.\n**BP Sıfırlaması**(Blueprint/Öğrenilen Eşyalar) ise sunucu sahibinin isteğine bağlıdır.")
                .WithThumbnailUrl("https://yt3.googleusercontent.com/HPu-kTkwgN4mPxO6_PJThrtbPQEL_esHXjbPVp7bR5SF3H0HX_p6ub960hiH-D5WiDtPTosOXw=s176-c-k-c0x00ffffff-no-rj")
                .AddField("Sonraki Güncelleme Tarihi:", $"<t:{nextUpdateTimeStamp}:F>", false)
                .AddField("Sonraki Güncellemeye Kalan Zaman:", $"<t:{nextUpdateTimeStamp}:R>", false)
                .WithColor(Discord.Color.Blue)
                .WithFooter($"Son Güncelleme: {DateTime.Now:dd/MM HH:mm}");
                var guildlist = Global.UpdateDateChannels.Keys.ToList();
                foreach (var guildId in guildlist)
                {
                    var guild = Global.Client.GetGuild(guildId);
                    if (guild == null) continue;
                    var channelids = Global.UpdateDateChannels[guildId].ToList();
                    foreach (var channelId in channelids)
                    {
                        var channel = guild.GetTextChannel(channelId);
                        if (channel == null) continue;
                        var testr = await Logger.CheckBotPerms(guild);
                        if (!await Logger.CheckBotPerms(guild) || !await Logger.CheckChannelPerms(channel)) { Logger.LogMessage($"Güncelleme Tarihi Yetki Yetersizliği | Guild: {guild.Name}"); continue; };
                        var messages = await channel.GetMessagesAsync(limit: 1).FlattenAsync();
                        var lastMessage = messages.FirstOrDefault() is IUserMessage userMessage ? userMessage : null;
                        if (lastMessage != null && lastMessage.Author.Id == Global.Client.CurrentUser.Id) await lastMessage.ModifyAsync(msg => msg.Embed = embedBuilder.Build());
                        else await channel.SendMessageAsync("", false, embedBuilder.Build());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogMessage($"Error - Responder_Date: {ex}");
                await Logger.DiscordMessage($"Error - Responder_Date: {ex}", true);
            }
        }

        public static async Task MessageReceived(SocketMessage message)
        {
            try
            {
                if (message.Channel.Id == Global.MainDiscordSohbet)
                {
                    IUser user = message.Author;
                    string userTag = $"{user.Mention}";
                    if (message.Author.Id == Global.Client.CurrentUser.Id) return;
                    if (updateKeys.Any(keyword => message.Content.ToLower().Contains(keyword)))
                    {
                        Logger.LogMessage("[Responder] Güncelleme sorusu cevaplanıyor...");
                        EmbedBuilder embedBuilder = new EmbedBuilder()
                        .WithTitle(":information_source:  **Güncelleme Bilgisi**  :information_source:")
                        .WithDescription("Her ayın ilk perşembesi (Yaz Dönemi 21:00 - Kış Dönemi 22:00) gelen güncelleme ile tüm sunuculara **Zorunlu Harita Sıfırlaması** atılır.\n**BP Sıfırlaması**(Blueprint/Öğrenilen Eşyalar) ise sunucu sahibinin isteğine bağlıdır.")
                        .WithThumbnailUrl("https://yt3.googleusercontent.com/HPu-kTkwgN4mPxO6_PJThrtbPQEL_esHXjbPVp7bR5SF3H0HX_p6ub960hiH-D5WiDtPTosOXw=s176-c-k-c0x00ffffff-no-rj")
                        .WithFooter(DateTime.Now.ToString(), "https://lh3.googleusercontent.com/a/ACg8ocJveuYqbU6KTFvsKpkmNLtB35Gd8-fsAbZzu3JVknZGDw=s288-c-no")
                        .AddField("Sonraki Güncelleme Tarihi:", $"<t:{nextUpdateTimeStamp}:F>", false)
                        .AddField("Sonraki Güncellemeye Kalan Zaman:", $"<t:{nextUpdateTimeStamp}:R>", false)
                        .AddField("Soran Kullanıcı", userTag, false)
                        .WithColor(Color.Blue);
                        await message.Channel.SendMessageAsync("", false, embedBuilder.Build());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogMessage($"Error - Responder_Message: {ex}");
                await Logger.DiscordMessage($"Error - Responder_Message: {ex}", true);
            }
        }
    }
}