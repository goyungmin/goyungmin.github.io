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
    public class CheckLobby : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            return Task.FromResult(services.GetRequiredService<DatabaseHandler>().Execute<GuildModel>(DatabaseHandler.Operation.LOAD, null, context.Guild.Id.ToString())?.Lobbies?.FirstOrDefault(x => x.ChannelID == context.Channel.Id) != null ? PreconditionResult.FromSuccess() : PreconditionResult.FromError("Current Channel is not a lobby!"));
        }
    }
}