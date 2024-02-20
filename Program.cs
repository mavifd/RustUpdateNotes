using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace RustTurkiye_Responder
{
    internal class Program
    {
        private DiscordSocketClient _client;
        private ulong _SohbetKanalID = 448607745122369536;
        private ulong _KlanAramaKanalID = 448441303043145729;
        private ulong _CommitKanalID = 1134966799876763658;
        private ulong _UpdateKanalID = 473843211954028544;

        private long _nextUpdateTimestamp = 0;

        private static HttpClient httpClient = new HttpClient();
        private static string apiUrl = "https://commits.facepunch.com/r/rust_reboot/?format=json";

        private static HashSet<string> storedCommits = new HashSet<string>();
        private static HashSet<string> sentCommits = new HashSet<string>();

        private static int maxCommits = 2000;
        private static int keepLatestCommits = 200;

        private static string EnSonOyun = "";
        private static string EnSonSunucu = "";

        private static void Main(string[] args)
        {
            Console.Title = "RT_Kontrol - Starting...";

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
                Task.Delay(5000);
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
                File.WriteAllText("scripts\\rustapp.txt", RT_Kontrol.Properties.Resources.rustapp);
                File.WriteAllText("scripts\\rustserver.txt", RT_Kontrol.Properties.Resources.rustserver);
            }
            else
            {
                LogMessage("[UpdateChecker] Script dosyası mevcut.");
                if (!File.Exists("scripts\\rustapp.txt") && !File.Exists("scripts\\rustserver.txt") && !File.Exists("scripts\\ruststaging.txt"))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    LogMessage("[UpdateChecker] Script dosyası içeriği oluşturuluyor.");
                    Console.ResetColor();
                    File.WriteAllText("scripts\\rustapp.txt", RT_Kontrol.Properties.Resources.rustapp);
                    File.WriteAllText("scripts\\rustserver.txt", RT_Kontrol.Properties.Resources.rustserver);
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
                EnSonOyun = GetRustVersionAsync("rustapp.txt").Result;

                LogMessage($"[UpdateChecker] Oyun Başlangıç Değeri: {EnSonOyun}");

                EnSonSunucu = GetRustVersionAsync("rustserver.txt").Result;

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

            _client.Ready += BotReady;

            while (!_botReady)
            {
                await Task.Delay(100);
            }

            System.Timers.Timer ResponderRunner = new System.Timers.Timer(60000); // 1 dakikada bir cevap güncelle
            ResponderRunner.Elapsed += (sender, e) => ResponderRunner_Elapse();
            ResponderRunner.Start();

            System.Timers.Timer CommitTrackRunner = new System.Timers.Timer(60000); // 1 dakikada bir commit kontrol et
            CommitTrackRunner.Elapsed += (sender, e) => CommitTrack_Elapse();
            CommitTrackRunner.Start();

            System.Timers.Timer UpdateCheckRunner = new System.Timers.Timer(1); // güncellemeyi mümkün olduğu sürece kontrol et
            UpdateCheckRunner.Elapsed += (sender, e) => UpdateCheck_Elapse();
            UpdateCheckRunner.Start();

            System.Timers.Timer RunTimeChecker = new System.Timers.Timer(1000); // güncellemeyi mümkün olduğu sürece kontrol et
            RunTimeChecker.Elapsed += (sender, e) => RunTimeChecker_Elapse();
            RunTimeChecker.Start();

            await Task.Delay(-1);
        }

        private bool _botReady = false;

        private Task BotReady()
        {
            _botReady = true;
            return Task.CompletedTask;
        }

        private static DateTime startTime;

        private void RunTimeChecker_Elapse()
        {
            TimeSpan elapsed = DateTime.Now - startTime;
            int days = elapsed.Days;
            int hours = elapsed.Hours;
            int minutes = elapsed.Minutes;
            int seconds = elapsed.Seconds;
            if (seconds == 59)
            {
                seconds = 0;
                minutes++;

                if (minutes == 60)
                {
                    minutes = 0;
                    hours++;

                    if (hours == 24)
                    {
                        hours = 0;
                        days++;
                    }
                }
            }
            Console.Title = $"RT_Kontrol - Running for {days} Day | {hours:D2}:{minutes:D2}:{seconds:D2}";
        }

        private static readonly object fileLock = new object();

        private void UpdateCheck_Elapse()
        {
            lock (fileLock)
            {
                string mevcutOyun = GetRustVersionAsync("rustapp.txt").Result;

                LogMessage($"[UpdateChecker] Oyun Mevcut Sürüm: {mevcutOyun}");

                string mevcutSunucu = GetRustVersionAsync("rustserver.txt").Result;

                LogMessage($"[UpdateChecker] Sunucu Mevcut Sürüm: {mevcutSunucu}");

                CheckAndUpdateVersion(
                   EnSonOyun,
                   mevcutOyun,
                   ":radioactive: **Oyuncular için yeni bir Güncelleme geldi!** :radioactive:",
                   "Güncellemeyi görmüyorsanız, Steaminizi yeniden başlatın.",
                   false);

                CheckAndUpdateVersion(
                   EnSonSunucu,
                   mevcutSunucu,
                   ":radioactive: **Sunucular için yeni bir Güncelleme geldi!** :radioactive:",
                   "Sunucu sahipleri, sunucularını güncelleyebilir.",
                   true);
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
                Console.WriteLine($"[UpdateChecker] Genel Hata: {ex.Message}");
                return "not valid";
            }
        }

        private async void CheckAndUpdateVersion(string MainVersion, string CurrentVersion, string message, string description, bool server)
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

        private async void CommitTrack_Elapse()
        {
            await CheckForNewCommits();
        }

        private async Task CheckForNewCommits()
        {
            try
            {
                var response = await httpClient.GetStringAsync(apiUrl);
                var commitData = JsonConvert.DeserializeObject<CommitData>(response);

                if (commitData?.Results.Count() != null)
                {
                    var newCommits = commitData.Results.Select(commit => commit.message).ToList();

                    if (storedCommits.Count >= maxCommits)
                    {
                        var oldestCommits = storedCommits.Take(storedCommits.Count - keepLatestCommits).ToList();
                        LogMessage("[CommitTracker] Eski veriler (stored) siliniyor.");
                        foreach (var commit in oldestCommits)
                        {
                            storedCommits.Remove(commit);
                        }
                    }

                    if (sentCommits.Count >= maxCommits)
                    {
                        var oldestCommits = sentCommits.Take(sentCommits.Count - keepLatestCommits).ToList();
                        LogMessage("[CommitTracker] Eski veriler (sended) siliniyor.");
                        foreach (var commit in oldestCommits)
                        {
                            sentCommits.Remove(commit);
                        }
                    }

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
                                embedBuilder.WithTitle(commit.user.name + "\n" + commit.branch);
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

        private void ResponderRunner_Elapse()
        {
            DateTime bugun = DateTime.Today;
            DateTime saat21 = bugun.AddHours(22);
            long bugüntimestamp = ((DateTimeOffset)saat21).ToUnixTimeSeconds();
            DateTime suAn = DateTime.Now;
            DateTime ilkGun = new DateTime(suAn.Year, suAn.Month, 1);
            int gunDegeri = (int)ilkGun.DayOfWeek;
            DateTime ilkPersembe = ilkGun.AddDays((4 - gunDegeri + 7) % 7);
            DateTime saat21ilkay = ilkPersembe.AddHours(22);
            long ayinilkpersembesitimestamp = ((DateTimeOffset)saat21ilkay).ToUnixTimeSeconds();
            if (bugüntimestamp > ayinilkpersembesitimestamp)
            {
                DateTime birSonrakiAyinIlkPersembesi = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(1);
                while (birSonrakiAyinIlkPersembesi.DayOfWeek != DayOfWeek.Thursday)
                {
                    birSonrakiAyinIlkPersembesi = birSonrakiAyinIlkPersembesi.AddDays(1);
                }
                birSonrakiAyinIlkPersembesi = birSonrakiAyinIlkPersembesi.AddHours(22);
                _nextUpdateTimestamp = new DateTimeOffset(birSonrakiAyinIlkPersembesi).ToUnixTimeSeconds();
            }
            else
            {
                DateTime buAyinIlkPersembesi = new DateTime(bugun.Year, bugun.Month, 1);
                while (buAyinIlkPersembesi.DayOfWeek != DayOfWeek.Thursday)
                {
                    buAyinIlkPersembesi = buAyinIlkPersembesi.AddDays(1);
                }
                buAyinIlkPersembesi = buAyinIlkPersembesi.AddHours(22);
                _nextUpdateTimestamp = new DateTimeOffset(buAyinIlkPersembesi).ToUnixTimeSeconds();
            }
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(_nextUpdateTimestamp);
            DateTime dateTime = dateTimeOffset.DateTime;
            LogMessage($"[Responder] Sonraki Güncelleme Tarihi: {dateTime}");
        }

        private List<string> blacklistedkeywordList = new List<string>
        {
            "istisna",
            "sürtük",
            "tüzük",
            "zehir",
            "temizle",
            "iyi iletişim",
            "silme"
        };

        private List<string> updatekeywordList = new List<string>
        {
            "wipe",
            "güncelleme",
            "global",
            "update"
        };

        private Task MessageReceived(SocketMessage message)
        {
            if (message.Channel.Id == _SohbetKanalID)
            {
                if (message.Author.Id != _client.CurrentUser.Id)
                {
                    if (updatekeywordList.Any(keyword => message.Content.ToLower().Contains(keyword)))
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

            if (message.Channel.Id == _KlanAramaKanalID)
            {
                if (message.Author.Id != _client.CurrentUser.Id)
                {
                    if (blacklistedkeywordList.Any(keyword => message.Content.ToLower().Contains(keyword)))
                    {
                        LogMessage("[Responder] Ceza veriliyor!");

                        SocketGuildUser user = message.Author as SocketGuildUser;

                        IRole roleToAssign = user.Guild.Roles.FirstOrDefault(x => x.Name == "CEZALI");

                        if (roleToAssign != null)
                        {
                            user.AddRoleAsync(roleToAssign);
                        }
                        else
                        {
                            LogMessage("[Responder] Cezalı rolü verilemedi.");
                        }

                        LogMessage("[Responder] Mesaj siliniyor...");

                        message.DeleteAsync();
                    }
                }
            }

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
    }
}