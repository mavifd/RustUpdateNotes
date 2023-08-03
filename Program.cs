using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

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
                        IUser user = message.Author;
                        string userTag = $"{user.Mention}";

                        DateTime now = DateTime.Now;
                        DateTime nextUpdate = GetNextUpdateDateTime(now);

                        TimeSpan timeRemaining = nextUpdate - now;

                        EmbedBuilder embedBuilder = new EmbedBuilder();
                        embedBuilder.WithTitle(":information_source:  **Güncelleme Bilgisi**  :information_source:");
                        embedBuilder.WithDescription("`Her ayın ilk perşembesi (Yaz Dönemi 21:00 - Kış Dönemi 22:00) gelen güncelleme ile tüm sunuculara` ***Zorunlu Harita Sıfırlaması*** `atılır. BP Sıfırlaması ise sunucu sahibinin isteğine bağlıdır.` ");
                        embedBuilder.WithThumbnailUrl("https://yt3.googleusercontent.com/ytc/AL5GRJUOzRJWMKDaDQdJVVsXHCBcWQsOZYe3YZOfTj1k=s176-c-k-c0x00ffffff-no-rj-mo");
                        embedBuilder.WithFooter(DateTime.Now.ToString(), "https://cdn.discordapp.com/attachments/1060075799081918516/1072987687730032670/logo.png");

                        embedBuilder.AddField("Sonraki Güncelleme Tarihi", $"Perşembe, {nextUpdate.ToString("dd MMMM yyyy")} saat {nextUpdate.ToString("HH:mm")}", false);
                        embedBuilder.AddField("Sonraki Güncellemeye Kalan Zaman", $"{timeRemaining.Days} gün, {timeRemaining.Hours} saat, {timeRemaining.Minutes} dakika", false);
                        embedBuilder.AddField("Soran Kullanıcı", userTag, false);
                        embedBuilder.WithColor(Color.Blue);

                        return message.Channel.SendMessageAsync("", false, embedBuilder.Build());
                    }
                }
            }
            return Task.CompletedTask;
        }

        private DateTime GetNextUpdateDateTime(DateTime currentDate)
        {
            int daysUntilNextThursday = ((int)DayOfWeek.Thursday - (int)currentDate.DayOfWeek + 7) % 7;
            DateTime nextThursday = currentDate.Date.AddDays(daysUntilNextThursday).AddHours(21);

            if (currentDate >= nextThursday)
            {
                nextThursday = nextThursday.AddMonths(1);
            }

            return nextThursday;
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
    }
}
