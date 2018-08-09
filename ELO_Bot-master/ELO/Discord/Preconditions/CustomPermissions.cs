namespace ELO.Discord.Preconditions
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using ELO.Discord.Extensions;
    using ELO.Handlers;
    using ELO.Models;

    using global::Discord;
    using global::Discord.Commands;

    using Microsoft.Extensions.DependencyInjection;

    public enum DefaultPermissionLevel
    {
        AllUsers,
        Registered,
        Moderators,
        Administrators,
        ServerOwner,
        BotOwner
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class CustomPermissions : PreconditionAttribute
    {
        private DefaultPermissionLevel defaultPermissionLevel;

        public CustomPermissions(DefaultPermissionLevel defaultPermission)
        {
            defaultPermissionLevel = defaultPermission;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext iContext, CommandInfo command, IServiceProvider services)
        {
            var context = iContext as SocketCommandContext;
            if (context.Channel is IDMChannel)
            {
                return Task.FromResult(PreconditionResult.FromError("This is a Guild command"));
            }

            var server = services.GetRequiredService<DatabaseHandler>().Execute<GuildModel>(DatabaseHandler.Operation.LOAD, null, context.Guild.Id.ToString());

            var resultInfo = new AccessResult();

            if (server.Settings.CustomCommandPermissions.CustomizedPermission.Any())
            {
                // Check for a command match
                var match = server.Settings.CustomCommandPermissions.CustomizedPermission.FirstOrDefault(x => x.IsCommand == true && x.Name.Equals(string.IsNullOrWhiteSpace(command.Aliases.FirstOrDefault()) ? command.Name : command.Aliases.FirstOrDefault(), StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    defaultPermissionLevel = match.Setting;
                    resultInfo.IsCommand = true;
                    resultInfo.IsOverridden = true;
                    resultInfo.MatchName = match.Name;
                }
                else
                {
                    // Check for a module match
                    match = server.Settings.CustomCommandPermissions.CustomizedPermission.FirstOrDefault(x => x.IsCommand == false && x.Name.Equals(string.IsNullOrWhiteSpace(command.Module.Aliases.FirstOrDefault()) ? command.Module.Name : command.Module.Aliases.FirstOrDefault(), StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                    {
                        defaultPermissionLevel = match.Setting;
                        resultInfo.IsCommand = false;
                        resultInfo.IsOverridden = true;
                        resultInfo.MatchName = match.Name;
                    }
                }
            }

            if (defaultPermissionLevel == DefaultPermissionLevel.AllUsers)
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            if (defaultPermissionLevel == DefaultPermissionLevel.Registered)
            {
                if (server.Users.Any(x => x.UserID == context.User.Id))
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }
            else if (defaultPermissionLevel == DefaultPermissionLevel.Moderators)
            {
                if (context.User.CastToSocketGuildUser().IsModeratorOrHigher(server.Settings.Moderation, context.Client))
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }
            else if (defaultPermissionLevel == DefaultPermissionLevel.Administrators)
            {
                if (context.User.CastToSocketGuildUser().IsAdminOrHigher(server.Settings.Moderation, context.Client))
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }
            else if (defaultPermissionLevel == DefaultPermissionLevel.ServerOwner)
            {
                if (context.User.Id == context.Guild.OwnerId
                    || context.Client.GetApplicationInfoAsync().Result.Owner.Id == context.User.Id)
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }
            else if (defaultPermissionLevel == DefaultPermissionLevel.BotOwner)
            {
                if (context.Client.GetApplicationInfoAsync().Result.Owner.Id == context.User.Id)
                {
                    return Task.FromResult(PreconditionResult.FromSuccess());
                }
            }

            return Task.FromResult(PreconditionResult.FromError($"You do not have the access level of {defaultPermissionLevel}, which is required to run this commandn\n" +
                $"IsCommand: {resultInfo.IsCommand}\n" +
                $"IsOverridden: {resultInfo.IsCommand}\n" +
                $"Match Name: {resultInfo.MatchName}"));
        }

        public class AccessResult
        {
            public bool IsCommand { get; set; } = true;
            public string MatchName { get; set; } = "Default";
            public bool IsOverridden { get; set; } = false;
        }
    }
}