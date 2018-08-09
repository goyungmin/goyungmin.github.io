namespace ELO.Modules.Admin
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using ELO.Discord.Context;
    using ELO.Discord.Preconditions;
    using ELO.Models;

    using global::Discord;
    using global::Discord.Commands;

    [CustomPermissions(DefaultPermissionLevel.Administrators)]
    [Summary("Rank info and management")]
    public class Rank : Base
    {
        [Command("RankInfo", RunMode = RunMode.Async)]
        [Summary("Display all ranks + Default")]
        public Task ViewRanksAsync()
        {

            var rankList = Context.Server.Ranks.OrderByDescending(r => r.Threshold).Select(
                r =>
                    {
                        var name = Context.Guild.GetRole(r.RoleID)?.Mention ?? $"[{r.RoleID}]";
                        var scoreInfo = $"{r.Threshold.ToString().PadRight(10)} +{(r.WinModifier == 0 ? Context.Server.Settings.Registration.DefaultWinModifier : r.WinModifier).ToString().PadRight(10)} -{(r.LossModifier == 0 ? Context.Server.Settings.Registration.DefaultLossModifier : r.LossModifier).ToString().PadRight(10)}\u200B";
                        return $"`{scoreInfo}` - {name}{(r.IsDefault ? " :small_orange_diamond:" : "")}";
                    }).ToList();

            return SimpleEmbedAsync($"`Threshold  +Win        -Lose      \u200B `\n{string.Join("\n", rankList)}");
        }

        [Command("AddRank")]
        [Summary("Add a rank users can achieve by getting a certain amount of points")]
        public Task AddRankAsync(int points, IRole role)
        {
            return AddRankAsync(role, points);
        }

        [Command("AddRank")]
        [Summary("Add a rank users can achieve by getting a certain amount of points")]
        public Task AddRankAsync(IRole role, int points)
        {
            if (Context.Server.Ranks.Any(x => x.RoleID == role.Id))
            {
                throw new Exception("This is already a rank");
            }

            var rank = new GuildModel.Rank
            {
                IsDefault = false,
                WinModifier = Context.Server.Settings.Registration.DefaultWinModifier,
                LossModifier = Context.Server.Settings.Registration.DefaultLossModifier,
                RoleID = role.Id,
                Threshold = points
            };
            Context.Server.Ranks.Add(rank);
            Context.Server.Save();
            return SimpleEmbedAsync("Rank added.");
        }

        [Command("DelRank")]
        [Summary("Delete a server rank")]
        public Task DelRankAsync(IRole role)
        {
            return DelRankAsync(role.Id);
        }

        [Command("DelRank")]
        [Summary("Delete a rank via role ID")]
        public Task DelRankAsync(ulong roleId)
        {
            if (Context.Server.Ranks.All(x => x.RoleID != roleId))
            {
                throw new Exception("This is not a rank");
            }

            var rank = Context.Server.Ranks.FirstOrDefault(x => x.RoleID == roleId);
            if (rank.IsDefault)
            {
                throw new Exception("You cannot delete the default rank");
            }

            Context.Server.Ranks.Remove(rank);
            Context.Server.Save();
            return SimpleEmbedAsync("Rank Removed.");
        }

        [Command("WinModifier")]
        [Summary("Set the amount of points a role receives upon winning with a specific rank")]
        public Task WinModifierAsync(IRole role, int points)
        {
            if (Context.Server.Ranks.All(x => x.RoleID != role.Id))
            {
                throw new Exception("This is not a rank");
            }

            if (points <= 0)
            {
                throw new Exception("Point modifier must be a positive integer");
            }

            var rank = Context.Server.Ranks.FirstOrDefault(x => x.RoleID == role.Id);
            rank.WinModifier = points;
            Context.Server.Save();
            return SimpleEmbedAsync("Rank Win Modifier Updated.");
        }

        [Command("LossModifier")]
        [Summary("Set the amount of points a role loses upon losing with a specific rank")]
        public Task LossModifierAsync(IRole role, int points)
        {
            if (Context.Server.Ranks.All(x => x.RoleID != role.Id))
            {
                throw new Exception("This is not a rank");
            }

            points = Math.Abs(points);

            if (points == 0)
            {
                throw new Exception("Point modifier must be an integer");
            }

            var rank = Context.Server.Ranks.FirstOrDefault(x => x.RoleID == role.Id);
            rank.LossModifier = points;
            Context.Server.Save();
            return SimpleEmbedAsync("Rank Loss Modifier Updated.");
        }
    }
}