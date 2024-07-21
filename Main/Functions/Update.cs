using Discord;
using RustUpdateNotes.GlobalClass;
using RustUpdateNotes.LoggerClass;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RustUpdateNotes.UpdateClass
{
    public static class Update
    {
        private static string Main = "";
        private static string Server = "";

        public static async Task Update_Runner()
        {
            await Initialize_UpdateChecker();
            while (true)
            {
                var maintask = CheckUpdates();
                var controltask = Task.Delay(TimeSpan.FromMinutes(5));
                var completedTask = await Task.WhenAny(maintask, controltask);
                if (completedTask != maintask)
                {
                    Logger.LogMessage($"UpdateRunner Timeout (5 minute)");
                    await Logger.DiscordMessage($"UpdateRunner Timeout (5 minute)", true);
                }
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        public static async Task CheckUpdates()
        {
            try
            {

                if (string.IsNullOrEmpty(Main) || string.IsNullOrEmpty(Server))
                {
                    Logger.LogMessage($"Main/Server null or empty. Getting version numbers...");
                    GetRustVersionAsync("rustapp.txt", ref Main);
                    Logger.LogMessage($"Main: {Main}");
                    GetRustVersionAsync("rustserver.txt", ref Server);
                    Logger.LogMessage($"Server: {Server}");
                    return;
                }

                string MainL = "1";
                string ServerL = "1";
                GetRustVersionAsync("rustapp.txt", ref MainL);
                Logger.LogMessage($"Main: {MainL}");
                GetRustVersionAsync("rustserver.txt", ref ServerL);
                Logger.LogMessage($"Server: {ServerL}");
                await CheckAndUpdateVersion(MainL, ServerL);
            }
            catch (Exception ex)
            {
                Logger.LogMessage($"Error - Update_Runner: {ex}");
                await Logger.DiscordMessage($"Error - Update_Runner: {ex}", true);
            }
        }

        private static async Task CheckAndUpdateVersion(string MainL, string ServerL)
        {
            await UpdateVersionIfNeeded(MainL, Main, ":radioactive: **Oyuncular için yeni bir Güncelleme geldi!** :radioactive:", "Güncellemeyi görmüyorsanız, steaminizi yeniden başlatmanız gerekiyor.", true, v => Main = v);
            await UpdateVersionIfNeeded(ServerL, Server, ":radioactive: **Sunucular için yeni bir Güncelleme geldi!** :radioactive:", "Sunucu sahipleri, sunucularını güncelleyebilirler.", true, v => Server = v);
        }

        private static async Task UpdateVersionIfNeeded(string newVersion, string currentVersion, string title, string message, bool local, Action<string> updateCurrentVersion)
        {
            if (IsValidVersion(newVersion) && newVersion != currentVersion)
            {
                Logger.LogMessage($"New update: {currentVersion} -> {newVersion}");
                string change = $"{currentVersion} --> {newVersion}";
                await SendUpdateMessage(title, message, change);
                updateCurrentVersion(newVersion);
            }
        }

        private static async Task SendUpdateMessage(string title, string message, string changemsg)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(message)
            .WithColor(Color.Blue)
            .WithThumbnailUrl("https://yt3.googleusercontent.com/HPu-kTkwgN4mPxO6_PJThrtbPQEL_esHXjbPVp7bR5SF3H0HX_p6ub960hiH-D5WiDtPTosOXw=s176-c-k-c0x00ffffff-no-rj")
            .AddField("Derleme Değişimi:", changemsg, true);
            var guildlist = Global.UpdateTrackerChannels.Keys.ToList();
            foreach (var guildId in guildlist)
            {
                var guild = Global.Client.GetGuild(guildId);
                if (guild == null)
                {
                    continue;
                }
                var channelids = Global.UpdateTrackerChannels[guildId].ToList();
                foreach (var channelId in channelids)
                {
                    var channel = guild.GetTextChannel(channelId);
                    if (channel == null)
                    {
                        continue;
                    }
                    if (!await Logger.CheckBotPerms(guild) || !await Logger.CheckChannelPerms(channel))
                    {
                        Logger.LogMessage($"Güncelleme Takip Yetki Yetersizliği| Guild: {guild.Name}");
                        continue;
                    };
                    await channel.SendMessageAsync("@everyone", false, embedBuilder.Build());
                }
            }
        }

        public static async Task Initialize_UpdateChecker()
        {
            Logger.LogMessage($"SteamCMD kontrol ediliyor...");
            if (File.Exists("C:\\steamcmd\\steamcmd.exe"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Logger.LogMessage($"SteamCMD doğru dizine kurulu.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Logger.LogMessage($"SteamCMD bulunamadı.");
                Console.ResetColor();
                await Task.Delay(5000);
                Environment.Exit(1);
            }
            if (!Directory.Exists("scripts"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Logger.LogMessage($"Script dosyası oluşturuluyor.");

                Console.ResetColor();
                Directory.CreateDirectory("scripts");
                Console.ForegroundColor = ConsoleColor.Green;
                Logger.LogMessage($"Script dosyası içeriği oluşturuluyor.");
                Console.ResetColor();
                File.WriteAllText("scripts\\rustapp.txt", Properties.Resources.rustapp);
                File.WriteAllText("scripts\\rustserver.txt", Properties.Resources.rustserver);
            }
            else
            {
                Logger.LogMessage($"Script dosyası mevcut.");
                if (!File.Exists("scripts\\rustapp.txt") && !File.Exists("scripts\\rustserver.txt"))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Logger.LogMessage($"Script dosyası içeriği oluşturuluyor.");
                    Console.ResetColor();
                    File.WriteAllText("scripts\\rustapp.txt", Properties.Resources.rustapp);
                    File.WriteAllText("scripts\\rustserver.txt", Properties.Resources.rustserver);
                }
                else
                {
                    Logger.LogMessage($"Script dosya içeriği mevcut.");
                }
            }

            if (!Directory.Exists("out"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Logger.LogMessage($"Out dosyası oluşturuluyor.");
                Console.ResetColor();
                Directory.CreateDirectory("out");
            }
            Console.ResetColor();
            Logger.LogMessage($"Döngüye giriliyor...");
        }

        private static void GetRustVersionAsync(string scriptFileName, ref string OptArg1)
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
                }
            }
        }

        private static string FindVersion(string path, string branch)
        {
            string fileContent = File.ReadAllText(path);

            int branchesStartIndex = fileContent.IndexOf("\"branches\"");
            if (branchesStartIndex == -1)
            {
                Logger.LogMessage($"branches cant found.");
                return null;
            }
            int startIndex = fileContent.IndexOf('{', branchesStartIndex);
            int endIndex = FindClosingBraceIndex(fileContent, startIndex);
            if (startIndex == -1 || endIndex == -1)
            {
                Logger.LogMessage($"cant parse branches section.");
                return null;
            }
            string branchesSection = fileContent.Substring(startIndex, endIndex - startIndex + 1);

            int aux02StartIndex = branchesSection.IndexOf($"\"{branch}\"");
            if (aux02StartIndex == -1)
            {
                Logger.LogMessage($"minor branch cant found.");
            }
            startIndex = branchesSection.IndexOf('{', aux02StartIndex);
            endIndex = FindClosingBraceIndex(branchesSection, startIndex);
            if (startIndex == -1 || endIndex == -1)
            {
                Logger.LogMessage($"cant parse minor branch section.");
                return null;
            }

            string aux02Section = branchesSection.Substring(startIndex, endIndex - startIndex + 1);

            string buildId = GetPropertyValue(aux02Section, "buildid");
            if (buildId == null)
            {
                Logger.LogMessage($"buildId null.");
                return null;
            }
            else
            {
                return buildId;
            }
        }

        private static string GetPropertyValue(string text, string propertyName)
        {
            string pattern = $"\"{propertyName}\"\\s*\"(.*?)\"";
            Match match = Regex.Match(text, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }

        private static int FindClosingBraceIndex(string text, int startIndex)
        {
            int braceCount = 0;
            for (int i = startIndex; i < text.Length; i++)
            {
                if (text[i] == '{')
                {
                    braceCount++;
                }
                else if (text[i] == '}')
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private static bool IsValidVersion(string version)
        {
            return version != null && version.Length == 8 && int.TryParse(version, out _);
        }
    }
}