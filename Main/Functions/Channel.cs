using Discord;
using Discord.WebSocket;
using RustUpdateNotes.GlobalClass;
using RustUpdateNotes.LoggerClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RustUpdateNotes.ChannelClass
{
    public static class Channel
    {
        public static async Task Channel_Runner()
        {
            while (true)
            {
                var maintask = CheckChannels();
                var controltask = Task.Delay(TimeSpan.FromMinutes(5));
                var completedTask = await Task.WhenAny(maintask, controltask);
                if (completedTask == maintask)
                {
                    Global.Channel_Succes++;
                }
                else
                {
                    Global.Channel_Fail++;
                    Logger.LogMessage($"InitChannelRunner Timeout (5 minute)");
                    await Logger.DiscordMessage($"InitChannelRunner Timeout (5 minute)", true);
                }
                await Task.Delay(TimeSpan.FromHours(1));
            }
        }
        public static async Task CheckChannels()
        {
            try
            {
                var guildlist = Global.Client.Guilds.ToList();
                foreach (var CurrentGuild in guildlist)
                {
                    string categoryName = "rust-güncelleme┃🔔";
                    string[] channelNames = { "güncelleme-notları┃📋", "güncelleme-tarihi┃📅", "güncelleme-takipçisi┃💻", "haftalık-mağaza┃🛒", "commits┃📝" };
                    if (!await Logger.CheckBotPerms(CurrentGuild))
                    {
                        Logger.LogMessage($"Genel Yetki Yetersizliği - Kanal | Guild: {CurrentGuild.Name}");
                        continue;
                    }
                    var categoryCheck = CurrentGuild.CategoryChannels.FirstOrDefault(c => c.Name == categoryName);
                    ICategoryChannel categoryCurrent;
                    if (categoryCheck != null)
                    {
                        Logger.LogMessage($"Category already created. | Guild: {CurrentGuild} | Category: {categoryCheck.Id}");
                        categoryCurrent = categoryCheck;
                    }
                    else
                    {
                        categoryCurrent = await CurrentGuild.CreateCategoryChannelAsync(categoryName);
                        Logger.LogMessage($"Category created. | Guild: {CurrentGuild} - Category: {categoryCurrent.Id}");
                    }
                    List<ulong> UpdateNoteChannelsL = new List<ulong>();
                    List<ulong> UpdateDateChannelsL = new List<ulong>();
                    List<ulong> UpdateTrackerChannelsL = new List<ulong>();
                    List<ulong> StoreCheckerChannelsL = new List<ulong>();
                    List<ulong> CommitFollowerChannelsL = new List<ulong>();
                    for (int i = 0; i < channelNames.Length; i++)
                    {
                        string channelName = channelNames[i];
                        if (CurrentGuild.Id == Global.MainDiscordID && (channelName == "güncelleme-notları┃📋" || channelName == "güncelleme-tarihi┃📅"))
                        {
                            continue;
                        }
                        var channelCheck = CurrentGuild.TextChannels.FirstOrDefault(c => c.Name == channelName && c.CategoryId == categoryCurrent.Id);
                        if (channelCheck != null)
                        {
                            Logger.LogMessage($"Channel already created: {channelCheck.Id}");
                            switch (i)
                            {
                                case 0:
                                    UpdateNoteChannelsL.Add(channelCheck.Id);
                                    break;

                                case 1:
                                    UpdateDateChannelsL.Add(channelCheck.Id);
                                    break;

                                case 2:
                                    UpdateTrackerChannelsL.Add(channelCheck.Id);
                                    break;

                                case 3:
                                    StoreCheckerChannelsL.Add(channelCheck.Id);
                                    break;

                                case 4:
                                    CommitFollowerChannelsL.Add(channelCheck.Id);
                                    break;
                            }
                        }
                        else
                        {
                            var newChannel = await CurrentGuild.CreateTextChannelAsync(channelName, x => x.CategoryId = categoryCurrent.Id);
                            Logger.LogMessage($"New Channel created: {newChannel.Id}");
                            switch (i)
                            {
                                case 0:
                                    UpdateNoteChannelsL.Add(newChannel.Id);
                                    try
                                    {
                                        await newChannel.SendMessageAsync("**Güncelleme Notları** kanalı başarıyla oluşturuldu.\nGüncelleme notları bu kanalda paylaşılacaktır.");
                                        var announcementChannel = Global.Client.GetChannel(1223058873573969920) as SocketNewsChannel;
                                        var targetChannel = Global.Client.GetChannel(newChannel.Id) as SocketTextChannel;
                                        var followChannel = await announcementChannel.FollowAnnouncementChannelAsync(targetChannel);
                                    }
                                    catch (Exception)
                                    {
                                        Logger.LogMessage($"Güncelleme notları takip edilemedi. | {CurrentGuild.Name}");
                                        await Logger.DiscordMessage($"Güncelleme notları takip edilemedi. | {CurrentGuild.Name}");
                                        await newChannel.SendMessageAsync("Güncelleme notları bir sorundan dolayı takip edilemedi.\nhttps://discord.com/channels/1223037877911556107/1223058873573969920 buradan kendiniz **Takip Et** diyerek ekleyebilirsiniz.");
                                    }
                                    break;

                                case 1:
                                    UpdateDateChannelsL.Add(newChannel.Id);
                                    await newChannel.SendMessageAsync("**Güncelleme Tarihi** kanalı başarıyla oluşturuldu.\nGüncelleme bilgisi saatlik olarak güncellenmektir. Güncelleme bilgisi 1 saat içinde bu kanala eklenecektir.");
                                    break;

                                case 2:
                                    UpdateTrackerChannelsL.Add(newChannel.Id);
                                    await newChannel.SendMessageAsync("**Güncelleme Takipçisi** kanalı başarıyla oluşturuldu.\nSunucu veya Oyuncu taraflı bir güncelleme tespit edildiğinde bu kanalda bildirim gelecektir.");
                                    break;

                                case 3:
                                    StoreCheckerChannelsL.Add(newChannel.Id);
                                    await newChannel.SendMessageAsync("**Haftalık Mağaza** kanalı başarıyla oluşturuldu.\nHer hafta mağaza yenilediğinde gelen skinlerin görsellerini ve fiyatlarını bu kanalda görebilirsiniz.");
                                    break;

                                case 4:
                                    CommitFollowerChannelsL.Add(newChannel.Id);
                                    await newChannel.SendMessageAsync("**Commits** kanalı başarıyla oluşturuldu.\nYeni bir commit tespit edildiğinde bu kanalda görebilirsiniz.");
                                    break;
                            }
                        }
                    }
                    Global.UpdateNoteChannels[CurrentGuild.Id] = UpdateNoteChannelsL;
                    Global.UpdateDateChannels[CurrentGuild.Id] = UpdateDateChannelsL;
                    Global.UpdateTrackerChannels[CurrentGuild.Id] = UpdateTrackerChannelsL;
                    Global.StoreCheckerChannels[CurrentGuild.Id] = StoreCheckerChannelsL;
                    Global.CommitFollowerChannels[CurrentGuild.Id] = CommitFollowerChannelsL;
                }
            }
            catch (Exception ex)
            {
                Logger.LogMessage($"Error - Channel: {ex}");
                await Logger.DiscordMessage($"Error - Channel: {ex}", true);
            }
        }
    }
}