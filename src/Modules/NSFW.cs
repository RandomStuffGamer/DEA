﻿using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using DEA.Database.Repository;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Xml;
using DEA.Resources;

namespace DEA.Modules
{
    public class NSFW : DEAModule
    {
        protected override void BeforeExecute()
        {
            InitializeData();
        }

        [Command("ChangeNSFWSettings")]
        [Require(Attributes.Admin)]
        [Summary("Enables/disables NSFW commands in your server.")]
        public async Task ChangeNSFWSettings()
        {
            switch (DbGuild.Nsfw)
            {
                case true:
                    GuildRepository.Modify(DEABot.GuildUpdateBuilder.Set(x => x.Nsfw, false), Context.Guild.Id);
                    await Reply($"You have successfully disabled NSFW commands!");
                    break;
                case false:
                    GuildRepository.Modify(DEABot.GuildUpdateBuilder.Set(x => x.Nsfw, true), Context.Guild.Id);
                    await Reply($"You have successfully enabled NSFW commands!");
                    break;
            }
        }

        [Command("SetNSFWChannel")]
        [Require(Attributes.Admin)]
        [Summary("Sets a specific channel for all NSFW commands.")]
        public async Task SetNSFWChannel(ITextChannel nsfwChannel)
        {
            GuildRepository.Modify(DEABot.GuildUpdateBuilder.Set(x => x.NsfwId, nsfwChannel.Id), Context.Guild.Id);
            var nsfwRole = Context.Guild.GetRole(DbGuild.NsfwRoleId);
            if (nsfwRole != null && Context.Guild.CurrentUser.GuildPermissions.Administrator)
            {
                await nsfwChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions().Modify(null, null, null, PermValue.Deny));
                await nsfwChannel.AddPermissionOverwriteAsync(nsfwRole, new OverwritePermissions().Modify(null, null, null, PermValue.Allow));
            }
            await Reply($"You have successfully set the NSFW channel to {nsfwChannel.Mention}.");
        }

        [Command("SetNSFWRole")]
        [Require(Attributes.Admin)]
        [Summary("Only allow users with a specific role to use NSFW commands.")]
        public async Task SetNSFWRole(IRole nsfwRole)
        {
            if (nsfwRole.Position > Context.Guild.CurrentUser.Roles.OrderByDescending(x => x.Position).First().Position)
                Error("You may not set the NSFW role to a role that is higher in hierarchy than DEA's highest role.");
            GuildRepository.Modify(DEABot.GuildUpdateBuilder.Set(x => x.NsfwRoleId, nsfwRole.Id), Context.Guild.Id);
            var nsfwChannel = Context.Guild.GetChannel(DbGuild.NsfwId);
            if (nsfwChannel != null && Context.Guild.CurrentUser.GuildPermissions.Administrator)
            {
                await nsfwChannel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions().Modify(null, null, null, PermValue.Deny));
                await nsfwChannel.AddPermissionOverwriteAsync(nsfwRole, new OverwritePermissions().Modify(null, null, null, PermValue.Allow));
            }
            await Reply($"You have successfully set the NSFW role to {nsfwRole.Mention}.");
        }

        [Command("NSFW")]
        [Alias("EnableNSFW", "DisableNSFW")]
        [Summary("Enables/disables the user's ability to use NSFW commands.")]
        [RequireBotPermission(GuildPermission.ManageRoles)]
        public async Task JoinNSFW()
        {
            var NsfwRole = Context.Guild.GetRole(DbGuild.NsfwRoleId);
            if (NsfwRole == null) Error("Everyone will always be able to use NSFW commands since there has been no NSFW role that has been set.\n" +
                                                     $"In order to change this, an administrator may use the `{Prefix}SetNSFWRole` command.");
            if ((Context.User as IGuildUser).RoleIds.Any(x => x == DbGuild.NsfwRoleId))
            {
                await (Context.User as IGuildUser).RemoveRoleAsync(NsfwRole);
                await Reply($"You have successfully disabled your ability to use NSFW commands.");
            }
            else
            {
                await (Context.User as IGuildUser).AddRoleAsync(NsfwRole);
                await Reply($"You have successfully enabled your ability to use NSFW commands.");
            }
        }

        [Command("Tits")]
        [Alias("titties", "tities", "boobs", "boob")]
        [Require(Attributes.Nsfw)]
        [Summary("Motorboat that shit.")]
        public async Task Tits()
        {
            using (var http = new HttpClient())
            {
                var obj = JArray.Parse(await http.GetStringAsync($"http://api.oboobs.ru/boobs/{new Random().Next(0, 10330)}").ConfigureAwait(false))[0];
                await ReplyAsync($"http://media.oboobs.ru/{obj["preview"]}").ConfigureAwait(false);
            }
        }

        [Command("Ass")]
        [Alias("butt", "butts", "booty")]
        [Require(Attributes.Nsfw)]
        [Summary("Sauce me some booty how about that.")]
        public async Task Ass()
        {
            using (var http = new HttpClient())
            {
                var obj = JArray.Parse(await http.GetStringAsync($"http://api.obutts.ru/butts/{new Random().Next(0, 4335)}").ConfigureAwait(false))[0];
                await ReplyAsync($"http://media.obutts.ru/{obj["preview"]}").ConfigureAwait(false);
            }
        }

        [Command("Hentai")]
        [Require(Attributes.Nsfw)]
        [Summary("The real shit goes down with custom hentai tags.")]
        public async Task Gelbooru([Remainder] string tag = "")
        {
            tag = tag?.Replace(" ", "_");
            using (var http = new HttpClient())
            {
                var data = await http.GetStreamAsync($"http://gelbooru.com/index.php?page=dapi&s=post&q=index&limit=100&tags={tag}").ConfigureAwait(false);
                var doc = new XmlDocument();
                doc.Load(data);

                var node = doc.LastChild.ChildNodes[new Random().Next(0, doc.LastChild.ChildNodes.Count)];
                if (node == null) Error("No result found.");

                var url = node.Attributes["file_url"].Value;

                if (!url.StartsWith("http"))
                    url = "https:" + url;
                await ReplyAsync(url).ConfigureAwait(false);
            }
        }
    }
}
