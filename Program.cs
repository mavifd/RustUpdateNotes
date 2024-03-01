using Discord;
using Discord.WebSocket;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace RT_Control
{
    internal class Program
    {
        private static string token = "MTExMTAxNjg2MDc0NjUzMDgzNg.G5Tmp_.pk-mcC5NNneSCwWOZuvFwOzovem4rieLdLKT3k";
        private static DiscordSocketClient _client;

        private static ulong _SohbetKanalID = 448607745122369536;
        private static ulong _CommitKanalID = 1134966799876763658;
        private static ulong _UpdateKanalID = 473843211954028544;
        private static ulong _SkinKanalID = 1004983958104199218;

        private static readonly HttpClient httpClient = new HttpClient();
        private static string commitApiUrl = "https://commits.facepunch.com/r/rust_reboot/?format=json";
        private static string skinApiUrl = "https://rustlabs.com/skins";

        private static HashSet<string> storedCommits = new HashSet<string>();
        private static HashSet<string> sentCommits = new HashSet<string>();

        private static string EnSonOyun = "";
        private static string EnSonSunucu = "";

        private static long _nextUpdateTimestamp = 0;

        private static bool _botReady = false;

        private static List<string> updateKeywords = new List<string>
        {
            "wipe",
            "güncelleme",
            "global",
            "update"
        };

        public class CommitData
        {
            public List<Commit> Results { get; set; }
        }

        public class Commit
        {
            public int id { get; set; }
            public string repo { get; set; }
            public string branch { get; set; }
            public string changeset { get; set; }
            public DateTime created { get; set; }
            public string message { get; set; }
            public User user { get; set; }
        }

        public class User
        {
            public string name { get; set; }
            public string avatar { get; set; }
        }

        private static HashSet<string> storedSkins = new HashSet<string>();
        private static HashSet<string> sentSkins = new HashSet<string>();

        private class SkinItem
        {
            public string Name { get; set; }
            public string Item { get; set; }
            public string Price { get; set; }
            public string Image { get; set; }
        }

        private static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        public async Task MainAsync()
        {
            Console.Title = "RT_Control";

            LogMessage("Starting...");

            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.All
            };

            _client = new DiscordSocketClient(config);

            _client.Log += Log;

            _client.MessageReceived += MessageReceived;

            _client.Ready += BotReady;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            while (!_botReady) { await Task.Delay(100); }

            LogMessage("Start done!");

            LogMessage("Initialize update checker...");

            await Task.Run(Initialize_UpdateChecker);

            LogMessage("Initialize update checker done!");

            LogMessage("Starting tasks...");

            _ = Task.Run(ResponderRunner);
            _ = Task.Run(CommitTracker);
            _ = Task.Run(SkinTracker);
            _ = Task.Run(UpdateChecker);

            LogMessage("Starting tasks done!");

            await Task.Delay(-1);
        }

        private static async Task Initialize_UpdateChecker()
        {
            LogMessage("[UpdateChecker] SteamCMD kontrol ediliyor...");
            if (File.Exists("C:\\steamcmd\\steamcmd.exe"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                LogMessage("[UpdateChecker] SteamCMD doğru dizine kurulu.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                LogMessage("[UpdateChecker] SteamCMD bulunamadı.");
                Console.ResetColor();
                await Task.Delay(5000);
                Environment.Exit(1);
            }

            if (!Directory.Exists("scripts"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                LogMessage("[UpdateChecker] Script dosyası oluşturuluyor.");

                Console.ResetColor();
                Directory.CreateDirectory("scripts");
                Console.ForegroundColor = ConsoleColor.Green;
                LogMessage("[UpdateChecker] Script dosyası içeriği oluşturuluyor.");
                Console.ResetColor();
                File.WriteAllText("scripts\\rustapp.txt", Properties.Resources.rustapp);
                File.WriteAllText("scripts\\rustserver.txt", Properties.Resources.rustserver);
            }
            else
            {
                LogMessage("[UpdateChecker] Script dosyası mevcut.");
                if (!File.Exists("scripts\\rustapp.txt") && !File.Exists("scripts\\rustserver.txt") && !File.Exists("scripts\\ruststaging.txt"))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    LogMessage("[UpdateChecker] Script dosyası içeriği oluşturuluyor.");
                    Console.ResetColor();
                    File.WriteAllText("scripts\\rustapp.txt", Properties.Resources.rustapp);
                    File.WriteAllText("scripts\\rustserver.txt", Properties.Resources.rustserver);
                }
                else
                {
                    LogMessage("[UpdateChecker] Script dosya içeriği mevcut.");
                }
            }

            if (!Directory.Exists("out"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                LogMessage("[UpdateChecker] Out dosyası oluşturuluyor.");
                Console.ResetColor();
                Directory.CreateDirectory("out");
            }

            LogMessage("[UpdateChecker] Başlangıç değerleri alınıyor...");

            while (true)
            {
                EnSonOyun = await GetRustVersionAsync("rustapp.txt");

                LogMessage($"[UpdateChecker] Oyun Başlangıç Değeri: {EnSonOyun}");

                EnSonSunucu = await GetRustVersionAsync("rustserver.txt");

                LogMessage($"[UpdateChecker] Sunucu Başlangıç Değeri: {EnSonSunucu}");

                if (IsValidVersion(EnSonOyun) && IsValidVersion(EnSonSunucu))
                {
                    break;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    LogMessage("[UpdateChecker] Geçersiz değer mevcut. Tekrarlanılıyor...");
                    Console.ResetColor();
                }
            }

            Console.ResetColor();
            LogMessage("[UpdateChecker] Döngüye giriliyor...");
        }

        private static async Task ResponderRunner()
        {
            while (true)
            {
                await ResponderUpdate();
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private static async Task CommitTracker()
        {
            while (true)
            {
                await CheckForNewCommits();
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private static async Task SkinTracker()
        {
            while (true)
            {
                await CheckForNewSkins();
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private static async Task CheckForNewSkins()
        {
            try
            {
                var response = await httpClient.GetStringAsync(skinApiUrl);

                if (response != null)
                {
                    List<SkinItem> skinData = ParseSkins(response);

                    if (skinData != null)
                    {
                        var newSkins = skinData.Select(item => item.Name).ToList();

                        if (storedSkins.Count == 0)
                        {
                            storedSkins.UnionWith(newSkins);
                        }
                        else
                        {
                            var differences = skinData.Where(skin => newSkins.Contains(skin.Name) && !storedSkins.Contains(skin.Name)).ToList();

                            if (differences.Any())
                            {
                                var channel = _client.GetChannel(_SkinKanalID) as IMessageChannel;

                                float totalcost = 0;
                                foreach (var skins in skinData) { totalcost = totalcost + float.Parse(skins.Price, CultureInfo.InvariantCulture.NumberFormat); }
                                var skincount = skinData.Count;

                                LogMessage($"[SkinTracker] Mağaza Yenilendi --> {skincount} yeni kostüm. Toplam Kostüm Değeri: {totalcost}$");

                                EmbedBuilder embedBuildformain = new EmbedBuilder();
                                embedBuildformain.WithTitle(":bell: MAĞAZA YENİLENDİ! | " + DateTime.Now.ToShortDateString() + " :bell:");
                                embedBuildformain.WithColor(Color.Blue);
                                embedBuildformain.WithDescription($"**{skincount}** yeni kostüm mağazaye eklendi. \n\n Toplam Kostüm Değeri: **{totalcost}$**");
                                embedBuildformain.WithUrl("https://store.steampowered.com/itemstore/252490/");
                                embedBuildformain.WithFooter(DateTime.Now.ToString(), "https://cdn.discordapp.com/attachments/1060075799081918516/1177909719772434432/logo.png?ex=657438e9&is=6561c3e9&hm=b17bd3166e83f5173abc9bca58df513f85e71ed445a1caf41a3429289ec78aa2&");
                                await channel.SendMessageAsync("@everyone", false, embedBuildformain.Build());

                                foreach (var skins in skinData)
                                {
                                    sentSkins.Add(skins.Name);
                                    LogMessage($"[SkinTracker] Yeni Skin --> {skins.Name}");
                                    EmbedBuilder embedBuilder = new EmbedBuilder();
                                    embedBuilder.WithTitle($"{skins.Name} - {skins.Price}$");
                                    embedBuilder.WithUrl("https://store.steampowered.com/itemstore/252490");
                                    embedBuilder.WithImageUrl("https://" + skins.Image);
                                    embedBuilder.WithFooter("Item Type: " + skins.Item);
                                    await channel.SendMessageAsync("", false, embedBuilder.Build());
                                }
                            }
                            storedSkins.UnionWith(newSkins);
                        }
                    }
                    else
                    {
                        Console.WriteLine("[SkinTracker] SkinData null.");
                    }
                }
                else
                {
                    Console.WriteLine("[SkinTracker] Response null.");
                }

                LogMessage($"[SkinTracker] Depolanan: {storedSkins.Count} - Gönderilen: {sentSkins.Count}");
            }
            catch (Exception ex)
            {
                LogMessage($"[SkinTracker] Hata oluştu: {ex.Message}");
            }
        }

        private static List<SkinItem> ParseSkins(string html)
        {
            List<SkinItem> skinItems = new List<SkinItem>();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var linkNodes = doc.DocumentNode.SelectNodes("//a");

            if (linkNodes != null)
            {
                for (int i = 0; i < Math.Min(21, linkNodes.Count); i++)
                {
                    var linkNode = linkNodes[i];
                    SkinItem skinItem = new SkinItem();
                    skinItem.Name = linkNode.GetAttributeValue("data-name", "");
                    skinItem.Item = linkNode.GetAttributeValue("data-item", "");
                    skinItem.Price = linkNode.GetAttributeValue("data-price", "");
                    var imgNode = linkNode.SelectSingleNode(".//img");
                    if (imgNode != null) skinItem.Image = imgNode.GetAttributeValue("data-src", "").TrimStart('/');
                    if (skinItem.Name != null && skinItem.Item != null && skinItem.Price != null && skinItem.Image != null) skinItems.Add(skinItem);
                }
            }

            return skinItems;
        }

        private static async Task UpdateChecker()
        {
            while (true)
            {
                string mevcutOyun = await GetRustVersionAsync("rustapp.txt");

                LogMessage($"[UpdateChecker] Oyun Mevcut Sürüm: {mevcutOyun}");

                string mevcutSunucu = await GetRustVersionAsync("rustserver.txt");

                LogMessage($"[UpdateChecker] Sunucu Mevcut Sürüm: {mevcutSunucu}");

                await CheckAndUpdateVersion(
                    EnSonOyun,
                    mevcutOyun,
                    ":radioactive: **Oyuncular için yeni bir Güncelleme geldi!** :radioactive:",
                    "Güncellemeyi görmüyorsanız, Steaminizi yeniden başlatın.",
                    false);

                await CheckAndUpdateVersion(
                   EnSonSunucu,
                   mevcutSunucu,
                   ":radioactive: **Sunucular için yeni bir Güncelleme geldi!** :radioactive:",
                   "Sunucu sahipleri, sunucularını güncelleyebilir.",
                   true);

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private static async Task<string> GetRustVersionAsync(string scriptFileName)
        {
            string outputFileName = $"out/{Path.GetFileNameWithoutExtension(scriptFileName)}out.txt";
            string scriptpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", scriptFileName);

            try
            {
                File.Delete(outputFileName);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c C:\\steamcmd\\steamcmd.exe +runscript {scriptpath} > {outputFileName}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    process.WaitForExit();

                    using (StreamReader fileReader = new StreamReader(outputFileName))
                    {
                        string fileContent = await fileReader.ReadToEndAsync();

                        int buildIdIndex = fileContent.IndexOf("\"buildid\"") + 12;
                        return fileContent.Substring(buildIdIndex, 8);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"[UpdateChecker] Genel Hata: {ex.Message}");
                return "not valid";
            }
        }

        private static async Task CheckAndUpdateVersion(string MainVersion, string CurrentVersion, string message, string description, bool server)
        {
            if (IsValidVersion(CurrentVersion) && CurrentVersion != MainVersion)
            {
                if (server) EnSonSunucu = CurrentVersion;
                else EnSonOyun = CurrentVersion;

                LogMessage($"{message} [{CurrentVersion}]");

                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.WithTitle(message);
                embedBuilder.WithDescription(description);
                embedBuilder.WithColor(Color.Blue);
                embedBuilder.WithThumbnailUrl("https://yt3.googleusercontent.com/ytc/AL5GRJUOzRJWMKDaDQdJVVsXHCBcWQsOZYe3YZOfTj1k=s176-c-k-c0x00ffffff-no-rj-mo");
                embedBuilder.WithFooter(DateTime.Now.ToString(), "https://cdn.discordapp.com/attachments/1060075799081918516/1177909719772434432/logo.png?ex=657438e9&is=6561c3e9&hm=b17bd3166e83f5173abc9bca58df513f85e71ed445a1caf41a3429289ec78aa2&");
                embedBuilder.AddField("Yeni Sürüm Numarası", CurrentVersion, true); ;

                var channel = _client.GetChannel(_UpdateKanalID) as IMessageChannel;
                await channel.SendMessageAsync("@everyone", false, embedBuilder.Build());
            }
        }

        private static bool IsValidVersion(string version)
        {
            return version != null && version.Length == 8 && int.TryParse(version, out _);
        }

        private static async Task CheckForNewCommits()
        {
            try
            {
                var response = await httpClient.GetStringAsync(commitApiUrl);
                var commitData = JsonConvert.DeserializeObject<CommitData>(response);

                if (commitData?.Results.Count() != null)
                {
                    var newCommits = commitData.Results.Select(commit => commit.message).ToList();

                    if (storedCommits.Count == 0)
                    {
                        storedCommits.UnionWith(newCommits);
                    }
                    else
                    {
                        var differences = commitData.Results.Where(commit => newCommits.Contains(commit.message) && !storedCommits.Contains(commit.message)).ToList();

                        if (differences.Any())
                        {
                            foreach (var commit in differences)
                            {
                                sentCommits.Add(commit.message);
                                LogMessage($"[CommitTracker] Yeni yorum --> {commit.message}");
                                EmbedBuilder embedBuilder = new EmbedBuilder();
                                embedBuilder.WithTitle($"{commit.user.name} \n {commit.branch}");
                                embedBuilder.WithDescription(commit.message);
                                embedBuilder.WithColor(Color.Blue);
                                embedBuilder.WithThumbnailUrl(commit.user.avatar);
                                embedBuilder.WithFooter(DateTime.Now.ToString(), "https://cdn.discordapp.com/attachments/1060075799081918516/1177909719772434432/logo.png?ex=657438e9&is=6561c3e9&hm=b17bd3166e83f5173abc9bca58df513f85e71ed445a1caf41a3429289ec78aa2&");
                                embedBuilder.AddField("ID", commit.id, true);
                                embedBuilder.AddField("ChangeSet", commit.changeset, true);

                                var channel = _client.GetChannel(_CommitKanalID) as IMessageChannel;
                                await channel.SendMessageAsync("", false, embedBuilder.Build());
                            }
                        }
                        storedCommits.UnionWith(newCommits);
                    }
                }
                else
                {
                    LogMessage("[CommitTracker] Yeni veri alınamadı.");
                }

                LogMessage($"[CommitTracker] Depolanan: {storedCommits.Count} - Gönderilen: {sentCommits.Count}");
            }
            catch (Exception ex)
            {
                LogMessage($"[CommitTracker] Hata oluştu: {ex.Message}");
            }
        }

        private static Task ResponderUpdate()
        {
            int DayTimeHour = 19; //18 YAZ, 19 KIŞ.
            DateTime Today = DateTime.Today;
            Today = Today.AddHours(DayTimeHour);
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
                _nextUpdateTimestamp = new DateTimeOffset(NextMonthFirstThursday, TimeSpan.Zero).ToUnixTimeSeconds();
            }
            else
            {
                DateTime ThisMonthFirstThursday = new DateTime(Today.Year, Today.Month, 1);
                while (ThisMonthFirstThursday.DayOfWeek != DayOfWeek.Thursday) { ThisMonthFirstThursday = ThisMonthFirstThursday.AddDays(1); }
                ThisMonthFirstThursday = ThisMonthFirstThursday.AddHours(DayTimeHour);
                _nextUpdateTimestamp = new DateTimeOffset(ThisMonthFirstThursday, TimeSpan.Zero).ToUnixTimeSeconds();
            }

            DateTimeOffset LocalTimeOffset = DateTimeOffset.FromUnixTimeSeconds(_nextUpdateTimestamp);
            DateTime LocalTime = LocalTimeOffset.LocalDateTime;
            LogMessage($"[Responder] Sonraki Güncelleme Tarihi: {LocalTime}");
            return Task.CompletedTask;
        }

        private static Task MessageReceived(SocketMessage message)
        {
            if (message.Channel.Id == _SohbetKanalID)
            {
                if (message.Author.Id != _client.CurrentUser.Id)
                {
                    if (updateKeywords.Any(keyword => message.Content.ToLower().Contains(keyword)))
                    {
                        LogMessage("[Responder] Güncelleme sorusu cevaplanıyor...");

                        IUser user = message.Author;
                        string userTag = $"{user.Mention}";

                        EmbedBuilder embedBuilder = new EmbedBuilder();
                        embedBuilder.WithTitle(":information_source:  **Güncelleme Bilgisi**  :information_source:");
                        embedBuilder.WithDescription("`Her ayın ilk perşembesi (Yaz Dönemi 21:00 - Kış Dönemi 22:00) gelen güncelleme ile tüm sunuculara` ***Zorunlu Harita Sıfırlaması*** `atılır. BP Sıfırlaması ise sunucu sahibinin isteğine bağlıdır.` ");
                        embedBuilder.WithThumbnailUrl("https://yt3.googleusercontent.com/ytc/AL5GRJUOzRJWMKDaDQdJVVsXHCBcWQsOZYe3YZOfTj1k=s176-c-k-c0x00ffffff-no-rj-mo");
                        embedBuilder.WithFooter(DateTime.Now.ToString(), "https://cdn.discordapp.com/attachments/1060075799081918516/1177909719772434432/logo.png?ex=657438e9&is=6561c3e9&hm=b17bd3166e83f5173abc9bca58df513f85e71ed445a1caf41a3429289ec78aa2&");
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

        private Task BotReady()
        {
            _botReady = true;
            return Task.CompletedTask;
        }

        private Task Log(LogMessage arg)
        {
            string logEntry = $"{DateTime.Now} | [DiscordConnection] {arg}";
            Console.WriteLine(logEntry);
            return Task.CompletedTask;
        }

        private static void LogMessage(string message)
        {
            string logEntry = $"{DateTime.Now} | {message}";
            Console.WriteLine(logEntry);
        }
    }
}