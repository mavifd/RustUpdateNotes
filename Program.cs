using Discord;
using Discord.Webhook;
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
        private static DiscordSocketClient _client;
        private static readonly HttpClient httpClient = new HttpClient();

        private static DiscordWebhookClient webhookLogs = new DiscordWebhookClient("https://discord.com/api/webhooks/1243163905660817539/lBmr8EQHh1xvVu-32u1W_cjbdyneoB4Rd271orjMWrPiXY6BQTfpkSjCtfLpnJkYE_bE");

        private static string token = "MTExMTAxNjg2MDc0NjUzMDgzNg.G5Tmp_.pk-mcC5NNneSCwWOZuvFwOzovem4rieLdLKT3k";

        private static string commitApiUrl = "https://commits.facepunch.com/r/rust_reboot/?format=json";
        private static string skinApiUrl = "https://store.steampowered.com/itemstore/252490/browse/?filter=Limited";

        private static HashSet<string> storedCommits = new HashSet<string>();
        private static HashSet<string> storedSkins = new HashSet<string>();

        private static Dictionary<ulong, List<ulong>> updateNotesChannel_IDS = new Dictionary<ulong, List<ulong>>();
        private static Dictionary<ulong, List<ulong>> updateDateChannel_IDS = new Dictionary<ulong, List<ulong>>();
        private static Dictionary<ulong, List<ulong>> updateTrackerChannel_IDS = new Dictionary<ulong, List<ulong>>();
        private static Dictionary<ulong, List<ulong>> storeCheckerChannel_IDS = new Dictionary<ulong, List<ulong>>();
        private static Dictionary<ulong, List<ulong>> commitFollowerChannel_IDS = new Dictionary<ulong, List<ulong>>();

        private static string Main_Public = "";

        private static string Server_Public = "";
        private static string Server_Staging = "";
        private static string Server_Aux02 = "";

        private static string Staging_Public = "";
        private static string Staging_Main = "";
        private static string Staging_Aux02 = "";

        private static long _nextUpdateTimestamp = 0;
        private static bool _botReady = false;

        private static ulong _SohbetKanalID = 1223037877911556110;
        private static ulong _PingMavi = 170569747497222145;
        private static ulong _MainDiscord = 1223037877911556107;

        private static List<string> UpdateKeys = new List<string> { "wipe", "güncelleme", "global", "update" };

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
            _client.MessageReceived += MessageReceived;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            while (!_botReady) { await Task.Delay(1000); }

            await _client.SetCustomStatusAsync($"🔔 Rust Güncelleme Notları");

            LogMessage("Starting tasks...");

            _ = Task.Run(InitChannel_Runner);
            _ = Task.Run(AppLogs_Runner);
            _ = Task.Run(UpdateMessage_Runner);
            _ = Task.Run(Responder_Runner);
            _ = Task.Run(Commit_Runner);
            _ = Task.Run(Skin_Runner);
            _ = Task.Run(Update_Runner);

            LogMessage("All done!");


            await Task.Delay(Timeout.Infinite);
        }

        private static async Task InitChannel_Runner()
        {
            while (true)
            {
                try
                {
                    await Initialize_Channels();
                }
                catch (Exception ex)
                {
                    LogMessage($"Error - InitChannel_Runner: {ex}");
                    await webhookLogs.SendMessageAsync($"<@{_PingMavi}> | Error - InitChannel_Runner: {ex}");
                }
                await Task.Delay(TimeSpan.FromHours(1));
            }
        }

        private static async Task AppLogs_Runner()
        {
            while (true)
            {
                try
                {
                    var botUser = _client.CurrentUser;
                    await webhookLogs.SendMessageAsync($"**{DateTime.Now}** - Server Count: {botUser.MutualGuilds.Count}");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error - AppLogs_Runner: {ex}");
                    await webhookLogs.SendMessageAsync($"<@{_PingMavi}> | Error - AppLogs_Runner: {ex}");
                }
                await Task.Delay(TimeSpan.FromHours(1));
            }
        }

        private static async Task UpdateMessage_Runner()
        {
            while (true)
            {
                try
                {
                    await SendUpdateDateMessage();
                }
                catch (Exception ex)
                {
                    LogMessage($"Error - UpdateMessage_Runner: {ex}");
                    await webhookLogs.SendMessageAsync($"<@{_PingMavi}> | Error - UpdateMessage_Runner: {ex}");
                }
                await Task.Delay(TimeSpan.FromHours(1));
            }
        }

        private static async Task Responder_Runner()
        {
            while (true)
            {
                try
                {
                    await ResponderUpdate();
                }
                catch (Exception ex)
                {
                    LogMessage($"Error - Responder_Runner: {ex}");
                    await webhookLogs.SendMessageAsync($"<@{_PingMavi}> | Error - Responder_Runner: {ex}");
                }
                await Task.Delay(TimeSpan.FromHours(1));
            }
        }

        private static async Task Commit_Runner()
        {
            while (true)
            {
                try
                {
                    await CheckForNewCommits();
                }
                catch (TaskCanceledException ex)
                {
                    LogMessage($"Error - Commit_Runner: Task Timeout. {ex}");
                    await webhookLogs.SendMessageAsync($"<@{_PingMavi}> | Error - Commit_Runner: {ex}");
                }
                catch (HttpRequestException ex)
                {
                    LogMessage($"Error - Commit_Runner: Webrequest failed. {ex}");
                    await webhookLogs.SendMessageAsync($"<@{_PingMavi}> | Error - Commit_Runner: {ex}");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error - Commit_Runner: {ex}");
                    await webhookLogs.SendMessageAsync($"<@{_PingMavi}> | Error - Commit_Runner: {ex}");
                }
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private static async Task Skin_Runner()
        {
            while (true)
            {
                try
                {
                    await CheckForNewSkins();
                }
                catch (TaskCanceledException ex)
                {
                    LogMessage($"Error - Skin_Runner: Task Timeout. {ex}");
                    await webhookLogs.SendMessageAsync($"Error - Skin_Runner: Task Timeout. {ex}");
                }
                catch (WebException ex)
                {
                    LogMessage($"Error - Skin_Runner: Webrequest error. {ex}");
                    await webhookLogs.SendMessageAsync($"<@{_PingMavi}> | Error - Skin_Runner: Webrequest error. {ex}");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error - Skin_Runner: {ex}");
                    await webhookLogs.SendMessageAsync($"<@{_PingMavi}> | Error - Skin_Runner: {ex}");
                }
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private static async Task Update_Runner()
        {
            await Initialize_UpdateChecker();
            while (true)
            {
                try
                {
                    string Local_Main_Public = "1";
                    string Local_Main_NULL1 = "1";
                    string Local_Main_NULL2 = "1";

                    string Local_Server_Public = "1";
                    string Local_Server_Staging = "1";
                    string Local_Server_Aux02 = "1";

                    string Local_Staging_Public = "1";
                    string Local_Staging_Main = "1";
                    string Local_Staging_Aux02 = "1";

                    GetRustVersionAsync("rustapp.txt", ref Local_Main_Public, ref Local_Main_NULL1, ref Local_Main_NULL2);
                    LogMessage($"[UpdateChecker] Local_Main_Public: {Local_Main_Public}");

                    GetRustVersionAsync("rustserver.txt", ref Local_Server_Public, ref Local_Server_Staging, ref Local_Server_Aux02);
                    LogMessage($"[UpdateChecker] Local_Server_Public: {Local_Server_Public}");
                    LogMessage($"[UpdateChecker] Local_Server_Staging: {Local_Server_Staging}");
                    LogMessage($"[UpdateChecker] Local_Server_Aux02: {Local_Server_Aux02}");

                    GetRustVersionAsync("ruststaging.txt", ref Local_Staging_Public, ref Local_Staging_Main, ref Local_Staging_Aux02);
                    LogMessage($"[UpdateChecker] Local_Staging_Public: {Local_Staging_Public}");
                    LogMessage($"[UpdateChecker] Local_Staging_Main: {Local_Staging_Main}");
                    LogMessage($"[UpdateChecker] Local_Staging_Aux02: {Local_Staging_Aux02}");

                    await CheckAndUpdateVersion(Local_Main_Public, Local_Server_Public, Local_Server_Staging, Local_Server_Aux02, Local_Staging_Public, Local_Staging_Main, Local_Staging_Aux02);
                }
                catch (Exception ex)
                {
                    LogMessage($"Error - Update_Runner: {ex}");
                    await webhookLogs.SendMessageAsync($"<@{_PingMavi}> | Error - Update_Runner: {ex}");
                }
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }


        private static async Task Initialize_Channels()
        {
            if (_client.ConnectionState != ConnectionState.Connected)
            {
                await Task.Delay(1000);
            }

            var guildlist = _client.Guilds;
            foreach (var CurrentGuild in guildlist)
            {
                string categoryName = "rust-güncelleme┃🔔";
                string[] channelNames = { "güncelleme-notları┃📋", "güncelleme-tarihi┃📅", "güncelleme-takipçisi┃💻", "haftalık-mağaza┃🛒", "commits┃📝" };

                if (!await CheckBotPerms(CurrentGuild)) { await NoPermsSendMessage(CurrentGuild, "(Genel Yetki Yetersizliği)"); continue; }

                var categoryCheck = CurrentGuild.CategoryChannels.FirstOrDefault(c => c.Name == categoryName);
                ICategoryChannel categoryCurrent;

                if (categoryCheck != null) { LogMessage($"Category already created. | Guild: {CurrentGuild} | Category: {categoryCheck.Id}"); categoryCurrent = categoryCheck; }
                else { categoryCurrent = await CurrentGuild.CreateCategoryChannelAsync(categoryName); LogMessage($"Category created. | Guild: {CurrentGuild} - Category: {categoryCurrent.Id}"); }

                List<ulong> updateNotesChannel_Local_IDS = new List<ulong>();
                List<ulong> updateDateChannel_Local_IDS = new List<ulong>();
                List<ulong> updateTrackerChannel_Local_IDS = new List<ulong>();
                List<ulong> storeCheckerChannel_Local_IDS = new List<ulong>();
                List<ulong> commitFollowerChannel_Local_IDS = new List<ulong>();

                for (int i = 0; i < channelNames.Length; i++)
                {
                    string channelName = channelNames[i];

                    if (CurrentGuild.Id == _MainDiscord && channelName == "güncelleme-notları┃📋") continue;

                    var channelCheck = CurrentGuild.TextChannels.FirstOrDefault(c => c.Name == channelName && c.CategoryId == categoryCurrent.Id);
                    if (channelCheck != null)
                    {
                        LogMessage($"Channel already created: {channelCheck.Id}");

                        switch (i)
                        {
                            case 0:
                                updateNotesChannel_Local_IDS.Add(channelCheck.Id);
                                break;

                            case 1:
                                updateDateChannel_Local_IDS.Add(channelCheck.Id);
                                break;

                            case 2:
                                updateTrackerChannel_Local_IDS.Add(channelCheck.Id);
                                break;

                            case 3:
                                storeCheckerChannel_Local_IDS.Add(channelCheck.Id);
                                break;

                            case 4:
                                commitFollowerChannel_Local_IDS.Add(channelCheck.Id);
                                break;
                        }
                    }
                    else
                    {
                        var newChannel = await CurrentGuild.CreateTextChannelAsync(channelName, x => x.CategoryId = categoryCurrent.Id);
                        LogMessage($"New Channel created: {newChannel.Id}");

                        LogMessage($"Kanal yetkileri ayarlanıyor. - {newChannel.Name} | {CurrentGuild.Name}");
                        await webhookLogs.SendMessageAsync($"Kanal yetkileri ayarlanıyor. - {newChannel.Name} | {CurrentGuild.Name}");

                        try
                        {
                            var botUser = _client.CurrentUser;
                            var overwritePermissions = new OverwritePermissions(
                            attachFiles: PermValue.Allow, embedLinks: PermValue.Allow, manageChannel: PermValue.Allow, manageRoles: PermValue.Allow, manageWebhooks: PermValue.Allow,
                            mentionEveryone: PermValue.Allow, readMessageHistory: PermValue.Allow, sendMessages: PermValue.Allow, viewChannel: PermValue.Allow, useApplicationCommands: PermValue.Allow);
                            await newChannel.AddPermissionOverwriteAsync(botUser, overwritePermissions);
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Kanal yetkisi ayarlanamadı.  - {newChannel.Name} | {CurrentGuild.Name} | {ex}");
                            await webhookLogs.SendMessageAsync($"Kanal yetkisi ayarlanamadı.  - {newChannel.Name} | {CurrentGuild.Name} | {ex}");
                        }

                        switch (i)
                        {
                            case 0:
                                updateNotesChannel_Local_IDS.Add(newChannel.Id);
                                try
                                {
                                    await newChannel.SendMessageAsync("**Güncelleme Notları** kanalı başarıyla oluşturuldu.\nGüncelleme notları bu kanalda paylaşılacaktır.");
                                    var announcementChannel = _client.GetChannel(1223058873573969920) as SocketNewsChannel;
                                    var targetChannel = _client.GetChannel(newChannel.Id) as SocketTextChannel;
                                    await Task.Delay(1000);
                                    var followChannel = await announcementChannel.FollowAnnouncementChannelAsync(targetChannel);
                                }
                                catch (Exception)
                                {
                                    LogMessage($"Güncelleme notları takip edilemedi. | {CurrentGuild.Name}");
                                    await webhookLogs.SendMessageAsync($"Güncelleme notları takip edilemedi. - {newChannel.Name} | {CurrentGuild.Name}");
                                }
                                break;

                            case 1:
                                updateDateChannel_Local_IDS.Add(newChannel.Id);
                                await newChannel.SendMessageAsync("**Güncelleme Tarihi** kanalı başarıyla oluşturuldu.\nGüncelleme bilgisi saatlik olarak güncellenmektir. Güncelleme bilgisi 1 saat içinde bu kanala eklenecektir.");
                                break;

                            case 2:
                                updateTrackerChannel_Local_IDS.Add(newChannel.Id);
                                await newChannel.SendMessageAsync("**Güncelleme Takipçisi** kanalı başarıyla oluşturuldu.\nSunucu veya Oyuncu taraflı bir güncelleme tespit edildiğinde bu kanalda bildirim gelecektir.");
                                break;

                            case 3:
                                storeCheckerChannel_Local_IDS.Add(newChannel.Id);
                                await newChannel.SendMessageAsync("**Haftalık Mağaza** kanalı başarıyla oluşturuldu.\nHer hafta mağaza yenilediğinde gelen skinlerin görsellerini ve fiyatlarını bu kanalda görebilirsiniz.");
                                break;

                            case 4:
                                commitFollowerChannel_Local_IDS.Add(newChannel.Id);
                                await newChannel.SendMessageAsync("**Commits** kanalı başarıyla oluşturuldu.\nYeni bir commit tespit edildiğinde bu kanalda görebilirsiniz.");
                                break;
                        }
                    }
                }

                updateNotesChannel_IDS[CurrentGuild.Id] = updateNotesChannel_Local_IDS;
                updateDateChannel_IDS[CurrentGuild.Id] = updateDateChannel_Local_IDS;
                updateTrackerChannel_IDS[CurrentGuild.Id] = updateTrackerChannel_Local_IDS;
                storeCheckerChannel_IDS[CurrentGuild.Id] = storeCheckerChannel_Local_IDS;
                commitFollowerChannel_IDS[CurrentGuild.Id] = commitFollowerChannel_Local_IDS;
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
                File.WriteAllText("scripts\\ruststaging.txt", Properties.Resources.ruststaging);
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
                    File.WriteAllText("scripts\\ruststaging.txt", Properties.Resources.ruststaging);
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
                GetRustVersionAsync_Starting("rustapp.txt");
                LogMessage($"[UpdateChecker] Main_Public: {Main_Public}");

                GetRustVersionAsync_Starting("rustserver.txt");
                LogMessage($"[UpdateChecker] Server_Public: {Server_Public}");
                LogMessage($"[UpdateChecker] Server_Staging: {Server_Staging}");
                LogMessage($"[UpdateChecker] Server_Aux02: {Server_Aux02}");

                GetRustVersionAsync_Starting("ruststaging.txt");
                LogMessage($"[UpdateChecker] Staging_Public: {Staging_Public}");
                LogMessage($"[UpdateChecker] Staging_Main: {Staging_Main}");
                LogMessage($"[UpdateChecker] Staging_Aux02: {Staging_Aux02}");

                if (IsValidVersion(Main_Public) &&
                    IsValidVersion(Server_Public) && IsValidVersion(Server_Staging) && IsValidVersion(Server_Aux02) &&
                    IsValidVersion(Staging_Public) && IsValidVersion(Staging_Main) && IsValidVersion(Staging_Aux02))
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
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(90));

            var response_main = await httpClient.GetAsync(skinApiUrl, cancellationTokenSource.Token);
            if (!response_main.IsSuccessStatusCode) { LogMessage("[SkinTracker] response contect null."); await webhookLogs.SendMessageAsync($"<@{_PingMavi}> | skin response main null."); return; }

            var response = await response_main.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(response)) { LogMessage("[SkinTracker] response null."); await webhookLogs.SendMessageAsync($"<@{_PingMavi}> | skin response null."); return; }

            List<SkinItem> skinData = ParseSkins(response);
            if (skinData == null || skinData.Count == 0) { LogMessage("[SkinTracker] skinData null."); await webhookLogs.SendMessageAsync($"<@{_PingMavi}> | skin data null."); return; }

            var newSkins = skinData.Select(Skin => Skin.Name).ToList();

            foreach (var item in skinData) LogMessage($"[SkinTracker] Name: {item.Name} | Price: {item.Price} | Image: {item.Image}");

            if (storedSkins.Count == 0)
            {
                storedSkins.Clear();
                storedSkins.UnionWith(newSkins);
                return;
            }

            var differences = skinData.Where(Skin => newSkins.Contains(Skin.Name) && !storedSkins.Contains(Skin.Name)).ToList();
            if (differences.Any())
            {
                var ourimage = CreateBigImage(skinData);
                ourimage.Save("skinimage.png", System.Drawing.Imaging.ImageFormat.Png);
                if (ourimage == null) { await webhookLogs.SendMessageAsync($"<@{_PingMavi}> | skin image null"); return; }

                var skincount = skinData.Count;
                float totalcost = 0;
                foreach (var skin in skinData)
                {
                    float price = float.Parse(skin.Price.Replace("$", ""), CultureInfo.InvariantCulture);
                    totalcost += price;
                }
                LogMessage($"[SkinTracker] Mağaza Yenilendi --> {skincount} yeni kostüm. Toplam Kostüm Değeri: {totalcost}$");

                EmbedBuilder embedBuildformain = new EmbedBuilder()
                .WithTitle(":bell: MAĞAZA YENİLENDİ! | " + DateTime.Now.ToShortDateString() + " :bell:")
                .WithColor(Discord.Color.Blue)
                .WithDescription($"**{skincount}** yeni kostüm mağazaye eklendi.\n\n Toplam Kostüm Değeri: **{totalcost}$**")
                .WithUrl("https://store.steampowered.com/itemstore/252490/");

                var guildlist = storeCheckerChannel_IDS.Keys.ToList();
                foreach (var guildId in guildlist)
                {
                    var guild = _client.GetGuild(guildId);
                    if (guild == null) continue;
                    var channelIds = storeCheckerChannel_IDS[guildId].ToList();
                    foreach (var channelId in channelIds)
                    {
                        var channel = guild.GetTextChannel(channelId);
                        if (channel == null) continue;
                        if (!await CheckBotPerms(guild) || !await CheckChannelPerms(channel)) { await NoPermsSendMessage(guild, "(Haftalık Mağaza Kanalı)"); continue; };
                        await channel.SendMessageAsync("@everyone", false, embedBuildformain.Build());
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
                    var guidlist = storeCheckerChannel_IDS.Keys.ToList();
                    foreach (var guildId in guidlist)
                    {
                        var guild = _client.GetGuild(guildId);
                        if (guild == null) continue;
                        var channelIds = storeCheckerChannel_IDS[guildId].ToList();
                        foreach (var channelId in channelIds)
                        {
                            var channel = guild.GetTextChannel(channelId);
                            if (channel == null) continue;
                            using (var memoryStream = new MemoryStream(imageData))
                            {
                                if (!await CheckBotPerms(guild) || !await CheckChannelPerms(channel)) { await NoPermsSendMessage(guild, "(Haftalık Mağaza Kanalı - Resim)"); continue; };
                                await channel.SendFileAsync(memoryStream, "skinimage.png");
                            }
                        }
                    }
                }
                File.Delete("skinimage.png");
                storedSkins.Clear();
                storedSkins.UnionWith(newSkins);
            }
        }

        private static System.Drawing.Image CreateBigImage(List<SkinItem> skindata)
        {
            var imageUrls = skindata.Select(Skin => Skin.Image).ToList();
            var skinNames = skindata.Select(Skin => Skin.Name).ToList();
            var skinPrices = skindata.Select(Skin => Skin.Price).ToList();
            int imageSize = (int)Math.Ceiling(Math.Sqrt(imageUrls.Count));
            int squareSize = imageSize * 400;
            Bitmap combinedImage = new Bitmap(squareSize, squareSize);
            Bitmap backgroundImage;

            using (MemoryStream stream = new MemoryStream())
            {
                Properties.Resources.background.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                backgroundImage = new Bitmap(stream);
            }

            using (Graphics g = Graphics.FromImage(combinedImage))
            {
                g.DrawImage(backgroundImage, 0, 0, combinedImage.Width, combinedImage.Height);

                int x = 0, y = 0;
                int index = 0;

                foreach (var url in imageUrls)
                {
                    using (WebClient client = new WebClient())
                    {
                        byte[] imageData = client.DownloadData(url);
                        using (MemoryStream stream = new MemoryStream(imageData))
                        {
                            System.Drawing.Image img = System.Drawing.Image.FromStream(stream);

                            g.DrawImage(img, x * 400, y * 400, 350, 350);

                            var skinname = skinNames[index];
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
                            }

                            using (Pen pen = new Pen(System.Drawing.Color.Black, 2))
                            {
                                g.DrawRectangle(pen, x * 400, y * 400, 400, 400);
                            }
                        }
                    }

                    index++; x++;
                    if (x >= imageSize) { x = 0; y++; }
                    if (index >= imageSize * imageSize) break;
                }
            }

            return combinedImage;
        }

        private static List<SkinItem> ParseSkins(string html)
        {
            List<SkinItem> skinItems = new List<SkinItem>();
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var storeItems = doc.DocumentNode.SelectNodes("//div[contains(@class, 'item_def_grid_item')]");
            if (storeItems == null) return null;
            for (int i = 0; i < storeItems.Count; i++)
            {
                var storeItem = storeItems[i];
                SkinItem skinItem = new SkinItem();

                HtmlNode nameNode = storeItem.SelectSingleNode(".//div[contains(@class, 'item_def_name')]/a");
                HtmlNode priceNode = storeItem.SelectSingleNode(".//div[contains(@class, 'item_def_price')]");
                HtmlNode itemImageNode = storeItem.SelectSingleNode(".//div[contains(@class, 'item_def_icon_container')]/a/img");

                if (nameNode != null) skinItem.Name = nameNode.InnerText.Trim();
                if (priceNode != null) skinItem.Price = priceNode.InnerText.Trim();
                if (itemImageNode != null) skinItem.Image = itemImageNode.GetAttributeValue("src", string.Empty);

                skinItems.Add(skinItem);
            }

            return skinItems;
        }

        private static void GetRustVersionAsync(string scriptFileName, ref string OptArg1, ref string OptArg2, ref string OptArg3)
        {
            string outputFileName = $"out/{Path.GetFileNameWithoutExtension(scriptFileName)}out.txt";
            string scriptpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", scriptFileName);

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

                if (scriptFileName == "rustapp.txt")
                {
                    OptArg1 = FindVersion(outputFileName, "public");
                }
                else if (scriptFileName == "rustserver.txt")
                {
                    OptArg1 = FindVersion(outputFileName, "public");
                    OptArg2 = FindVersion(outputFileName, "staging");
                    OptArg3 = FindVersion(outputFileName, "aux02");
                }
                else if (scriptFileName == "ruststaging.txt")
                {
                    OptArg1 = FindVersion(outputFileName, "public");
                    OptArg2 = FindVersion(outputFileName, "main");
                    OptArg3 = FindVersion(outputFileName, "aux02");
                }
            }
        }

        private static void GetRustVersionAsync_Starting(string scriptFileName)
        {
            string outputFileName = $"out/{Path.GetFileNameWithoutExtension(scriptFileName)}out.txt";
            string scriptpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", scriptFileName);

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

                if (scriptFileName == "rustapp.txt")
                {
                    Main_Public = FindVersion(outputFileName, "public");
                }
                else if (scriptFileName == "rustserver.txt")
                {
                    Server_Public = FindVersion(outputFileName, "public");
                    Server_Staging = FindVersion(outputFileName, "staging");
                    Server_Aux02 = FindVersion(outputFileName, "aux02");
                }
                else if (scriptFileName == "ruststaging.txt")
                {
                    Staging_Public = FindVersion(outputFileName, "public");
                    Staging_Main = FindVersion(outputFileName, "main");
                    Staging_Aux02 = FindVersion(outputFileName, "aux02");
                }
            }
        }

        private static string FindVersion(string path, string branch)
        {
            string fileContent = File.ReadAllText(path);

            int branchesStartIndex = fileContent.IndexOf("\"branches\"");
            if (branchesStartIndex == -1) { LogMessage("branches cant found."); return null; }
            int startIndex = fileContent.IndexOf('{', branchesStartIndex);
            int endIndex = FindClosingBraceIndex(fileContent, startIndex);
            if (startIndex == -1 || endIndex == -1) { LogMessage("cant parse branches section"); return null; }
            string branchesSection = fileContent.Substring(startIndex, endIndex - startIndex + 1);

            int aux02StartIndex = branchesSection.IndexOf($"\"{branch}\"");
            if (aux02StartIndex == -1) { LogMessage("minor branch cant found."); }
            startIndex = branchesSection.IndexOf('{', aux02StartIndex);
            endIndex = FindClosingBraceIndex(branchesSection, startIndex);
            if (startIndex == -1 || endIndex == -1) { LogMessage("cant parse minor branch section"); return null; }
            string aux02Section = branchesSection.Substring(startIndex, endIndex - startIndex + 1);

            string buildId = GetPropertyValue(aux02Section, "buildid");
            if (buildId == null) { LogMessage("buildid cant found."); return null; }
            else return buildId;
        }

        private static string GetPropertyValue(string text, string propertyName)
        {
            string pattern = $"\"{propertyName}\"\\s*\"(.*?)\"";
            Match match = Regex.Match(text, pattern);
            if (match.Success) return match.Groups[1].Value;
            return null;
        }

        private static int FindClosingBraceIndex(string text, int startIndex)
        {
            int braceCount = 0;
            for (int i = startIndex; i < text.Length; i++)
            {
                if (text[i] == '{') { braceCount++; }
                else if (text[i] == '}') { braceCount--; if (braceCount == 0) return i; }
            }
            return -1;
        }

        private static async Task UpdateVersionIfNeeded(string newVersion, string currentVersion, string title, string message, bool everyone, Action<string> updateCurrentVersion)
        {
            if (IsValidVersion(newVersion) && newVersion != currentVersion)
            {
                LogMessage($"[UpdateChecker] Update found! {currentVersion} -> {newVersion}");
                string change = $"{currentVersion} --> {newVersion}";
                await SendUpdateMessage(title, message, change, everyone);
                updateCurrentVersion(newVersion);
            }
        }

        private static async Task CheckAndUpdateVersion(string MainL, string ServerPublicL, string ServerStagingL, string ServerAuxL, string StagingPublicL, string StagingMainL, string StagingAuxL)
        {
            await UpdateVersionIfNeeded(MainL, Main_Public, ":radioactive: **Oyuncular için yeni bir Güncelleme geldi!** :radioactive:", "Güncellemeyi görmüyorsanız, steaminizi yeniden başlatmanız gerekiyor.", true, v => Main_Public = v);
            await UpdateVersionIfNeeded(ServerPublicL, Server_Public, ":radioactive: **Sunucular için yeni bir Güncelleme geldi!** :radioactive:", "Sunucu sahipleri, sunucularını güncelleyebilirler.", true, v => Server_Public = v);
            await UpdateVersionIfNeeded(ServerStagingL, Server_Staging, "", "**Sunucu** taraflı Rust Staging güncellemesi.", false, v => Server_Staging = v);
            await UpdateVersionIfNeeded(ServerAuxL, Server_Aux02, "", "**Sunucu** taraflı Rust Staging - **Aux02** güncellemesi.", false, v => Server_Aux02 = v);
            await UpdateVersionIfNeeded(StagingPublicL, Staging_Public, "", "**İstemci** taraflı Rust Staging - **Public** güncellemesi.", false, v => Staging_Public = v);
            await UpdateVersionIfNeeded(StagingMainL, Staging_Main, "", "**İstemci** taraflı Rust Staging - **Main** güncellemesi.", false, v => Staging_Main = v);
            await UpdateVersionIfNeeded(StagingAuxL, Staging_Aux02, "", "**İstemci** taraflı Rust Staging - **Aux02** güncellemesi.", false, v => Staging_Aux02 = v);
        }

        private static async Task SendUpdateMessage(string title, string message, string changemsg, bool everyone)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(message)
            .WithColor(Discord.Color.Blue)
            .WithThumbnailUrl("https://yt3.googleusercontent.com/HPu-kTkwgN4mPxO6_PJThrtbPQEL_esHXjbPVp7bR5SF3H0HX_p6ub960hiH-D5WiDtPTosOXw=s176-c-k-c0x00ffffff-no-rj")
            .AddField("Sürüm Numarası Değişimi:", changemsg, true);
            var guildlist = updateTrackerChannel_IDS.Keys.ToList();
            foreach (var guildId in guildlist)
            {
                var guild = _client.GetGuild(guildId);
                if (guild == null) continue;
                var channelids = updateTrackerChannel_IDS[guildId].ToList();
                foreach (var channelId in channelids)
                {
                    var channel = guild.GetTextChannel(channelId);
                    if (channel == null) continue;
                    if (!await CheckBotPerms(guild) || !await CheckChannelPerms(channel)) { await NoPermsSendMessage(guild, "(Güncelleme Takip Kanalı)"); continue; };
                    if (everyone) await channel.SendMessageAsync("@everyone", false, embedBuilder.Build());
                    else if (guild.Id == _MainDiscord && channel.Id == 1243032097099350029) await channel.SendMessageAsync($"<@{_PingMavi}>", false, embedBuilder.Build());
                    else { await channel.SendMessageAsync("", false, embedBuilder.Build()); }
                }
            }
        }

        private static async Task CheckForNewCommits()
        {
            var response = await httpClient.GetStringAsync(commitApiUrl);
            var commitData = JsonConvert.DeserializeObject<CommitData>(response);
            if (commitData?.Results == null || !commitData.Results.Any()) { LogMessage("[CommitTracker] commitData null."); await webhookLogs.SendMessageAsync($"<@{_PingMavi}> | Commit data null."); return; }

            var newCommits = commitData.Results.Select(commit => commit.message).ToList();
            if (newCommits == null || !newCommits.Any()) { LogMessage("[CommitTracker] newCommits null."); await webhookLogs.SendMessageAsync($"<@{_PingMavi}> | new commits null."); return; }

            if (storedCommits.Count == 0) { storedCommits.UnionWith(newCommits); await webhookLogs.SendMessageAsync($"<@{_PingMavi}> | Commit first run"); return; }

            var differences = commitData.Results.Where(commit => newCommits.Contains(commit.message) && !storedCommits.Contains(commit.message)).ToList();
            if (!differences.Any()) { return; }

            foreach (var commit in differences)
            {
                LogMessage($"[CommitTracker] Yeni Commit: {commit.id}");
                var commitlink = "https://commits.facepunch.com/" + commit.id;
                EmbedBuilder newEmbedBuilder = new EmbedBuilder()
                .WithAuthor(commit.user.name, commit.user.avatar)
                .WithTitle(commit.branch)
                .WithDescription(commit.message)
                .WithUrl(commitlink)
                .WithColor(Discord.Color.Blue)
                .WithFooter($"ID: {commit.id} | Change: {commit.changeset} | {DateTime.Now.ToString()}");

                var guildlist = commitFollowerChannel_IDS.Keys.ToList();
                foreach (var guildId in guildlist)
                {
                    var guild = _client.GetGuild(guildId);
                    if (guild == null) continue;
                    var channelids = commitFollowerChannel_IDS[guildId].ToList();
                    foreach (var channelId in channelids)
                    {
                        var channel = guild.GetTextChannel(channelId);
                        if (channel == null) continue;
                        if (!await CheckBotPerms(guild) || !await CheckChannelPerms(channel)) { await NoPermsSendMessage(guild, "(Commit Takip Kanalı)"); continue; };
                        await channel.SendMessageAsync("", false, newEmbedBuilder.Build());
                    }
                }
            }
            storedCommits.UnionWith(newCommits);

            LogMessage($"[CommitTracker] Depolanan: {storedCommits.Count}");
        }

        private static Task ResponderUpdate()
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
            EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithTitle(":information_source:  **Güncelleme Bilgisi**  :information_source:")
            .WithDescription("Her ayın ilk perşembesi (Yaz Dönemi 21:00 - Kış Dönemi 22:00) gelen güncelleme ile tüm sunuculara **Zorunlu Harita Sıfırlaması** atılır.\n**BP Sıfırlaması**(Blueprint/Öğrenilen Eşyalar) ise sunucu sahibinin isteğine bağlıdır.")
            .WithThumbnailUrl("https://yt3.googleusercontent.com/HPu-kTkwgN4mPxO6_PJThrtbPQEL_esHXjbPVp7bR5SF3H0HX_p6ub960hiH-D5WiDtPTosOXw=s176-c-k-c0x00ffffff-no-rj")
            .AddField("Sonraki Güncelleme Tarihi:", $"<t:{_nextUpdateTimestamp}:F>", false)
            .AddField("Sonraki Güncellemeye Kalan Zaman:", $"<t:{_nextUpdateTimestamp}:R>", false)
            .WithColor(Discord.Color.Blue)
            .WithFooter($"Son Güncelleme: {DateTime.Now.ToString()}");
            var guildlist = updateDateChannel_IDS.Keys.ToList();
            foreach (var guildId in guildlist)
            {
                var guild = _client.GetGuild(guildId);
                if (guild == null) continue;
                var channelids = updateDateChannel_IDS[guildId].ToList();
                foreach (var channelId in channelids)
                {
                    var channel = guild.GetTextChannel(channelId);
                    if (channel == null) continue;
                    var testr = await CheckBotPerms(guild);
                    if (!await CheckBotPerms(guild) || !await CheckChannelPerms(channel)) { await NoPermsSendMessage(guild, "(Güncelleme Tarihi kanalı)"); continue; };
                    var messages = await channel.GetMessagesAsync(limit: 1).FlattenAsync();
                    var lastMessage = messages.FirstOrDefault() as IUserMessage;
                    if (lastMessage != null && lastMessage.Author.Id == _client.CurrentUser.Id) await lastMessage.ModifyAsync(msg => msg.Embed = embedBuilder.Build());
                    else await channel.SendMessageAsync("", false, embedBuilder.Build());
                }
            }
        }

        private static Task MessageReceived(SocketMessage message)
        {
            if (message.Channel.Id == _SohbetKanalID)
            {
                IUser user = message.Author;
                string userTag = $"{user.Mention}";
                if (message.Author.Id == _client.CurrentUser.Id) return Task.CompletedTask;
                if (UpdateKeys.Any(keyword => message.Content.ToLower().Contains(keyword)))
                {
                    LogMessage("[Responder] Güncelleme sorusu cevaplanıyor...");
                    EmbedBuilder embedBuilder = new EmbedBuilder()
                    .WithTitle(":information_source:  **Güncelleme Bilgisi**  :information_source:")
                    .WithDescription("Her ayın ilk perşembesi (Yaz Dönemi 21:00 - Kış Dönemi 22:00) gelen güncelleme ile tüm sunuculara **Zorunlu Harita Sıfırlaması** atılır.\n**BP Sıfırlaması**(Blueprint/Öğrenilen Eşyalar) ise sunucu sahibinin isteğine bağlıdır.")
                    .WithThumbnailUrl("https://yt3.googleusercontent.com/HPu-kTkwgN4mPxO6_PJThrtbPQEL_esHXjbPVp7bR5SF3H0HX_p6ub960hiH-D5WiDtPTosOXw=s176-c-k-c0x00ffffff-no-rj")
                    .WithFooter(DateTime.Now.ToString(), "https://lh3.googleusercontent.com/a/ACg8ocJveuYqbU6KTFvsKpkmNLtB35Gd8-fsAbZzu3JVknZGDw=s288-c-no")
                    .AddField("Sonraki Güncelleme Tarihi:", $"<t:{_nextUpdateTimestamp}:F>", false)
                    .AddField("Sonraki Güncellemeye Kalan Zaman:", $"<t:{_nextUpdateTimestamp}:R>", false)
                    .AddField("Soran Kullanıcı", userTag, false)
                    .WithColor(Discord.Color.Blue);
                    return message.Channel.SendMessageAsync("", false, embedBuilder.Build());
                }
            }
            return Task.CompletedTask;
        }

        private async Task OnJoinedGuild(SocketGuild guild)
        {
            LogMessage($"New guild: {guild}");
            await webhookLogs.SendMessageAsync($"New guild: {guild}");
            await Initialize_Channels();
        }

        private async Task OnLeaveGuild(SocketGuild guild)
        {
            LogMessage($"Guild leaved: {guild}");
            await webhookLogs.SendMessageAsync($"Guild leaved: {guild}");
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

        private static Task NoPermsSendMessage(SocketGuild CurrentGuild, string detay)
        {
            LogMessage($"Yetersiz yetki. {detay} | Guild: {CurrentGuild.Name} - Id: {CurrentGuild.Id}");
            return Task.CompletedTask;
        }

        private static async Task<bool> CheckBotPerms(SocketGuild guild)
        {
            var requiredPermissions = new[] {
               GuildPermission.AttachFiles, GuildPermission.EmbedLinks, GuildPermission.ManageChannels,GuildPermission.ManageRoles,GuildPermission.ManageWebhooks,
               GuildPermission.MentionEveryone, GuildPermission.ReadMessageHistory, GuildPermission.SendMessages,GuildPermission.ViewChannel,GuildPermission.UseApplicationCommands};
            var botUser = guild.CurrentUser;
            return await Task.FromResult(requiredPermissions.All(permission => botUser.GuildPermissions.Has(permission)));
        }

        private static async Task<bool> CheckChannelPerms(SocketTextChannel channel)
        {
            var requiredPermissions = new[] {
                ChannelPermission.AttachFiles, ChannelPermission.EmbedLinks, ChannelPermission.ManageChannels,ChannelPermission.ManageRoles,ChannelPermission.ManageWebhooks,
                ChannelPermission.MentionEveryone, ChannelPermission.ReadMessageHistory, ChannelPermission.SendMessages,ChannelPermission.ViewChannel,ChannelPermission.UseApplicationCommands};
            var permissions = channel.Guild.CurrentUser.GetPermissions(channel);
            return await Task.FromResult(requiredPermissions.All(permission => permissions.Has(permission)));
        }

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
            public string Price { get; set; }
            public string Image { get; set; }
        }
    }
}