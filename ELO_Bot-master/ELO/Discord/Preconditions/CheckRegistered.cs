namespace ELO.Discord.Preconditions
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Discord.Commands;
    using ELO.Handlers;
    using ELO.Models;

    using Microsoft.Extensions.DependencyInjection;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CheckRegistered : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            return Task.FromResult(services.GetRequiredService<DatabaseHandler>().Execute<GuildModel>(DatabaseHandler.Operation.LOAD, null, context.Guild.Id.ToString())?.Users?.FirstOrDefault(x => x.UserID == context.User.Id) != null ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("User is Not Registered!"));
        }
    }
}