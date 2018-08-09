namespace ELO.Discord.Context
{
    using System;
    using System.Linq;

    using ELO.Handlers;
    using ELO.Models;

    using global::Discord.Commands;
    using global::Discord.WebSocket;

    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// The context.
    /// </summary>
    public class Context : ShardedCommandContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Context"/> class.
        /// </summary>
        /// <param name="client">
        /// The client param.
        /// </param>
        /// <param name="message">
        /// The message param.
        /// </param>
        /// <param name="serviceProvider">
        /// The service provider.
        /// </param>
        public Context(DiscordShardedClient client, SocketUserMessage message, IServiceProvider serviceProvider) : base(client, message)
        {
            // These are our custom additions to the context, giving access to the server object and all server objects through Context.
            Server = serviceProvider.GetRequiredService<DatabaseHandler>().Execute<GuildModel>(DatabaseHandler.Operation.LOAD, null, Guild.Id.ToString());
            Elo = new ServerContext
                      {
                          User = Server?.Users?.FirstOrDefault(x => x.UserID == User.Id),
                          Lobby = Server?.Lobbies?.FirstOrDefault(x => x.ChannelID == Channel.Id)
                      };
            Provider = serviceProvider;
            Prefix = Server.Settings.CustomPrefix ?? serviceProvider.GetRequiredService<ConfigModel>().Prefix;
        }

        /// <summary>
        /// Gets the server.
        /// </summary>
        public GuildModel Server { get; }

        public string Prefix { get; set; }

        public ServerContext Elo { get; set; }

        /// <summary>
        /// Gets the provider.
        /// </summary>
        public IServiceProvider Provider { get; }

        public class ServerContext
        {
            public GuildModel.User User { get; set; }

            public GuildModel.Lobby Lobby { get; set; }
        }
    }
}