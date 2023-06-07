using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

//
//MTExMTAxNjg2MDc0NjUzMDgzNg.G5Tmp_.pk-mcC5NNneSCwWOZuvFwOzovem4rieLdLKT3k

namespace RustTurkiye_Responder
{
    internal class Program
    {
        private DiscordSocketClient _client;
        private ulong _channelId = 448607745122369536;

        private static void Main(string[] args)
        {
            new Program().RunBotAsync().GetAwaiter().GetResult();
        }

        public async Task RunBotAsync()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All
            };

            _client = new DiscordSocketClient(config);

            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, "MTExMTAxNjg2MDc0NjUzMDgzNg.G5Tmp_.pk-mcC5NNneSCwWOZuvFwOzovem4rieLdLKT3k");

            await _client.StartAsync();

            _client.MessageReceived += MessageReceived;

            await Task.Delay(-1);
        }

        private Task MessageReceived(SocketMessage message)
        {
            if (message.Channel.Id == _channelId)
            {
                if (message.Author.Id != _client.CurrentUser.Id) // Botun kendi mesajlarını kontrol et
                {
                    if (message.Content.ToLower().Contains("wipe") || message.Content.ToLower().Contains("güncelleme") || message.Content.ToLower().Contains("global"))
                    {
                        DateTime today = DateTime.Today;
                        DateTime firstThursdayOfMonth = new DateTime(today.Year, today.Month, 1);

                        while (firstThursdayOfMonth.DayOfWeek != DayOfWeek.Thursday)
                        {
                            firstThursdayOfMonth = firstThursdayOfMonth.AddDays(1);
                        }

                        if (today > firstThursdayOfMonth)
                        {
                            firstThursdayOfMonth = firstThursdayOfMonth.AddMonths(1);

                            while (firstThursdayOfMonth.DayOfWeek != DayOfWeek.Thursday)
                            {
                                firstThursdayOfMonth = firstThursdayOfMonth.AddDays(1);
                            }
                        }

                        TimeZoneInfo cet = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
                        DateTime cetTime = TimeZoneInfo.ConvertTimeFromUtc(today.ToUniversalTime(), cet);
                        bool isDaylight = cet.IsDaylightSavingTime(cetTime);

                        if (isDaylight)
                        {
                            firstThursdayOfMonth = firstThursdayOfMonth.AddHours(21);
                        }
                        else
                        {
                            firstThursdayOfMonth = firstThursdayOfMonth.AddHours(22);
                        }

                        long timestamp = new DateTimeOffset(firstThursdayOfMonth).ToUnixTimeSeconds();
                        IUser user = message.Author;
                        string userTag = $"{user.Mention}";

                        EmbedBuilder embedBuilder = new EmbedBuilder();
                        embedBuilder.WithTitle(":information_source:  **Güncelleme Bilgisi**  :information_source:");
                        embedBuilder.WithDescription("`Her ayın ilk perşembesi (Yaz Dönemi 21:00 - Kış Dönemi 22:00) gelen güncelleme ile tüm sunuculara` ***Zorunlu Harita Sıfırlaması*** `atılır. BP Sıfırlaması ise sunucu sahibinin isteğine bağlıdır.` ");
                        embedBuilder.WithThumbnailUrl("https://yt3.googleusercontent.com/ytc/AL5GRJUOzRJWMKDaDQdJVVsXHCBcWQsOZYe3YZOfTj1k=s176-c-k-c0x00ffffff-no-rj-mo");
                        embedBuilder.WithFooter(DateTime.Now.ToString(), "https://cdn.discordapp.com/attachments/1060075799081918516/1072987687730032670/logo.png");
                        embedBuilder.AddField("Sonraki Güncelleme Tarihi", $"<t:{timestamp}:F>", false);
                        embedBuilder.AddField("Sonraki Güncellemeye Kalan Zaman", $"<t:{timestamp}:R>", false);
                        embedBuilder.AddField("Soran Kullanıcı", userTag, false);
                        embedBuilder.WithColor(Color.Blue);

                        return message.Channel.SendMessageAsync("", false, embedBuilder.Build());
                    }
                }
            }
            return Task.CompletedTask;
        }

        public static DateTime GetLocalDateTime(DateTime utcDateTime, TimeZoneInfo timeZone)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

            DateTime time = TimeZoneInfo.ConvertTime(utcDateTime, timeZone);

            return time;
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
    }
}