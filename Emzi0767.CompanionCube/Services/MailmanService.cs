using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Emzi0767.CompanionCube.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Emzi0767.CompanionCube.Services
{
    /// <summary>
    /// Provides mailman functionality.
    /// </summary>
    public sealed class MailmanService
    {
        private static Regex SourceChannelRegex { get; } = new Regex(@"^\[c:(?<channel>\d+)\]", RegexOptions.Compiled);

        private DiscordClient Discord { get; }

        private MailmanSettings Settings { get; set; } = null;
        private bool IsEnabled { get; set; } = true;
        private DiscordChannel Channel { get; set; } = null;

        public MailmanService(DiscordClient discord)
        {
            this.Discord = discord;
            this.Discord.MessageCreated += this.Discord_MessageCreated;
        }

        public async Task EnableAsync(DatabaseContext db, ulong guildId, ulong channelId)
        {
            var meta = await db.Metadata.FirstOrDefaultAsync(x => x.MetaKey == MailmanSettings.MetaKey);
            this.Settings = new MailmanSettings
            {
                Guild = guildId,
                Channel = channelId
            };
            this.Channel = await this.Discord.GetChannelAsync(this.Settings.Channel);
            var settingsJson = JsonConvert.SerializeObject(this.Settings);
            this.IsEnabled = true;

            if (meta != null)
            {
                meta.MetaValue = settingsJson;
                db.Metadata.Update(meta);
            }
            else
            {
                await db.Metadata.AddAsync(new DatabaseMetadata
                {
                    MetaKey = MailmanSettings.MetaKey,
                    MetaValue = settingsJson
                });
            }

            await db.SaveChangesAsync();
        }

        public async Task DisableAsync(DatabaseContext db)
        {
            this.Settings = null;
            this.IsEnabled = false;
            this.Channel = null;
            var meta = await db.Metadata.FirstOrDefaultAsync(x => x.MetaKey == MailmanSettings.MetaKey);
            if (meta != null)
            {
                db.Metadata.Remove(meta);
                await db.SaveChangesAsync();
            }
        }

        public async Task<bool> SendMessageAsync(DatabaseContext db, DiscordUser author, DiscordChannel source, string message)
        {
            await this.InitializeSettingsAsync(db);
            if (!this.IsEnabled)
                return false;

            var msg = $"[c:{source.Id}] {author.Username}#{author.Discriminator} ({author.Id}):\n\n{message}";
            await this.Channel.SendMessageAsync(msg);
            return true;
        }

        private async Task InitializeSettingsAsync(DatabaseContext db)
        {
            if (!this.IsEnabled || this.Settings != null)
                return;

            var meta = await db.Metadata.FirstOrDefaultAsync(x => x.MetaKey == MailmanSettings.MetaKey);
            if (meta == null)
            {
                this.IsEnabled = false;
                this.Channel = null;
                return;
            }

            this.Settings = JsonConvert.DeserializeObject<MailmanSettings>(meta.MetaValue);
            this.Channel = await this.Discord.GetChannelAsync(this.Settings.Channel);
        }

        private async Task Discord_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (!this.IsEnabled)
                return;

            if (e.Channel.Id != this.Settings.Channel)
                return;

            if (e.Message.ReferencedMessage == null)
                return;

            var refmsg = e.Message.ReferencedMessage;
            if (refmsg.Content == null)
                refmsg = await e.Channel.GetMessageAsync(refmsg.Id);

            var srcmatch = SourceChannelRegex.Match(refmsg.Content);
            if (!srcmatch.Success || !srcmatch.Groups["channel"].Success)
                return;

            var srcraw = srcmatch.Groups["channel"].Value;
            if (!ulong.TryParse(srcraw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var srcid))
                return;

            var src = await this.Discord.GetChannelAsync(srcid);
            await src.SendMessageAsync($"**Emzi:** {e.Message.Content}");
        }
    }
}
