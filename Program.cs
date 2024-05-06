using Discord;
using Discord.WebSocket;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RT_Control
{
    internal class Program
    {
        private static string token = "MTExMTAxNjg2MDc0NjUzMDgzNg.G5Tmp_.pk-mcC5NNneSCwWOZuvFwOzovem4rieLdLKT3k";
        private static DiscordSocketClient _client;

        private static ulong _SohbetKanalID = 1223037877911556110;
        private static ulong _CommitKanalID = 1223059020269486101;
        private static ulong _UpdateKanalID = 1223058923775594588;
        private static ulong _SkinKanalID = 1223058996970131596;

        private static readonly HttpClient httpClient = new HttpClient();
        private static string commitApiUrl = "https://commits.facepunch.com/r/rust_reboot/?format=json";
        private static string skinApiUrl = "https://rust.scmm.app/store";

        private static HashSet<string> storedCommits = new HashSet<string>();

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

        private class SkinItem
        {
            public string Name { get; set; }
            public string Item { get; set; }
            public string Price { get; set; }
            public string Image { get; set; }
        }

        private static HashSet<string> storedSkins = new HashSet<string>();

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
                await Task.Delay(TimeSpan.FromMinutes(5));
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

        private static bool skinControlTimer = false;

        private static async Task CheckForNewSkins()
        {
            try
            {
                var cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(1));

                var response_main = await httpClient.GetAsync(skinApiUrl, cancellationTokenSource.Token);

                if (!response_main.IsSuccessStatusCode) { LogMessage("[SkinTracker] response contect null."); return; }

                var response = await response_main.Content.ReadAsStringAsync();

                if (response == "") { LogMessage("[SkinTracker] response null."); return; }

                List<SkinItem> skinData = ParseSkins(response);

                if (skinData.Count == 0) { LogMessage("[SkinTracker] skinData null."); return; }

                var newSkins = skinData.Select(Skin => Skin.Name).ToList();

                foreach (var item in skinData)
                {
                    LogMessage($"[SkinTracker] Name: {item.Name} | Price: {item.Price} | Type: {item.Item} | Image: {item.Image}");
                }

                if (storedSkins.Count == 0)
                {
                    storedSkins.Clear();
                    storedSkins.UnionWith(newSkins);
                }
                else
                {
                    var differences = skinData.Where(Skin => newSkins.Contains(Skin.Name) && !storedSkins.Contains(Skin.Name)).ToList();
                    if (differences.Any())
                    {
                        if (!skinControlTimer)
                        {
                            await Task.Delay(TimeSpan.FromMinutes(10));
                            skinControlTimer = true;
                            return;
                        }
                        skinControlTimer = false;

                        var ourimage = CreateBigImage(skinData);
                        ourimage.Save("skinimage.png", System.Drawing.Imaging.ImageFormat.Png);

                        var channel = _client.GetChannel(_SkinKanalID) as IMessageChannel;
                        var skincount = skinData.Count;
                        float totalcost = 0;
                        foreach (var skin in skinData)
                        {
                            float price = float.Parse(skin.Price.Replace("$", ""), CultureInfo.InvariantCulture);
                            totalcost += price;
                        }
                        LogMessage($"[SkinTracker] Mağaza Yenilendi --> {skincount} yeni kostüm. Toplam Kostüm Değeri: {totalcost}$");
                        EmbedBuilder embedBuildformain = new EmbedBuilder();
                        embedBuildformain.WithTitle(":bell: MAĞAZA YENİLENDİ! | " + DateTime.Now.ToShortDateString() + " :bell:");
                        embedBuildformain.WithColor(Discord.Color.Blue);
                        embedBuildformain.WithDescription($"**{skincount}** yeni kostüm mağazaye eklendi.\n\n Toplam Kostüm Değeri: **{totalcost}$**");
                        embedBuildformain.WithUrl("https://store.steampowered.com/itemstore/252490/");
                        embedBuildformain.WithFooter(DateTime.Now.ToString(), "https://lh3.googleusercontent.com/a/ACg8ocJveuYqbU6KTFvsKpkmNLtB35Gd8-fsAbZzu3JVknZGDw=s288-c-no");
                        await channel.SendMessageAsync("@everyone", false, embedBuildformain.Build());

                        using (var fileStream = new FileStream("skinimage.png", FileMode.Open))
                        {
                            var memoryStream = new MemoryStream();
                            fileStream.CopyTo(memoryStream);
                            memoryStream.Position = 0;
                            await channel.SendFileAsync(memoryStream, "skinimage.png");
                        }
                        File.Delete("skinimage.png");
                        storedSkins.Clear();
                        storedSkins.UnionWith(newSkins);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogMessage("[SkinTracker] İstek zaman aşımına uğradı.");
                return;
            }
            catch (HttpRequestException ex)
            {
                LogMessage("[SkinTracker] HTTP request failed: " + ex.Message);
            }
            catch (Exception ex)
            {
                LogMessage("[SkinTracker] An error occurred: " + ex.Message);
            }
        }

        private static System.Drawing.Image CreateBigImage(List<SkinItem> skindata)
        {
            var imageUrls = skindata.Select(Skin => Skin.Image).ToList();
            var skinNames = skindata.Select(Skin => Skin.Name).ToList();
            var skinPrices = skindata.Select(Skin => Skin.Price).ToList();
            var skinType = skindata.Select(Skin => Skin.Item).ToList();

            int imageSize = (int)Math.Ceiling(Math.Sqrt(imageUrls.Count)); // Calculate the size of the square
            int squareSize = imageSize * 400; // Set the size of the square (default to 200 pixels)
            Bitmap combinedImage = new Bitmap(squareSize, squareSize);

            using (Graphics g = Graphics.FromImage(combinedImage))
            {
                g.Clear(System.Drawing.Color.FromArgb(255, 70, 70, 70)); // Set the background to white
                int x = 0, y = 0;
                int index = 0;

                foreach (var url in imageUrls)
                {
                    try
                    {
                        using (WebClient client = new WebClient())
                        {
                            byte[] imageData = client.DownloadData(url);
                            using (MemoryStream stream = new MemoryStream(imageData))
                            {
                                System.Drawing.Image img = System.Drawing.Image.FromStream(stream);

                                // Draw each image at 200x200 size
                                g.DrawImage(img, x * 400, y * 400, 350, 350);

                                var skinname = skinNames[index];
                                var skintype = skinType[index];
                                var skinprice = skinPrices[index];

                                // Draw the text
                                if (index < imageUrls.Count)
                                {
                                    Font font = new Font("Arial", 18, FontStyle.Bold);
                                    Font font2 = new Font("Arial", 12);
                                    SizeF textSize = g.MeasureString(skinname, font);
                                    if (textSize.Width > 400 || textSize.Height > 400) // Check if text fits within 200x200
                                    {
                                        float scale = Math.Min(400 / textSize.Width, 400 / textSize.Height);
                                        font = new Font("Arial", 18 * scale, FontStyle.Bold);
                                    }
                                    float textX = x * 400 + (400 - textSize.Width) / 2;
                                    float textY = (y + 1) * 400 - textSize.Height; // Place the text at the bottom of the image
                                    g.DrawString(skinname, font, Brushes.White, new PointF(textX, textY - 30));
                                    g.DrawString(skinprice, font, Brushes.ForestGreen, new PointF(textX, textY));
                                    g.DrawString(skintype, font2, Brushes.DarkGray, new PointF(textX + 75, textY + 5));
                                }

                                // Draw the border
                                using (Pen pen = new Pen(System.Drawing.Color.Black, 2))
                                {
                                    g.DrawRectangle(pen, x * 400, y * 400, 400, 400);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage("[SkinTracker] Creating image fail: " + ex.Message);
                    }

                    index++;
                    x++;
                    if (x >= imageSize)
                    {
                        x = 0;
                        y++;
                    }
                    if (index >= imageSize * imageSize) // If it exceeds the square size, exit the loop
                        break;
                }
            }

            return combinedImage;
        }

        private static List<string> ExtractImageUrlsStartingWith(string html)
        {
            List<string> imageUrls = new List<string>();
            string pattern = @"<img.*?src=""(.*?)"".*?>";
            MatchCollection matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                string src = match.Groups[1].Value;
                if (!src.StartsWith("https://avatars", StringComparison.OrdinalIgnoreCase)
                    && (src.StartsWith("https://steamcommunity-a.akamaihd.net/economy", StringComparison.OrdinalIgnoreCase) ||
                    src.StartsWith("https://files.facepunch", StringComparison.OrdinalIgnoreCase))) imageUrls.Add(src);
            }
            return imageUrls;
        }

        private static List<SkinItem> ParseSkins(string html)
        {
            List<SkinItem> skinItems = new List<SkinItem>();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            var storeItems = doc.DocumentNode.SelectNodes("//div[@class='store-item full-height']");
            List<string> imageUrls = ExtractImageUrlsStartingWith(html);
            if (storeItems != null)
            {
                if (imageUrls.Count >= storeItems.Count)
                {
                    for (int i = 0; i < storeItems.Count; i++)
                    {
                        var storeItem = storeItems[i];
                        var imageUrl = imageUrls[i];

                        SkinItem skinItem = new SkinItem();

                        var nameNode = storeItem.SelectSingleNode(".//h6[@class='mud-typography mud-typography-h6']");
                        var itemTypeNode = storeItem.SelectSingleNode(".//h6[@class='mud-typography mud-typography-subtitle1 mud-secondary-text']");
                        var priceNode = storeItem.SelectSingleNode(".//h6[@class='mud-typography mud-typography-h6 no-wrap']");

                        if (nameNode != null) skinItem.Name = nameNode.InnerText.Trim();
                        if (itemTypeNode != null) skinItem.Item = itemTypeNode.InnerText.Trim();
                        if (priceNode != null) skinItem.Price = priceNode.SelectSingleNode(".//span")?.InnerText.Trim();
                        skinItem.Image = imageUrl;

                        skinItems.Add(skinItem);
                    }
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

                LogMessage($"[UpdateChecker] {message} [{CurrentVersion}]");

                string changenumber_t = MainVersion + " --> " + CurrentVersion;

                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.WithTitle(message);
                embedBuilder.WithDescription(description);
                embedBuilder.WithColor(Discord.Color.Blue);
                embedBuilder.WithThumbnailUrl("https://yt3.googleusercontent.com/HPu-kTkwgN4mPxO6_PJThrtbPQEL_esHXjbPVp7bR5SF3H0HX_p6ub960hiH-D5WiDtPTosOXw=s176-c-k-c0x00ffffff-no-rj");
                embedBuilder.WithFooter(DateTime.Now.ToString(), "https://lh3.googleusercontent.com/a/ACg8ocJveuYqbU6KTFvsKpkmNLtB35Gd8-fsAbZzu3JVknZGDw=s288-c-no");
                embedBuilder.AddField("Sürüm Numarası Değişimi:", changenumber_t, true); ;

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
                                LogMessage($"[CommitTracker] Yeni Commit: {commit.id}");

                                var commitlink = "https://commits.facepunch.com/" + commit.id;

                                EmbedBuilder newEmbedBuilder = new EmbedBuilder();
                                newEmbedBuilder.WithAuthor(commit.user.name, commit.user.avatar);
                                newEmbedBuilder.WithTitle(commit.branch);
                                newEmbedBuilder.WithDescription(commit.message);
                                newEmbedBuilder.WithUrl(commitlink);
                                newEmbedBuilder.WithColor(Discord.Color.Blue);
                                newEmbedBuilder.WithFooter($"ID: {commit.id} | Change: {commit.changeset} | {DateTime.Now.ToString()}");

                                var channel = _client.GetChannel(_CommitKanalID) as IMessageChannel;
                                await channel.SendMessageAsync("", false, newEmbedBuilder.Build());
                            }
                        }
                        storedCommits.UnionWith(newCommits);
                    }
                }
                else
                {
                    LogMessage("[CommitTracker] Yeni veri alınamadı.");
                }

                LogMessage($"[CommitTracker] Depolanan: {storedCommits.Count}");
            }
            catch (Exception ex)
            {
                LogMessage($"[CommitTracker] Hata oluştu: {ex.Message}");
            }
        }

        private static Task ResponderUpdate()
        {
            int DayTimeHour = 18; //18 YAZ, 19 KIŞ.
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
            IUser user = message.Author;
            string userTag = $"{user.Mention}";

            if (message.Channel.Id == _SohbetKanalID)
            {
                if (message.Author.Id != _client.CurrentUser.Id)
                {
                    if (updateKeywords.Any(keyword => message.Content.ToLower().Contains(keyword)))
                    {
                        LogMessage("[Responder] Güncelleme sorusu cevaplanıyor...");
                        EmbedBuilder embedBuilder = new EmbedBuilder();
                        embedBuilder.WithTitle(":information_source:  **Güncelleme Bilgisi**  :information_source:");
                        embedBuilder.WithDescription("`Her ayın ilk perşembesi (Yaz Dönemi 21:00 - Kış Dönemi 22:00) gelen güncelleme ile tüm sunuculara` ***Zorunlu Harita Sıfırlaması*** `atılır.\nBP(Blueprint/Öğrenilen Eşyalar) Sıfırlaması ise sunucu sahibinin isteğine bağlıdır.`");
                        embedBuilder.WithThumbnailUrl("https://yt3.googleusercontent.com/HPu-kTkwgN4mPxO6_PJThrtbPQEL_esHXjbPVp7bR5SF3H0HX_p6ub960hiH-D5WiDtPTosOXw=s176-c-k-c0x00ffffff-no-rj");
                        embedBuilder.WithFooter(DateTime.Now.ToString(), "https://lh3.googleusercontent.com/a/ACg8ocJveuYqbU6KTFvsKpkmNLtB35Gd8-fsAbZzu3JVknZGDw=s288-c-no");
                        embedBuilder.AddField("Sonraki Güncelleme Tarihi:", $"<t:{_nextUpdateTimestamp}:F>", false);
                        embedBuilder.AddField("Sonraki Güncellemeye Kalan Zaman:", $"<t:{_nextUpdateTimestamp}:R>", false);
                        embedBuilder.AddField("Soran Kullanıcı", userTag, false);
                        embedBuilder.WithColor(Discord.Color.Blue);

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