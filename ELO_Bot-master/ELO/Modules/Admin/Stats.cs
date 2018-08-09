namespace ELO.Modules.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using ELO.Discord.Context;
    using ELO.Discord.Extensions;
    using ELO.Discord.Preconditions;
    
    using global::Discord.Commands;
    using global::Discord.WebSocket;

    [CustomPermissions(DefaultPermissionLevel.Moderators)]
    [Summary("Direct user stats modifications")]
    public class Stats : Base
    {
        [Command("ModifyPoints")]
        [Summary("Add or subtract points from a user")]
        public Task ModifyPointsAsync(SocketGuildUser user, int pointsToAddOrSubtract)
        {
            return ModifyAsync(new List<SocketGuildUser> { user }, ScoreType.point, pointsToAddOrSubtract);
        }

        [Command("ModifyPoints")]
        [Summary("Add or subtract points from the given user(s)")]
        public Task ModifyPointsAsync(int pointsToAddOrSubtract, params SocketGuildUser[] users)
        {
            return ModifyAsync(users.ToList(), ScoreType.point, pointsToAddOrSubtract);
        }

        [Command("SetPoints")]
        [Summary("Set the points of a user")]
        public Task SetPointsAsync(SocketGuildUser user, int points)
        {
            return SetAsync(new List<SocketGuildUser> { user }, ScoreType.point, points);
        }

        [Command("SetPoints")]
        [Summary("Set the points of the given user(s)")]
        public Task SetPointsAsync(int points, params SocketGuildUser[] users)
        {
            return SetAsync(users.ToList(), ScoreType.point, points);
        }

        [Command("ModifyKills")]
        [Summary("Add or subtract kills from a user")]
        public Task ModifyKillsAsync(SocketGuildUser user, int killsToAddOrSubtract)
        {
            return ModifyAsync(new List<SocketGuildUser> { user }, ScoreType.kill, killsToAddOrSubtract);
        }

        [Command("ModifyKills")]
        [Summary("Add or subtract kills from the given user(s)")]
        public Task ModifyKillsAsync(int killsToAddOrSubtract, params SocketGuildUser[] users)
        {
            return ModifyAsync(users.ToList(), ScoreType.kill, killsToAddOrSubtract);
        }

        [Command("SetKills")]
        [Summary("Set the kills of a user")]
        public Task SetKillsAsync(SocketGuildUser user, int kills)
        {
            return SetAsync(new List<SocketGuildUser> { user }, ScoreType.kill, kills);
        }

        [Command("SetKills")]
        [Summary("Set the kills of the given user(s)")]
        public Task SetKillsAsync(int kills, params SocketGuildUser[] users)
        {
            return SetAsync(users.ToList(), ScoreType.kill, kills);
        }

        [Command("ModifyDeaths")]
        [Summary("Add or subtract Deaths from a user")]
        public Task ModifyDeathsAsync(SocketGuildUser user, int deathsToAddOrSubtract)
        {
            return ModifyAsync(new List<SocketGuildUser> { user }, ScoreType.death, deathsToAddOrSubtract);
        }

        [Command("ModifyDeaths")]
        [Summary("Add or subtract Deaths from the given user(s)")]
        public Task ModifyDeathsAsync(int deathsToAddOrSubtract, params SocketGuildUser[] users)
        {
            return ModifyAsync(users.ToList(), ScoreType.death, deathsToAddOrSubtract);
        }

        [Command("SetDeaths")]
        [Summary("Set the Deaths of a user")]
        public Task SetDeathsAsync(SocketGuildUser user, int deaths)
        {
            return SetAsync(new List<SocketGuildUser> { user }, ScoreType.death, deaths);
        }

        [Command("SetDeaths")]
        [Summary("Set the Deaths of the given user(s)")]
        public Task SetDeathsAsync(int deaths, params SocketGuildUser[] users)
        {
            return SetAsync(users.ToList(), ScoreType.death, deaths);
        }

        [Command("ModifyWins")]
        [Summary("Add or subtract Wins from a user")]
        public Task ModifyWinsAsync(SocketGuildUser user, int winsToAddOrSubtract)
        {
            return ModifyAsync(new List<SocketGuildUser> { user }, ScoreType.win, winsToAddOrSubtract);
        }

        [Command("ModifyWins")]
        [Summary("Add or subtract Wins from the given user(s)")]
        public Task ModifyWinsAsync(int winsToAddOrSubtract, params SocketGuildUser[] users)
        {
            return ModifyAsync(users.ToList(), ScoreType.win, winsToAddOrSubtract);
        }

        [Command("SetWins")]
        [Summary("Set the Wins of a user")]
        public Task SetWinsAsync(SocketGuildUser user, int wins)
        {
            return SetAsync(new List<SocketGuildUser> { user }, ScoreType.win, wins);
        }

        [Command("SetWins")]
        [Summary("Set the Wins of the given user(s)")]
        public Task SetWinsAsync(int wins, params SocketGuildUser[] users)
        {
            return SetAsync(users.ToList(), ScoreType.win, wins);
        }

        [Command("ModifyLosses")]
        [Summary("Add or subtract Losses from a user")]
        public Task ModifyLossesAsync(SocketGuildUser user, int lossesToAddOrSubtract)
        {
            return ModifyAsync(new List<SocketGuildUser> { user }, ScoreType.loss, lossesToAddOrSubtract);
        }

        [Command("ModifyLosses")]
        [Summary("Add or subtract Losses from the given user(s)")]
        public Task ModifyLossesAsync(int lossesToAddOrSubtract, params SocketGuildUser[] users)
        {
            return ModifyAsync(users.ToList(), ScoreType.loss, lossesToAddOrSubtract);
        }

        [Command("SetLosses")]
        [Summary("Set the Losses of a user")]
        public Task SetLossesAsync(SocketGuildUser user, int losses)
        {
            return SetAsync(new List<SocketGuildUser> { user }, ScoreType.loss, losses);
        }

        [Command("SetLosses")]
        [Summary("Set the Losses of the given user(s)")]
        public Task SetLossesAsync(int losses, params SocketGuildUser[] users)
        {
            return SetAsync(users.ToList(), ScoreType.loss, losses);
        }

        public Task SetAsync(List<SocketGuildUser> users, ScoreType type, int modifier)
        {
            var sb = new StringBuilder();
            foreach (var user in users)
            {
                var eUser = Context.Server.Users.FirstOrDefault(x => x.UserID == user.Id);
                if (eUser == null)
                {
                    sb.AppendLine("User is not registered");
                    continue;
                }

                int finalValue;
                switch (type)
                {
                    case ScoreType.win:
                        eUser.Stats.Wins = modifier;
                        finalValue = eUser.Stats.Wins;
                        break;
                    case ScoreType.loss:
                        eUser.Stats.Losses = modifier;
                        finalValue = eUser.Stats.Losses;
                        break;
                    case ScoreType.draw:
                        eUser.Stats.Draws = modifier;
                        finalValue = eUser.Stats.Draws;
                        break;
                    case ScoreType.kill:
                        eUser.Stats.Kills = modifier;
                        finalValue = eUser.Stats.Kills;
                        break;
                    case ScoreType.death:
                        eUser.Stats.Deaths = modifier;
                        finalValue = eUser.Stats.Deaths;
                        break;
                    case ScoreType.point:
                        eUser.Stats.Points = modifier;
                        finalValue = eUser.Stats.Points;
                        var nick = Task.Run(() => UserManagement.UserRenameAsync(Context, eUser));
                        var role = Task.Run(() => UserManagement.UpdateUserRanksAsync(Context, eUser));
                        break;
                    default:
                        throw new InvalidOperationException("Unable to modify stats with provided type");
                }

                sb.AppendLine($"{user.Mention} {type}'s set to: {finalValue}");
            }
            Context.Server.Save();
            return SimpleEmbedAsync(sb.ToString());
        }

        public Task ModifyAsync(List<SocketGuildUser> users, ScoreType type, int modifier)
        {
            var sb = new StringBuilder();
            foreach (var user in users)
            {
                var eUser = Context.Server.Users.FirstOrDefault(x => x.UserID == user.Id);
                if (eUser == null)
                {
                    sb.AppendLine("User is not registered");
                    continue;
                }

                int finalValue;
                switch (type)
                {
                    case ScoreType.win:
                        eUser.Stats.Wins += modifier;
                        finalValue = eUser.Stats.Wins;
                        break;
                    case ScoreType.loss:
                        eUser.Stats.Losses += modifier;
                        finalValue = eUser.Stats.Losses;
                        break;
                    case ScoreType.draw:
                        eUser.Stats.Draws += modifier;
                        finalValue = eUser.Stats.Draws;
                        break;
                    case ScoreType.kill:
                        eUser.Stats.Kills += modifier;
                        finalValue = eUser.Stats.Kills;
                        break;
                    case ScoreType.death:
                        eUser.Stats.Deaths += modifier;
                        finalValue = eUser.Stats.Deaths;
                        break;
                    case ScoreType.point:
                        eUser.Stats.Points += modifier;
                        finalValue = eUser.Stats.Points;
                        var nick = Task.Run(() => UserManagement.UserRenameAsync(Context, eUser));
                        var role = Task.Run(() => UserManagement.UpdateUserRanksAsync(Context, eUser));
                        break;
                    default:
                        throw new InvalidOperationException("Unable to modify stats with provided type");
                }

                sb.AppendLine($"{user.Mention} {type}'s modified: {finalValue}");
            }

            Context.Server.Save();
            return SimpleEmbedAsync(sb.ToString());
        }

        public enum ScoreType
        {
            win,
            loss,
            draw,
            kill,
            death,
            point
        }
    }
}
