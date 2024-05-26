using Discord;
using Discord.Webhook;
using Discord.WebSocket;
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
        private static DiscordSocketClient _client;
        private static readonly HttpClient httpClient = new HttpClient();

        private static DiscordWebhookClient webhookLogs = new DiscordWebhookClient("https://discord.com/api/webhooks/1243163905660817539/lBmr8EQHh1xvVu-32u1W_cjbdyneoB4Rd271orjMWrPiXY6BQTfpkSjCtfLpnJkYE_bE");

        private static string token = "MTExMTAxNjg2MDc0NjUzMDgzNg.G5Tmp_.pk-mcC5NNneSCwWOZuvFwOzovem4rieLdLKT3k";

        private static string commitApiUrl = "https://commits.facepunch.com/r/rust_reboot/?format=json";
        private static string skinApiUrl = "https://rust.scmm.app/store";

        private static HashSet<string> storedCommits = new HashSet<string>();
        private static HashSet<string> storedSkins = new HashSet<string>();

        private static Dictionary<ulong, List<ulong>> updateDateChannel_IDS = new Dictionary<ulong, List<ulong>>();
        private static Dictionary<ulong, List<ulong>> updateTrackerChannel_IDS = new Dictionary<ulong, List<ulong>>();
        private static Dictionary<ulong, List<ulong>> storeCheckerChannel_IDS = new Dictionary<ulong, List<ulong>>();
        private static Dictionary<ulong, List<ulong>> commitFollowerChannel_IDS = new Dictionary<ulong, List<ulong>>();

        private static string EnSonOyun = "";
        private static string EnSonSunucu = "";

        private static long _nextUpdateTimestamp = 0;
        private static bool _botReady = false;
        private static bool skinControlTimer = false;

        private static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            Console.Title = "RT_Control";

            LogMessage("Starting...");

            var config = new DiscordSocketConfig { GatewayIntents = GatewayIntents.All };

            _client = new DiscordSocketClient(config);

            _client.Log += Log;
            _client.Ready += BotReady;
            _client.JoinedGuild += OnJoinedGuild;
            _client.LeftGuild += OnLeaveGuild;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            while (!_botReady) { await Task.Delay(1000); }

            await _client.SetCustomStatusAsync($"Rust Güncelleme Notları");

            LogMessage("Initialize update checker...");
            await Task.Run(Initialize_UpdateChecker);

            LogMessage("Initialize channels...");
            await Task.Run(Initialize_Channels);

            LogMessage("Starting tasks...");
            _ = Task.Run(ResponderandChannels);
            _ = Task.Run(CommitandSkin);
            _ = Task.Run(UpdateChecker);

            LogMessage("All done!");

            await Task.Delay(-1);
        }

        private static async Task ResponderandChannels()
        {
            while (true)
            {
                try
                {
                    await ResponderUpdate();
                    await SendUpdateDateMessage();
                    await Initialize_Channels();
                    await AppLogs();
                }
                catch (Exception ex)
                {
                    LogMessage($"Error - ResponderandChannels: {ex}");
                    await webhookLogs.SendMessageAsync($"Error - ResponderandChannels: {ex}");
                }
                await Task.Delay(TimeSpan.FromHours(1));
            }
        }

        private static async Task CommitandSkin()
        {
            while (true)
            {
                try
                {
                    await CheckForNewCommits();
                    await CheckForNewSkins();
                }
                catch (Exception ex)
                {
                    LogMessage($"Error - CommitandSkin: {ex}");
                    await webhookLogs.SendMessageAsync($"Error - CommitandSkin: {ex}");
                }
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
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

        private static async Task AppLogs()
        {
            try
            {
                var botUser = _client.CurrentUser;
                await webhookLogs.SendMessageAsync($"Server Count: {botUser.MutualGuilds.Count}");
                foreach (var guild in botUser.MutualGuilds) await webhookLogs.SendMessageAsync($"Guild Name: {guild.Name} - Guild ID: {guild.Id}");
            }
            catch (Exception ex)
            {
                LogMessage($"Error - AppLogs: {ex}");
                await webhookLogs.SendMessageAsync($"Error - AppLogs: {ex}");
            }
        }

        private static async Task Initialize_Channels()
        {
            foreach (var CurrentGuild in _client.Guilds)
            {
                string categoryName = "rust-güncelleme┃🔔";
                string[] channelNames = { "güncelleme-tarihi┃📅", "güncelleme-takipçisi┃💻", "haftalık-mağaza┃🛒", "commits┃📝" };
                try
                {
                    if (!CheckBotPerms(CurrentGuild)) await NoPermsSendMessage(CurrentGuild);

                    var categoryCheck = CurrentGuild.CategoryChannels.FirstOrDefault(c => c.Name == categoryName);
                    ICategoryChannel categoryCurrent;

                    if (categoryCheck != null) { LogMessage($"Category already created. | Guild: {CurrentGuild} | Category: {categoryCheck.Id}"); categoryCurrent = categoryCheck; }
                    else { categoryCurrent = await CurrentGuild.CreateCategoryChannelAsync(categoryName); LogMessage($"Category created. | Guild: {CurrentGuild} - Category: {categoryCurrent.Id}"); }

                    List<ulong> updateDateChannel_Local_IDS = new List<ulong>();
                    List<ulong> updateTrackerChannel_Local_IDS = new List<ulong>();
                    List<ulong> storeCheckerChannel_Local_IDS = new List<ulong>();
                    List<ulong> commitFollowerChannel_Local_IDS = new List<ulong>();

                    for (int i = 0; i < channelNames.Length; i++)
                    {
                        string channelName = channelNames[i];
                        var channelCheck = CurrentGuild.TextChannels.FirstOrDefault(c => c.Name == channelName && c.CategoryId == categoryCurrent.Id);
                        if (channelCheck != null)
                        {
                            LogMessage($"Channel already created: {channelCheck.Id}");
                            switch (i)
                            {
                                case 0:
                                    updateDateChannel_Local_IDS.Add(channelCheck.Id);
                                    break;

                                case 1:
                                    updateTrackerChannel_Local_IDS.Add(channelCheck.Id);
                                    break;

                                case 2:
                                    storeCheckerChannel_Local_IDS.Add(channelCheck.Id);
                                    break;

                                case 3:
                                    commitFollowerChannel_Local_IDS.Add(channelCheck.Id);
                                    break;
                            }
                        }
                        else
                        {
                            var newChannel = await CurrentGuild.CreateTextChannelAsync(channelName, x => x.CategoryId = categoryCurrent.Id);
                            LogMessage($"New Channel created: {newChannel.Id}");
                            switch (i)
                            {
                                case 0:
                                    updateDateChannel_Local_IDS.Add(newChannel.Id);
                                    await newChannel.SendMessageAsync("**Güncelleme Tarihi** kanalı başarıyla oluşturuldu.\n\nGüncelleme bilgisi saatlik olarak güncellenmektir. 1 Saat içinde güncelleme bilgisi bu kanala eklenecektir.");
                                    break;

                                case 1:
                                    updateTrackerChannel_Local_IDS.Add(newChannel.Id);
                                    await newChannel.SendMessageAsync("**Güncelleme Takipçisi** kanalı başarıyla oluşturuldu.\n\nSunucu veya Oyuncu taraflı bir güncelleme tespit edildiğinde bu kanalda bildirim gelecektir.");
                                    break;

                                case 2:
                                    storeCheckerChannel_Local_IDS.Add(newChannel.Id);
                                    await newChannel.SendMessageAsync("**Haftalık Mağaza** kanalı başarıyla oluşturuldu.\n\nHer hafta mağaza yenilediğinde gelen skinlerin görsellerini ve fiyatlarını bu kanalda görebilirsiniz.");
                                    break;

                                case 3:
                                    commitFollowerChannel_Local_IDS.Add(newChannel.Id);
                                    await newChannel.SendMessageAsync("**Commits** kanalı başarıyla oluşturuldu.\n\nYeni bir commit tespit edildiğinde bu kanalda görebilirsiniz.");
                                    break;
                            }
                        }
                    }
                    updateDateChannel_IDS[CurrentGuild.Id] = updateDateChannel_Local_IDS;
                    updateTrackerChannel_IDS[CurrentGuild.Id] = updateTrackerChannel_Local_IDS;
                    storeCheckerChannel_IDS[CurrentGuild.Id] = storeCheckerChannel_Local_IDS;
                    commitFollowerChannel_IDS[CurrentGuild.Id] = commitFollowerChannel_Local_IDS;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error - Initialize_Channels: {ex}");
                    await webhookLogs.SendMessageAsync($"Error - Initialize_Channels: {ex}");
                }
            }
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

                foreach (var item in skinData) LogMessage($"[SkinTracker] Name: {item.Name} | Price: {item.Price} | Type: {item.Item} | Image: {item.Image}");

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
                        if (ourimage == null) return;

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

                        foreach (var guildId in storeCheckerChannel_IDS.Keys)
                        {
                            var guild = _client.GetGuild(guildId);
                            if (guild == null) continue;

                            foreach (var channelId in storeCheckerChannel_IDS[guildId])
                            {
                                var channel = guild.GetTextChannel(channelId);
                                if (channel != null)
                                {
                                    try
                                    {
                                        if (!CheckBotPerms(guild)) await NoPermsSendMessage(guild);
                                        if (!CheckChannelPerms(channel)) await NoPermsSendMessage(guild);
                                        else await channel.SendMessageAsync("@everyone", false, embedBuildformain.Build());
                                    }
                                    catch (Exception ex)
                                    {
                                        LogMessage($"Error - CheckForNewSkins: {ex}");
                                        await webhookLogs.SendMessageAsync($"Error - CheckForNewSkins: {ex}");
                                    }
                                }
                            }
                        }

                        using (var fileStream = new FileStream("skinimage.png", FileMode.Open))
                        {
                            byte[] imageData;
                            using (var memoryStream = new MemoryStream())
                            {
                                fileStream.CopyTo(memoryStream);
                                imageData = memoryStream.ToArray();
                            }

                            foreach (var guildId in storeCheckerChannel_IDS.Keys)
                            {
                                var guild = _client.GetGuild(guildId);
                                if (guild == null) continue;

                                foreach (var channelId in storeCheckerChannel_IDS[guildId])
                                {
                                    var channel = guild.GetTextChannel(channelId);
                                    if (channel != null)
                                    {
                                        using (var memoryStream = new MemoryStream(imageData))
                                        {
                                            try
                                            {
                                                if (!CheckBotPerms(guild)) await NoPermsSendMessage(guild);
                                                if (!CheckChannelPerms(channel)) await NoPermsSendMessage(guild);
                                                else await channel.SendFileAsync(memoryStream, "skinimage.png");
                                            }
                                            catch (Exception ex)
                                            {
                                                LogMessage($"Error - CheckForNewSkins: {ex}");
                                                await webhookLogs.SendMessageAsync($"Error - CheckForNewSkins: {ex}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        File.Delete("skinimage.png");
                        storedSkins.Clear();
                        storedSkins.UnionWith(newSkins);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                LogMessage("[SkinTracker] İstek zaman aşımına uğradı.");
                await webhookLogs.SendMessageAsync($"Error - CheckForNewSkins_TimeOut: {ex}");
            }
            catch (HttpRequestException ex)
            {
                LogMessage("[SkinTracker] HTTP request failed: " + ex.Message);
                await webhookLogs.SendMessageAsync($"Error - CheckForNewSkins_RequestFailed: {ex}");
            }
            catch (Exception ex)
            {
                LogMessage("[SkinTracker] An error occurred: " + ex.Message);
                await webhookLogs.SendMessageAsync($"Error - CheckForNewSkins_Generally: {ex}");
            }
        }

        private static System.Drawing.Image CreateBigImage(List<SkinItem> skindata)
        {
            var imageUrls = skindata.Select(Skin => Skin.Image).ToList();
            var skinNames = skindata.Select(Skin => Skin.Name).ToList();
            var skinPrices = skindata.Select(Skin => Skin.Price).ToList();
            var skinType = skindata.Select(Skin => Skin.Item).ToList();
            int imageSize = (int)Math.Ceiling(Math.Sqrt(imageUrls.Count));
            int squareSize = imageSize * 400;
            Bitmap combinedImage = new Bitmap(squareSize, squareSize);
            Bitmap backgroundImage;
            try
            {
                using (WebClient client = new WebClient())
                {
                    byte[] imageData = client.DownloadData("https://cdn.discordapp.com/attachments/1243011831891623936/1243016965715525733/back.png?ex=664ff142&is=664e9fc2&hm=a723c7433f7a340fcc3f4e3794fb2540229d562cf4b5eb3cf076f8c6b16e242a&");
                    using (MemoryStream stream = new MemoryStream(imageData)) backgroundImage = new Bitmap(stream);
                }
            }
            catch (Exception ex)
            {
                LogMessage("[SkinTracker] An error occurred: " + ex.Message);
                return null;
            }

            using (Graphics g = Graphics.FromImage(combinedImage))
            {
                g.DrawImage(backgroundImage, 0, 0, combinedImage.Width, combinedImage.Height);

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

                                g.DrawImage(img, x * 400, y * 400, 350, 350);

                                var skinname = skinNames[index];
                                var skintype = skinType[index];
                                var skinprice = skinPrices[index];

                                if (index < imageUrls.Count)
                                {
                                    Font font = new Font("Arial", 18, FontStyle.Bold);
                                    Font font2 = new Font("Arial", 12);
                                    SizeF textSize = g.MeasureString(skinname, font);
                                    if (textSize.Width > 400 || textSize.Height > 400)
                                    {
                                        float scale = Math.Min(400 / textSize.Width, 400 / textSize.Height);
                                        font = new Font("Arial", 18 * scale, FontStyle.Bold);
                                    }
                                    float textX = x * 400 + (400 - textSize.Width) / 2;
                                    float textY = (y + 1) * 400 - textSize.Height;
                                    g.DrawString(skinname, font, Brushes.White, new PointF(textX, textY - 30));
                                    g.DrawString(skinprice, font, Brushes.ForestGreen, new PointF(textX, textY));
                                    g.DrawString(skintype, font2, Brushes.DarkGray, new PointF(textX + 75, textY + 5));
                                }

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
                        return null;
                    }

                    index++; x++;
                    if (x >= imageSize) { x = 0; y++; }
                    if (index >= imageSize * imageSize) break;
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
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
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
                LogMessage($"Error - GetRustVersionAsync: {ex.Message}");
                await webhookLogs.SendMessageAsync($"Error - GetRustVersionAsync: {ex}");
                return null;
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
                embedBuilder.AddField("Sürüm Numarası Değişimi:", changenumber_t, true); ;

                foreach (var guildId in updateTrackerChannel_IDS.Keys)
                {
                    var guild = _client.GetGuild(guildId);
                    if (guild == null) continue;

                    foreach (var channelId in updateTrackerChannel_IDS[guildId])
                    {
                        var channel = guild.GetTextChannel(channelId);
                        if (channel != null)
                        {
                            try
                            {
                                if (!CheckBotPerms(guild)) await NoPermsSendMessage(guild);
                                if (!CheckChannelPerms(channel)) await NoPermsSendMessage(guild);
                                else await channel.SendMessageAsync("@everyone", false, embedBuilder.Build());
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"Error - CheckAndUpdateVersion: {ex}");
                                await webhookLogs.SendMessageAsync($"Error - CheckAndUpdateVersion: {ex}");
                            }
                        }
                    }
                }
            }
        }



        private static async Task CheckForNewCommits()
        {
            try
            {
                var response = await httpClient.GetStringAsync(commitApiUrl);
                var commitData = JsonConvert.DeserializeObject<CommitData>(response);
                if (commitData?.Results == null) { LogMessage("[CommitTracker] Yeni veri alınamadı."); return; }

                var newCommits = commitData.Results.Select(commit => commit.Message).ToList();
                if (!newCommits.Any()) { LogMessage("[CommitTracker] Yeni commit alınamadı."); return; }

                if (storedCommits.Count == 0)
                {
                    storedCommits.UnionWith(newCommits);
                }
                else
                {
                    var differences = commitData.Results.Where(commit => newCommits.Contains(commit.Message) && !storedCommits.Contains(commit.Message)).ToList();
                    if (differences.Any())
                    {
                        foreach (var commit in differences)
                        {
                            LogMessage($"[CommitTracker] Yeni Commit: {commit.Id}");
                            var commitlink = "https://commits.facepunch.com/" + commit.Id;
                            EmbedBuilder newEmbedBuilder = new EmbedBuilder();
                            newEmbedBuilder.WithAuthor(commit.UserInfo.Name, commit.UserInfo.Avatar);
                            newEmbedBuilder.WithTitle(commit.Branch);
                            newEmbedBuilder.WithDescription(commit.Message);
                            newEmbedBuilder.WithUrl(commitlink);
                            newEmbedBuilder.WithColor(Discord.Color.Blue);
                            newEmbedBuilder.WithFooter($"ID: {commit.Id} | Change: {commit.ChangeSet} | {DateTime.Now.ToString()}");
                            foreach (var guildId in commitFollowerChannel_IDS.Keys)
                            {
                                var guild = _client.GetGuild(guildId);
                                if (guild == null) continue;

                                foreach (var channelId in commitFollowerChannel_IDS[guildId])
                                {
                                    var channel = guild.GetTextChannel(channelId);
                                    if (channel != null)
                                    {
                                        try
                                        {
                                            if (!CheckBotPerms(guild)) await NoPermsSendMessage(guild);
                                            if (!CheckChannelPerms(channel)) await NoPermsSendMessage(guild);
                                            else await channel.SendMessageAsync("", false, newEmbedBuilder.Build());
                                        }
                                        catch (Exception ex)
                                        {
                                            LogMessage($"Error - CheckForNewCommits: {ex}");
                                            await webhookLogs.SendMessageAsync($"Error - CheckForNewCommits: {ex}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    storedCommits.UnionWith(newCommits);
                }
                LogMessage($"[CommitTracker] Depolanan: {storedCommits.Count}");
            }
            catch (Exception ex)
            {
                LogMessage($"Error - CheckForNewCommits: {ex}");
                await webhookLogs.SendMessageAsync($"Error - CheckForNewCommits: {ex}");
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

        private static async Task SendUpdateDateMessage()
        {
            LogMessage("[Responder] Güncelleme sorusu cevaplanıyor...");
            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle(":information_source:  **Güncelleme Bilgisi**  :information_source:");
            embedBuilder.WithDescription("Her ayın ilk perşembesi (Yaz Dönemi 21:00 - Kış Dönemi 22:00) gelen güncelleme ile tüm sunuculara **Zorunlu Harita Sıfırlaması** atılır.\n**BP Sıfırlaması**(Blueprint/Öğrenilen Eşyalar) ise sunucu sahibinin isteğine bağlıdır.");
            embedBuilder.WithThumbnailUrl("https://yt3.googleusercontent.com/HPu-kTkwgN4mPxO6_PJThrtbPQEL_esHXjbPVp7bR5SF3H0HX_p6ub960hiH-D5WiDtPTosOXw=s176-c-k-c0x00ffffff-no-rj");
            embedBuilder.AddField("Sonraki Güncelleme Tarihi:", $"<t:{_nextUpdateTimestamp}:F>", false);
            embedBuilder.AddField("Sonraki Güncellemeye Kalan Zaman:", $"<t:{_nextUpdateTimestamp}:R>", false);
            embedBuilder.WithColor(Discord.Color.Blue);
            embedBuilder.WithFooter($"En Son Tarih Kontrolü: {DateTime.Now.ToString()}");

            foreach (var guildId in updateDateChannel_IDS.Keys)
            {
                var guild = _client.GetGuild(guildId);
                if (guild == null) continue;
                foreach (var channelId in updateDateChannel_IDS[guildId])
                {
                    var channel = guild.GetTextChannel(channelId);
                    if (channel != null)
                    {
                        try
                        {
                            if (!CheckBotPerms(guild)) await NoPermsSendMessage(guild);
                            if (!CheckChannelPerms(channel)) await NoPermsSendMessage(guild);
                            else
                            {
                                var messages = await channel.GetMessagesAsync(limit: 1).FlattenAsync();
                                var lastMessage = messages.FirstOrDefault() as IUserMessage;
                                if (lastMessage != null && lastMessage.Author.Id == _client.CurrentUser.Id) await lastMessage.ModifyAsync(msg => msg.Embed = embedBuilder.Build());
                                else await channel.SendMessageAsync("", false, embedBuilder.Build());
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Error - SendUpdateDateMessage: {ex}");
                            await webhookLogs.SendMessageAsync($"Error - SendUpdateDateMessage: {ex}");
                        }
                    }
                }
            }
        }

        private async Task OnJoinedGuild(SocketGuild guild)
        {
            try
            {
                LogMessage($"New guild: {guild}");
                await webhookLogs.SendMessageAsync($"New guild: {guild}");
                await Initialize_Channels();
            }
            catch (Exception ex)
            {
                LogMessage($"Error - OnJoinedGuild: {ex}");
                await webhookLogs.SendMessageAsync($"Error - OnJoinedGuild: {ex}");
            }
        }

        private async Task OnLeaveGuild(SocketGuild guild)
        {
            try
            {
                LogMessage($"Guild leaved: {guild}");
                await webhookLogs.SendMessageAsync($"Guild leaved: {guild}");
                await Initialize_Channels();
            }
            catch (Exception ex)
            {
                LogMessage($"Error - OnLeaveGuild: {ex}");
                await webhookLogs.SendMessageAsync($"Error - OnLeaveGuild: {ex}");
            }
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

        private static bool IsValidVersion(string version)
        {
            return version != null && version.Length == 8 && int.TryParse(version, out _);
        }

        private static async Task NoPermsSendMessage(SocketGuild CurrentGuild)
        {
            try
            {
                LogMessage($"Yetersiz yetki. | Guild: {CurrentGuild.Name} - Id: {CurrentGuild.Id}");
                await webhookLogs.SendMessageAsync($"Yetersiz yetki. | Guild: {CurrentGuild.Name} - Id: {CurrentGuild.Id}");
                var user = _client.GetUserAsync(CurrentGuild.OwnerId).Result;
                await UserExtensions.SendMessageAsync(user, $"Yetersiz yetki. | Guild: {CurrentGuild}\nBot gerekli izinlere sahip değil.");
            }
            catch (Exception ex)
            {
                LogMessage($"Error - Sending Direct Message: {ex}");
                await webhookLogs.SendMessageAsync($"Error - Sending Direct Message: {ex}");
            }
        }

        private static bool CheckBotPerms(SocketGuild guild)
        {
            var requiredPermissions = new[] {
                GuildPermission.AttachFiles, GuildPermission.ChangeNickname, GuildPermission.EmbedLinks, GuildPermission.ManageChannels,
                GuildPermission.ManageMessages, GuildPermission.ReadMessageHistory,GuildPermission.ViewChannel,GuildPermission.SendMessages};
            var botUser = guild.CurrentUser;
            return requiredPermissions.All(permission => botUser.GuildPermissions.Has(permission));
        }

        private static bool CheckChannelPerms(SocketTextChannel channel)
        {
            var requiredPermissions = new[] {
                ChannelPermission.AttachFiles, ChannelPermission.EmbedLinks, ChannelPermission.ManageChannels, ChannelPermission.ManageMessages,
                ChannelPermission.ReadMessageHistory, ChannelPermission.ViewChannel,ChannelPermission.SendMessages };
            var permissions = channel.Guild.CurrentUser.GetPermissions(channel);
            return requiredPermissions.All(permission => permissions.Has(permission));
        }

        private static bool HasPermission(SocketGuild guild, GuildPermission permission)
        {
            var currentUser = guild.GetUser(_client.CurrentUser.Id);
            return currentUser.GuildPermissions.Has(permission);
        }

        public class CommitData
        {
            public List<Commit> Results { get; set; }
        }

        public class Commit
        {
            public int Id { get; set; }
            public string Repository { get; set; }
            public string Branch { get; set; }
            public string ChangeSet { get; set; }
            public DateTime CreateDate { get; set; }
            public string Message { get; set; }
            public User UserInfo { get; set; }
        }

        public class User
        {
            public string Name { get; set; }
            public string Avatar { get; set; }
        }

        public class SkinItem
        {
            public string Name { get; set; }
            public string Item { get; set; }
            public string Price { get; set; }
            public string Image { get; set; }
        }
    }
}