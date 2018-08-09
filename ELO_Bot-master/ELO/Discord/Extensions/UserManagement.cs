namespace ELO.Discord.Extensions
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Discord;

    using ELO.Discord.Context;
    using ELO.Handlers;
    using ELO.Models;

    public class UserManagement
    {
        public static GuildModel.Rank MaxRole(Context context, GuildModel.User user = null)
        {
            try
            {
                // Get the highest point threshold based of the users score
                // This is the highest rank points that is lower than the user's points
                var maxRankPoints = context.Server.Ranks.Where(x => x.Threshold <= (user?.Stats.Points ?? context.Elo.User.Stats.Points)).Max(x => x.Threshold);

                // Get the first role that matches this threshold
                var maxRank = context.Server.Ranks.First(x => x.Threshold == maxRankPoints);

                // Ensure that the returned role has a score. If not, use the default server modifier
                maxRank.LossModifier = maxRank.LossModifier == 0 ? context.Server.Settings.Registration.DefaultLossModifier : maxRank.LossModifier;
                maxRank.WinModifier = maxRank.WinModifier == 0 ? context.Server.Settings.Registration.DefaultWinModifier : maxRank.WinModifier;
                return maxRank;
            }
            catch
            {
                return new GuildModel.Rank
                {
                    LossModifier = context.Server.Settings.Registration.DefaultLossModifier,
                    WinModifier = context.Server.Settings.Registration.DefaultWinModifier,
                    RoleID = 0,
                    Threshold = 0,
                    IsDefault = true
                };
            }
        }

        public static async Task UpdateUserRanksAsync(Context context, GuildModel.User user = null)
        {
            try
            {
                if (user == null)
                {
                    user = context.Elo.User;
                }

                var maxRank = MaxRole(context, user);

                var serverRole = context.Guild.GetRole(maxRank.RoleID);
                if (serverRole != null)
                {
                    try
                    {
                        var gUser = context.Guild.GetUser(user.UserID);
                        if (gUser == null)
                        {
                            return;
                        }

                        if (gUser.Roles.Any(x => x.Id == serverRole.Id))
                        {
                            // Return if the user already has the role
                            return;
                        }

                        var rolesToRemove = gUser.Roles.Where(x => context.Server.Ranks.Any(r => r.RoleID == x.Id) && x.Id != serverRole.Id);

                        await gUser.RemoveRolesAsync(rolesToRemove);

                        await gUser.AddRoleAsync(serverRole);
                    }
                    catch (Exception e)
                    {
                        // Role Unavailable OR user unable to receive role due to permissions
                        LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
                    }
                }
            }
            catch (Exception e)
            {
                // No applicable roles
                LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
            }
        }

        public static async Task UserRenameAsync(Context context, GuildModel.User user = null)
        {
            if (user == null)
            {
                user = context.Elo.User;
            }

            var rename = context.Server.Settings.Registration.NameFormat.Replace("{score}", user.Stats.Points.ToString()).Replace("{username}", user.Username);

            try
            {
                var gUser = context.Guild.GetUser(user.UserID);
                if (gUser == null)
                {
                    return;
                }

                if (gUser.Nickname == rename)
                {
                    return;
                }

                if (gUser.Id == context.Guild.OwnerId)
                {
                    return;
                }

                if (gUser.Roles.Max(x => x.Position) >= context.Guild.CurrentUser.Roles.Max(x => x.Position))
                {
                    return;
                }

                await gUser.ModifyAsync(x => x.Nickname = rename);
            }
            catch (Exception e)
            {
                // Error renaming user (permissions above bot.)
                LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
            }
        }

        public static async Task RegisterAsync(Context con, GuildModel server, IUser user, string name)
        {
            if (name.Length > 20)
            {
                throw new Exception("Name must be equal to or less than 20 characters long");
            }

            var newUser = new GuildModel.User
            {
                UserID = user.Id,
                Username = name,
                Stats = new GuildModel.User.Score
                {
                    Points = server.Settings.Registration.RegistrationBonus
                }
            };

            string userSelfUpdate = null;

            var eloUser = server.Users.FirstOrDefault(x => x.UserID == user.Id);

            if (eloUser != null)
            {
                if (!server.Settings.Registration.AllowMultiRegistration)
                {
                    throw new Exception("You are not allowed to re-register");
                }

                userSelfUpdate = $"{eloUser.Username} => {name}";
                newUser.Stats = eloUser.Stats;
                newUser.Banned = eloUser.Banned;
                server.Users.Remove(eloUser);
            }
            else
            {
                if (server.Users.Count > 20 && (server.Settings.Premium.Expiry < DateTime.UtcNow || !server.Settings.Premium.IsPremium))
                {
                    throw new Exception($"Premium is required to register more than 20 users. {ConfigModel.Load().PurchaseLink}\n"
                                        + $"Get the server owner to purchase a key and use the command `{con.Prefix}Premium <key>`");
                }
            }

            server.Users.Add(newUser);

            await RegisterUpdatesAsync(con, server, newUser, user);

            server.Save();

            if (userSelfUpdate == null)
            {
                await con.Channel.SendMessageAsync("", false, new EmbedBuilder { Title = $"Success, Registered as {name}", Description = server.Settings.Registration.Message }.Build());
            }
            else
            {
                await con.Channel.SendMessageAsync("", false, new EmbedBuilder
                                                                  {
                                                                      Description = "You have re-registered.\n" + 
                                                                                   $"Name: {userSelfUpdate}\n" + 
                                                                                   "Role's have been updated\n" + 
                                                                                   "Stats have been saved."
                                                                  }.Build());
            }

            if (con.Guild.GetRole(server.Ranks.FirstOrDefault(x => x.IsDefault)?.RoleID ?? 0) is IRole RegRole)
            {
                try
                {
                    await (user as IGuildUser).AddRoleAsync(RegRole);
                }
                catch (Exception e)
                {
                    LogHandler.LogMessage(e.ToString(), LogSeverity.Error);
                }
            }
        }

        public static async Task RegisterUpdatesAsync(Context con, GuildModel server, GuildModel.User newUser, IUser user)
        {
            if (newUser.Stats.Points == server.Settings.Registration.RegistrationBonus && server.Ranks.FirstOrDefault(x => x.IsDefault)?.RoleID != null)
            {
                var registerRole = con.Guild.GetRole(server.Ranks.FirstOrDefault(x => x.IsDefault).RoleID);
                if (registerRole != null)
                {
                    try
                    {
                        await (user as IGuildUser).AddRoleAsync(registerRole);
                    }
                    catch
                    {
                        // user Permissions above the bot.
                    }
                }
            }
            else
            {
                await UpdateUserRanksAsync(con, newUser);
            }

            await UserRenameAsync(con, newUser);
        }
    }
}