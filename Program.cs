using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace RustTurkiye_Responder
{
    internal class Program
    {
        private DiscordSocketClient _client;
        private ulong _channelId = 448607745122369536;
        private long _nextUpdateTimestamp = 0;

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

            // İlk başta bir sonraki güncelleme zamanını hesaplayın
            CalculateNextUpdateTimestamp();

            // Timer'ı başlatarak her dakika bir sonraki güncelleme zamanını yeniden hesaplayın
            Timer timer = new Timer(60_000); // 60,000 milisaniye = 1 dakika
            timer.Elapsed += (sender, e) => CalculateNextUpdateTimestamp();
            timer.Start();

            await Task.Delay(-1);
        }

        private void CalculateNextUpdateTimestamp()
        {
            DateTime bugun = DateTime.Today;
            DateTime birSonrakiAyinIlkPersembesi = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(1);

            while (birSonrakiAyinIlkPersembesi.DayOfWeek != DayOfWeek.Thursday)
            {
                birSonrakiAyinIlkPersembesi = birSonrakiAyinIlkPersembesi.AddDays(1);
            }

            birSonrakiAyinIlkPersembesi = birSonrakiAyinIlkPersembesi.AddHours(21);

            TimeZoneInfo cet = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            DateTime cetZamani = TimeZoneInfo.ConvertTimeFromUtc(birSonrakiAyinIlkPersembesi.ToUniversalTime(), cet);
            bool yazZamani = cet.IsDaylightSavingTime(cetZamani);

            if (yazZamani)
            {
                birSonrakiAyinIlkPersembesi = birSonrakiAyinIlkPersembesi.AddHours(1);
            }

            _nextUpdateTimestamp = new DateTimeOffset(birSonrakiAyinIlkPersembesi).ToUnixTimeSeconds();
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

                        // Şu anki zamanı Central European Standard Time (CET) zaman dilimine göre hesaplayın
                        TimeZoneInfo cet = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
                        DateTime cetZamani = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cet);

                        EmbedBuilder embedBuilder = new EmbedBuilder();
                        embedBuilder.WithTitle(":information_source:  **Güncelleme Bilgisi**  :information_source:");
                        embedBuilder.WithDescription("`Her ayın ilk perşembesi (Yaz Dönemi 21:00 - Kış Dönemi 22:00) gelen güncelleme ile tüm sunuculara` ***Zorunlu Harita Sıfırlaması*** `atılır. BP Sıfırlaması ise sunucu sahibinin isteğine bağlıdır.` ");
                        embedBuilder.WithThumbnailUrl("https://yt3.googleusercontent.com/ytc/AL5GRJUOzRJWMKDaDQdJVVsXHCBcWQsOZYe3YZOfTj1k=s176-c-k-c0x00ffffff-no-rj-mo");
                        embedBuilder.WithFooter(DateTime.Now.ToString(), "https://cdn.discordapp.com/attachments/1060075799081918516/1072987687730032670/logo.png");

                        // Eğer şu anki zaman 21:00'dan sonra ise, bir sonraki güncelleme zamanını bir sonraki ayınkini hesaplayın
                        if (cetZamani.Hour >= 21 && cetZamani.Month == DateTimeOffset.FromUnixTimeSeconds(_nextUpdateTimestamp).DateTime.Month)
                        {
                            CalculateNextUpdateTimestamp();
                        }

                        embedBuilder.AddField("Sonraki Güncelleme Tarihi", $"<t:{_nextUpdateTimestamp}:F>", false);
                        embedBuilder.AddField("Sonraki Güncellemeye Kalan Zaman", $"<t:{_nextUpdateTimestamp}:R>", false);
                        embedBuilder.AddField("Soran Kullanıcı", userTag, false);
                        embedBuilder.WithColor(Color.Blue);

                        return message.Channel.SendMessageAsync("", false, embedBuilder.Build());
                    }
                }
            }
            return Task.CompletedTask;
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }
    }
}
