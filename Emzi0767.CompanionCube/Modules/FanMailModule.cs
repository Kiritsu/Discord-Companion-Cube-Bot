// This file is part of Companion Cube project
//
// Copyright 2018 Emzi0767
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Emzi0767.CompanionCube.Attributes;
using Emzi0767.CompanionCube.Services;

namespace Emzi0767.CompanionCube.Modules
{
    [Group("mail")]
    [Description("Message Emzi while he's in self-imposed exile.")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    [Hidden]
    [RequireDirectMessage, NotBlacklisted]
    public sealed class FanMailModule : BaseCommandModule
    {
        private DatabaseContext Database { get; }
        private MailmanService Mailman { get; }

        public FanMailModule(DatabaseContext db, MailmanService mailman)
        {
            this.Database = db;
            this.Mailman = mailman;
        }

        [Command("set")]
        [Description("Sets the mailbox guild and channel to use, and enables the mailbox.")]
        [RequireOwner]
        public async Task SetAsync(CommandContext ctx,
            [Description("Guild to use as mailbox.")] DiscordGuild guild,
            [Description("Channel to use as mailbox.")] DiscordChannel channel)
        {
            if (channel.Guild != guild)
            {
                await ctx.RespondAsync("Must be a channel within the specified guild.");
                return;
            }

            await this.Mailman.EnableAsync(this.Database, guild.Id, channel.Id);
            await ctx.RespondAsync($"Mailman enabled in {guild.Id}::{channel.Id}.");
        }

        [Command("unset")]
        [Description("Disables the mailbox.")]
        [RequireOwner]
        public async Task UnsetAsync(CommandContext ctx)
        {
            await this.Mailman.DisableAsync(this.Database);
            await ctx.RespondAsync("Mailman disabled.");
        }

        [Command("send")]
        [Description("Sends a message to the mailbox.")]
        [Cooldown(3, 900, CooldownBucketType.User)]
        public async Task SendAsync(CommandContext ctx,
            [Description("Contents of the message to send."), RemainingText] string contents)
        {
            if (contents.Length > 1000)
            {
                await ctx.RespondAsync("Must be less than 1000 characters.");
                return;
            }

            if (!await this.Mailman.SendMessageAsync(this.Database, ctx.User, ctx.Channel, contents))
                await ctx.RespondAsync("Message wasn't sent: mailman is not enabled.");
            else
                await ctx.RespondAsync("Message sent. Responses will be delivered via the same DM.");
        }
    }
}
