using Discord;
using HtmlAgilityPack;
using RustUpdateNotes.GlobalClass;
using RustUpdateNotes.LoggerClass;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RustUpdateNotes.SkinClass
{
    public static class Skin
    {
        private static HashSet<string> storedSkins = new HashSet<string>();

        private static readonly string skinApiUrl = "https://store.steampowered.com/itemstore/252490/browse/?filter=Limited";

        public static async Task Skin_Runner()
        {
            while (true)
            {
                var maintask = CheckSkin();
                var controltask = Task.Delay(TimeSpan.FromMinutes(5));
                var completedTask = await Task.WhenAny(maintask, controltask);
                if (completedTask != maintask)
                {
                    Logger.LogMessage($"SkinRunner Timeout (5 minute)");
                    await Logger.DiscordMessage($"SkinRunner Timeout (5 minute)", true);
                }
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

       

        private static readonly List<string> userAgents = new List<string>
    {
      "Mozilla/5.0 (Linux x86_64; rv:128.0) Gecko/20100101 Firefox/128.0",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.2903.63",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 14.7; rv:128.0) Gecko/20100101 Firefox/128.0",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 14.7; rv:132.0) Gecko/20100101 Firefox/132.0",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_7_1) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4.1 Safari/605.1.15",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.2903.63",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:128.0) Gecko/20100101 Firefox/128.0",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:132.0) Gecko/20100101 Firefox/132.0",
    "Mozilla/5.0 (X11; Fedora; Linux x86_64; rv:128.0) Gecko/20100101 Firefox/128.0",
    "Mozilla/5.0 (X11; Fedora; Linux x86_64; rv:132.0) Gecko/20100101 Firefox/132.0",
    "Mozilla/5.0 (X11; Linux i686; rv:128.0) Gecko/20100101 Firefox/128.0",
    "Mozilla/5.0 (X11; Linux i686; rv:132.0) Gecko/20100101 Firefox/132.0",
    "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
    "Mozilla/5.0 (X11; Linux x86_64; rv:132.0) Gecko/20100101 Firefox/132.0",
    "Mozilla/5.0 (X11; Ubuntu; Linux i686; rv:128.0) Gecko/20100101 Firefox/128.0",
    "Mozilla/5.0 (X11; Ubuntu; Linux i686; rv:132.0) Gecko/20100101 Firefox/132.0",
    "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:128.0) Gecko/20100101 Firefox/128.0",
    "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:132.0) Gecko/20100101 Firefox/132.0"
    };

        private static int proxyIndex = 0;
        private static int agentIndex = 0;
       
        private static string GetNextHeader()
        {
            var agent = userAgents[agentIndex];
            agentIndex = (agentIndex + 1) % userAgents.Count;
            return agent;
        }

        public static async Task CheckSkin()
        {
            try
            {

                var cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(90));

                var handler = new HttpClientHandler
                {
                    UseCookies = true,
                    CookieContainer = new CookieContainer()
                };

                using (var httpClient = new HttpClient(handler))
                {
                    httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                    httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.5");
                    httpClient.DefaultRequestHeaders.Referrer = new Uri("https://www.google.com/");
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(GetNextHeader());
                    handler.CookieContainer.Add(new Uri("https://www.google.com"), new Cookie("session", "Mavi"));
                    var response_main = await httpClient.GetAsync(skinApiUrl, cancellationTokenSource.Token);
                    if (!response_main.IsSuccessStatusCode)
                    {
                        Logger.LogMessage($"response_main null.");
                        await Logger.DiscordMessage($"response_main null.", true);
                        return;
                    }

                    var response = await response_main.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(response))
                    {
                        Logger.LogMessage($"response null.");
                        await Logger.DiscordMessage($"response null.", true);
                        return;
                    }

                    List<SkinItem> skinData = ParseSkins(response);
                    if (skinData == null || skinData.Count == 0)
                    {
                        Logger.LogMessage($"skinData null. (Skinler güncelleniyor olabilir.)");
                        await Logger.DiscordMessage($"skinData null. (Skinler güncelleniyor olabilir.)");
                        return;
                    }

                    var newSkins = skinData.Select(Skin => Skin.Name).ToList();
                    
                    foreach (var item in skinData)
                    {
                        var pricetx = Convert.ToDouble(item.Price);
                        Logger.LogMessage($"{item.Name} | {pricetx.ToString("N2")} TL | {item.Image}");
                    }

                    if (storedSkins.Count == 0)
                    {
                        storedSkins.UnionWith(newSkins);
                        return;
                    }

                    var differences = skinData.Where(Skin => newSkins.Contains(Skin.Name) && !storedSkins.Contains(Skin.Name)).ToList();
                    if (differences.Any())
                    {
                        var ourimage = CreateBigImage(skinData);
                        ourimage.Save("skinimage.png", System.Drawing.Imaging.ImageFormat.Png);
                        if (ourimage == null)
                        {
                            Logger.LogMessage($"ourimage null.");
                            await Logger.DiscordMessage($"ourimage null.", true);
                            return;
                        }

                        var skincount = skinData.Count;
                        float totalcost = 0;
                        foreach (var skin in skinData)
                        {
                            float price = float.Parse(skin.Price.Replace("$", ""), CultureInfo.InvariantCulture);
                            totalcost += price;
                        }
                        Logger.LogMessage($"Mağaza Yenilendi --> {skincount} yeni kostüm. Toplam Kostüm Değeri: {totalcost}");

                        EmbedBuilder embedBuildformain = new EmbedBuilder()
                        .WithTitle(":bell: MAĞAZA YENİLENDİ! | " + DateTime.Now.ToShortDateString() + " :bell:")
                        .WithColor(Discord.Color.Blue)
                        .WithDescription($"**{skincount}** yeni kostüm mağazaye eklendi.\n\n Toplam Kostüm Değeri: **₺{totalcost.ToString("N2")}**\n\nKur 35TL olarak sabit alınmıştır, küçük farklar olabilir.")
                        .WithUrl("https://store.steampowered.com/itemstore/252490/");

                        var guildlist = Global.StoreCheckerChannels.Keys.ToList();
                        foreach (var guildId in guildlist)
                        {
                            var guild = Global.Client.GetGuild(guildId);
                            if (guild == null)
                            {
                                continue;
                            }
                            var channelIds = Global.StoreCheckerChannels[guildId].ToList();
                            foreach (var channelId in channelIds)
                            {
                                var channel = guild.GetTextChannel(channelId);
                                if (channel == null)
                                {
                                    continue;
                                }
                                if (!await Logger.CheckBotPerms(guild) || !await Logger.CheckChannelPerms(channel))
                                {
                                    Logger.LogMessage($"Mağaza Kanalı Yetki Yetersizliği | Guild: {guild.Name}");
                                    continue;
                                };
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
                            var guidlist = Global.StoreCheckerChannels.Keys.ToList();
                            foreach (var guildId in guidlist)
                            {
                                var guild = Global.Client.GetGuild(guildId);
                                if (guild == null)
                                {
                                    continue;
                                }
                                var channelIds = Global.StoreCheckerChannels[guildId].ToList();
                                foreach (var channelId in channelIds)
                                {
                                    var channel = guild.GetTextChannel(channelId);
                                    if (channel == null)
                                    {
                                        continue;
                                    }
                                    using (var memoryStream = new MemoryStream(imageData))
                                    {
                                        if (!await Logger.CheckBotPerms(guild) || !await Logger.CheckChannelPerms(channel))
                                        {
                                            Logger.LogMessage($"Mağaza Kanalı Yetki Yetersizliği (Resim) | Guild: {guild.Name}");
                                            continue;
                                        };
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
            }
            catch (ArgumentException)
            {
                Logger.LogMessage($"ArgumentException - New Skins...");
                await Logger.DiscordMessage($"ArgumentException - New Skins...");
            }
            catch (Exception ex)
            {
                Logger.LogMessage($"Error - Skin: {ex}");
                await Logger.DiscordMessage($"Error - Skin: {ex}", true);
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

                            var tprice = Convert.ToDouble(skinPrices[index]);
                            var skinname = skinNames[index];
                            var skinprice = "₺" + tprice.ToString("N2");

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
                    if (x >= imageSize)
                    {
                        x = 0;
                        y++;
                    }
                    if (index >= imageSize * imageSize)
                    {
                        break;
                    }
                }
            }

            return combinedImage;
        }

        private static List<SkinItem> ParseSkins(string html)
        {
            List<SkinItem> skinItems = new List<SkinItem>();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            var storeItems = doc.DocumentNode.SelectNodes("//div[contains(@class, 'item_def_grid_item')]");
            if (storeItems == null)
            {
                return null;
            }
            for (int i = 0; i < storeItems.Count; i++)
            {
                var storeItem = storeItems[i];
                SkinItem skinItem = new SkinItem();
                HtmlNode nameNode = storeItem.SelectSingleNode(".//div[contains(@class, 'item_def_name')]/a");
                HtmlNode priceNode = storeItem.SelectSingleNode(".//div[contains(@class, 'item_def_price')]");
                HtmlNode itemImageNode = storeItem.SelectSingleNode(".//div[contains(@class, 'item_def_icon_container')]/a/img");
                if (nameNode != null)
                {
                    skinItem.Name = nameNode.InnerText.Trim();
                }
                if (priceNode != null)
                {
                    var st1_price = priceNode.InnerText.Trim();
                    st1_price = Regex.Replace(st1_price, @"[^0-9,.]", "");
                    if (decimal.TryParse(st1_price, out decimal price))
                    {
                        skinItem.Price = ((price * 35)/100).ToString();
                    }
                    else
                    {
                        skinItem.Price = "0";
                    }
                }
                if (itemImageNode != null)
                {
                    skinItem.Image = itemImageNode.GetAttributeValue("src", string.Empty);
                }
                skinItems.Add(skinItem);
            }
            return skinItems;
        }

        private class SkinItem
        {
            public string Name { get; set; }
            public string Price { get; set; }
            public string Image { get; set; }
        }
    }
}