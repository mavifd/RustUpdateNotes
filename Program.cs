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

            CalculateNextUpdateTimestamp();

            Timer timer = new Timer(60_000);
            timer.Elapsed += (sender, e) => CalculateNextUpdateTimestamp();
            timer.Start();

            await Task.Delay(-1);
        }

        private void CalculateNextUpdateTimestamp()
        {

            //bugün 21:00 timestamp alma
            DateTime bugun = DateTime.Today;
            DateTime saat21 = bugun.AddHours(21);
            long bugüntimestamp = ((DateTimeOffset)saat21).ToUnixTimeSeconds();

            //ayın ilk perşembesi 21:00 timestamp alma
            DateTime suAn = DateTime.Now;
            DateTime ilkGun = new DateTime(suAn.Year, suAn.Month, 1);
            int gunDegeri = (int)ilkGun.DayOfWeek;
            DateTime ilkPersembe = ilkGun.AddDays((4 - gunDegeri + 7) % 7);
            DateTime saat21ilkay = ilkPersembe.AddHours(21);
            long ayinilkpersembesitimestamp = ((DateTimeOffset)saat21ilkay).ToUnixTimeSeconds();


            //loglama
            Console.WriteLine(bugüntimestamp.ToString());
            Console.WriteLine(ayinilkpersembesitimestamp.ToString());


            //ilk persembe geçildi mi?
            if(bugüntimestamp > ayinilkpersembesitimestamp) 
            {
                //ilk perşembe geçildi
                Console.WriteLine("ilk perşembe geçilmiş");
                DateTime birSonrakiAyinIlkPersembesi = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(1);
                while (birSonrakiAyinIlkPersembesi.DayOfWeek != DayOfWeek.Thursday)
                {
                    birSonrakiAyinIlkPersembesi = birSonrakiAyinIlkPersembesi.AddDays(1);
                }
                birSonrakiAyinIlkPersembesi = birSonrakiAyinIlkPersembesi.AddHours(21);
                _nextUpdateTimestamp = new DateTimeOffset(birSonrakiAyinIlkPersembesi).ToUnixTimeSeconds();
            }
            else 
            {
                //ilk perşembe geçilmedi
                Console.WriteLine("ilk perşembe geçilmemiş");
                DateTime buAyinIlkPersembesi = new DateTime(bugun.Year, bugun.Month, 1);
                while (buAyinIlkPersembesi.DayOfWeek != DayOfWeek.Thursday)
                {
                    buAyinIlkPersembesi = buAyinIlkPersembesi.AddDays(1);
                }
                buAyinIlkPersembesi = buAyinIlkPersembesi.AddHours(21);
                _nextUpdateTimestamp = new DateTimeOffset(buAyinIlkPersembesi).ToUnixTimeSeconds();
            }
        }

        private Task MessageReceived(SocketMessage message)
        {
            if (message.Channel.Id == _channelId)
            {
                if (message.Author.Id != _client.CurrentUser.Id) 
                {
                    if (message.Content.ToLower().Contains("wipe") || message.Content.ToLower().Contains("güncelleme") || message.Content.ToLower().Contains("global"))
                    {
                        IUser user = message.Author;
                        string userTag = $"{user.Mention}";

                        EmbedBuilder embedBuilder = new EmbedBuilder();
                        embedBuilder.WithTitle(":information_source:  **Güncelleme Bilgisi**  :information_source:");
                        embedBuilder.WithDescription("`Her ayın ilk perşembesi (Yaz Dönemi 21:00 - Kış Dönemi 22:00) gelen güncelleme ile tüm sunuculara` ***Zorunlu Harita Sıfırlaması*** `atılır. BP Sıfırlaması ise sunucu sahibinin isteğine bağlıdır.` ");
                        embedBuilder.WithThumbnailUrl("https://yt3.googleusercontent.com/ytc/AL5GRJUOzRJWMKDaDQdJVVsXHCBcWQsOZYe3YZOfTj1k=s176-c-k-c0x00ffffff-no-rj-mo");
                        embedBuilder.WithFooter(DateTime.Now.ToString(), "https://cdn.discordapp.com/attachments/1060075799081918516/1072987687730032670/logo.png");
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
