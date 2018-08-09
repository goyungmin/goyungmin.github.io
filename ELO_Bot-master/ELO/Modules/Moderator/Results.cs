namespace ELO.Modules.Moderator
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using ELO.Discord.Context;
    using ELO.Discord.Extensions;
    using ELO.Discord.Preconditions;
    using ELO.Models;

    using global::Discord;
    using global::Discord.Addons.Interactive;
    using global::Discord.Commands;
    using global::Discord.WebSocket;

    [CustomPermissions(DefaultPermissionLevel.Moderators)]
    [Summary("Game results submissions")]
    public class Results : Base
    {
        [Command("ResultTypes", RunMode = RunMode.Async)]
        [Summary("list game result types")]
        public Task ResultTypesAsync()
        {
            return SimpleEmbedAsync($"**Game Results:**\n{string.Join("\n", EloInfo.GameResults())}");
        }

        [Command("Game")]
        [Summary("Submit a game result")]
        public async Task GameAsync(IMessageChannel lobbyChannel, int gameNumber, GuildModel.GameResult._Result result)
        {
            if (Context.Server.Lobbies.All(x => x.ChannelID != lobbyChannel.Id))
            {
                throw new Exception("Channel is not a lobby");
            }

            var game = Context.Server.Results.FirstOrDefault(x => x.LobbyID == lobbyChannel.Id && x.GameNumber == gameNumber);
            if (game.Result != GuildModel.GameResult._Result.Undecided)
            {
                await InlineReactionReplyAsync(
                    new ReactionCallbackData(
                        "",
                        new EmbedBuilder
                            {
                                Description =
                                    "This game's Result has already been set to:\n"
                                    + $"{game.Result.ToString()}\n"
                                    + "Please react with ☑ To Still modify the result and update scores\n"
                                    + "Or react with 🇽 to cancel this command"
                            }.Build()).WithCallback(new Emoji("☑"),
                        (c, r) => GameManagement.GameResultAsync(Context, game, result))
                        .WithCallback(new Emoji("🇽"), (c,r) => SimpleEmbedAsync("Canceled Game Result")));
            }
            else
            {
                await GameManagement.GameResultAsync(Context, game, result);
            }
        }

        [Command("Win")]
        [Summary("Run a win event for the specified users")]
        public Task WinGameAsync(params SocketGuildUser[] users)
        {
            return GameManagement.WinAsync(users.ToList(), Context);
        }
        
        [Command("Lose")]
        [Summary("Run a Lose event for the specified users")]
        public Task LoseGameAsync(params SocketGuildUser[] users)
        {
            return GameManagement.LoseAsync(users.ToList(), Context);
        }

        [CheckLobby]
        [Command("ClearProposedResult")]
        [Summary("Clear the result of a proposal in the current channel")]
        public Task ClearGResAsync(int gameNumber)
        {
            return ClearGResAsync(Context.Channel.Id, gameNumber);
        }

        [Command("ClearProposedResult")]
        [Summary("Clear the result of a proposal in the given channel")]
        public Task ClearGResAsync(ITextChannel lobbyChannel, int gameNumber)
        {
            return ClearGResAsync(lobbyChannel.Id, gameNumber);
        }

        public Task ClearGResAsync(ulong lobbyChannel, int gameNumber)
        {
            var selectedGame = Context.Server.Results.FirstOrDefault(x => x.LobbyID == lobbyChannel && x.GameNumber == gameNumber);
            if (selectedGame == null)
            {
                throw new Exception("Game Unavailable. Incorrect Data.");
            }

            selectedGame.Proposal = new GuildModel.GameResult.ResultProposal();
            Context.Server.Save();
            return ReplyAsync("Reset.");
        }

    }
}