namespace ELO.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using global::Discord;
    using global::Discord.Commands;

    using ELO.Discord.Context;
    using ELO.Discord.Extensions;
    using ELO.Discord.Preconditions;
    using ELO.Models;

    using global::Discord.WebSocket;

    [CustomPermissions(DefaultPermissionLevel.Registered)]
    [CheckLobby]
    [Summary("Game Queuing and Information")]
    public class MatchMaking : Base
    {
        [Command("Join")]
        [Alias("j", "sign", "play", "rdy", "ready")]
        [Summary("Join the current lobby's queue")]
        public async Task JoinLobbyAsync()
        {
            if (!Context.Elo.Lobby.Game.QueuedPlayerIDs.Contains(Context.User.Id))
            {
                if (Context.Server.Settings.GameSettings.BlockMultiQueuing)
                {
                    if (Context.Server.Lobbies.Any(x => x.Game.QueuedPlayerIDs.Contains(Context.User.Id) || x.Game.Team1.Players.Contains(Context.User.Id) || x.Game.Team2.Players.Contains(Context.User.Id)))
                    {
                        throw new Exception("MultiQueuing is disabled by the server Admins");
                    }
                }

                if (Context.Elo.User.Banned.Banned)
                {
                    throw new Exception($"You are banned from matchmaking for another {(Context.Elo.User.Banned.ExpiryTime - DateTime.UtcNow).TotalMinutes}");
                }

                if (Context.Elo.Lobby.Game.IsPickingTeams)
                {
                    throw new Exception("Currently Picking teams. Please wait until this is completed");
                }

                var previous = Context.Server.Results.Where(x => x.LobbyID == Context.Elo.Lobby.ChannelID && (x.Team1.Contains(Context.User.Id) || x.Team2.Contains(Context.User.Id))).OrderByDescending(x => x.Time).FirstOrDefault();
                if (previous != null && previous.Time + Context.Server.Settings.GameSettings.ReQueueDelay > DateTime.UtcNow)
                {
                    if (previous.Result == GuildModel.GameResult._Result.Undecided)
                    {
                        throw new Exception($"You must wait another {(previous.Time + Context.Server.Settings.GameSettings.ReQueueDelay - DateTime.UtcNow).TotalMinutes} minutes before rejoining the queue");
                    }
                }

                Context.Elo.Lobby.Game.QueuedPlayerIDs.Add(Context.User.Id);
                Context.Server.Save();
                await SimpleEmbedAsync($"[{Context.Elo.Lobby.Game.QueuedPlayerIDs.Count}/{Context.Elo.Lobby.UserLimit}] Added {Context.User.Mention} to queue");
                if (Context.Elo.Lobby.UserLimit >= Context.Elo.Lobby.Game.QueuedPlayerIDs.Count)
                {
                    // Game is ready to be played
                    await FullGame.FullQueueAsync(Context);
                }
            }
            else
            {
                if (Context.Server.Settings.Readability.JoinLeaveErrors)
                {
                    throw new Exception("You are already queued for this lobby");
                }
            }
        }

        [Command("Leave")]
        [Alias("l", "out", "unSign", "remove", "unready")]
        [Summary("Leave the current lobby's queue")]
        public async Task LeaveLobbyAsync()
        {
            if (Context.Elo.Lobby.Game.QueuedPlayerIDs.Contains(Context.User.Id))
            {
                if (Context.Elo.Lobby.Game.IsPickingTeams)
                {
                    throw new Exception("Currently Picking teams. Please wait until this is completed");
                }

                Context.Elo.Lobby.Game.QueuedPlayerIDs.Remove(Context.User.Id);
                await SimpleEmbedAsync($"[{Context.Elo.Lobby.Game.QueuedPlayerIDs.Count}/{Context.Elo.Lobby.UserLimit}] Removed {Context.User.Mention} from queue");
                Context.Server.Save();
            }
            else
            {
                if (Context.Server.Settings.Readability.JoinLeaveErrors)
                {
                    throw new Exception("You cannot leave a lobby you aren't queued for");
                }
            }
        }

        [Command("Queue", RunMode = RunMode.Async)]
        [Alias("q", "listPlayers", "playerList", "lps")]
        [Summary("View the current lobby's queue")]
        public async Task QueueAsync()
        {
            var queuedPlayers = Context.Elo.Lobby.Game.QueuedPlayerIDs.Select(p => Context.Guild.GetUser(p)).Where(x => x != null).ToList();

            if (Context.Elo.Lobby.Game.QueuedPlayerIDs.Count != queuedPlayers.Count)
            {
                Context.Elo.Lobby.Game.QueuedPlayerIDs = queuedPlayers.Select(x => x.Id).ToList();
                Context.Server.Save();
            }

            if (!Context.Elo.Lobby.Game.IsPickingTeams)
            {
                if (queuedPlayers.Any())
                {
                    await SimpleEmbedAsync($"**Player List [{Context.Elo.Lobby.Game.QueuedPlayerIDs.Count}/{Context.Elo.Lobby.UserLimit}]**\n" + 
                                           $"{string.Join("\n", queuedPlayers.Select(x => x.Mention))}");
                }
                else
                {
                    await SimpleEmbedAsync($"[0/{Context.Elo.Lobby.UserLimit}] The queue is empty");
                }
            }
            else
            {
                await SimpleEmbedAsync($"**Team1 Captain** {Context.Guild.GetUser(Context.Elo.Lobby.Game.Team1.Captain)?.Mention}\n" +
                                       $"**Team1:** {string.Join(", ", Context.Elo.Lobby.Game.Team1.Players.Select(x => Context.Guild.GetUser(x)?.Mention).ToList())}\n\n" +
                                       $"**Team2 Captain** {Context.Guild.GetUser(Context.Elo.Lobby.Game.Team2.Captain)?.Mention}\n" +
                                       $"**Team2:** {string.Join(", ", Context.Elo.Lobby.Game.Team2.Players.Select(x => Context.Guild.GetUser(x)?.Mention).ToList())}\n\n" +
                                       $"**Select Your Teams using `{Context.Prefix}pick <@user>`**\n" +
                                       $"**It is Captain {(Context.Elo.Lobby.Game.Team1.TurnToPick ? 1 : 2)}'s Turn to pick**\n\n" +
                                       "**Player Pool**\n" +
                                       $"{string.Join(" ", Context.Elo.Lobby.Game.QueuedPlayerIDs.Select(x => Context.Guild.GetUser(x)?.Mention))}");
            }
        }

        [Command("Lobby", RunMode = RunMode.Async)]
        [Summary("View information about the current lobby")]
        public Task LobbyInfoAsync()
        {
            return ReplyAsync(new EmbedBuilder
            {
                Title = $"{Context.Channel.Name} ",
                Description = $"**Players Per team**: {Context.Elo.Lobby.UserLimit / 2}\n" +
                              $"**Total Players**: {Context.Elo.Lobby.UserLimit}\n" +
                              $"**Sort Mode**: {Context.Elo.Lobby.PickMode.ToString()}\n" +
                              $"**Game Number**: {Context.Elo.Lobby.GamesPlayed + 1}\n" +
                              $"**Host Pick mode**: {Context.Elo.Lobby.HostSelectionMode}\n" +
                              "**Description**:\n" +
                              $"{Context.Elo.Lobby.Description}",
                Color = Color.Blue
            }.Build());
        }

        public Task CheckReplacePermissionAsync(GuildModel.Lobby.CurrentGame g, ulong userToReplace)
        {
            if (!g.QueuedPlayerIDs.Contains(userToReplace) && !g.Team1.Players.Contains(userToReplace) && !g.Team2.Players.Contains(userToReplace))
            {
                throw new Exception("User is not queued.");
            }

            if (g.Team1.Captain == userToReplace || g.Team2.Captain == userToReplace)
            {
                throw new Exception("You cannot replace a team captain");
            }

            if (Context.Elo.User.Banned.Banned)
            {
                throw new Exception($"You are banned from matchmaking for another {(Context.Elo.User.Banned.ExpiryTime - DateTime.UtcNow).TotalMinutes} minutes");
            }

            if (g.QueuedPlayerIDs.Contains(Context.User.Id) || g.Team1.Players.Contains(Context.User.Id) || g.Team2.Players.Contains(Context.User.Id) || g.Team2.Captain == Context.User.Id || g.Team1.Captain == Context.User.Id)
            {
                throw new Exception("You cannot replace a user if you are in the queue yourself");
            }
            
            if (Context.Server.Settings.GameSettings.BlockMultiQueuing)
            {
                if (Context.Server.Lobbies.Any(x => x.Game.QueuedPlayerIDs.Contains(Context.User.Id) || x.Game.Team1.Players.Contains(Context.User.Id) || x.Game.Team2.Players.Contains(Context.User.Id)))
                {
                    throw new Exception("MultiQueuing is disabled by the server Admins");
                }
            }

            var previous = Context.Server.Results.Where(x => x.LobbyID == Context.Elo.Lobby.ChannelID && (x.Team1.Contains(Context.User.Id) || x.Team2.Contains(Context.User.Id))).OrderByDescending(x => x.Time).FirstOrDefault();
            if (previous != null && previous.Time + Context.Server.Settings.GameSettings.ReQueueDelay > DateTime.UtcNow)
            {
                if (previous.Result == GuildModel.GameResult._Result.Undecided)
                {
                    throw new Exception($"You must wait another {(previous.Time + Context.Server.Settings.GameSettings.ReQueueDelay - DateTime.UtcNow).TotalMinutes} minutes before rejoining the queue");
                }
            }

            return Task.CompletedTask;
        }

        [Command("Replace")]
        [Summary("Replace a user in the current queue")]
        public async Task ReplaceAsync(SocketGuildUser userToReplace)
        {
            var g = Context.Elo.Lobby.Game;
            await CheckReplacePermissionAsync(g, userToReplace.Id);
          
            if (g.QueuedPlayerIDs.Contains(userToReplace.Id))
            {
                Context.Elo.Lobby.Game.QueuedPlayerIDs.Remove(userToReplace.Id);
                Context.Elo.Lobby.Game.QueuedPlayerIDs.Add(Context.User.Id);
            }
            else if (g.Team1.Players.Contains(userToReplace.Id))
            {
                Context.Elo.Lobby.Game.QueuedPlayerIDs.Remove(userToReplace.Id);
                Context.Elo.Lobby.Game.QueuedPlayerIDs.Add(Context.User.Id);
            }
            else if (g.Team2.Players.Contains(userToReplace.Id))
            {
                Context.Elo.Lobby.Game.QueuedPlayerIDs.Remove(userToReplace.Id);
                Context.Elo.Lobby.Game.QueuedPlayerIDs.Add(Context.User.Id);
            }
            else
            {
                throw new Exception("Unknown Player Exception!");
            }

            Context.Server.Save();
            await SimpleEmbedAsync($"Success, {Context.User.Mention} replaced {userToReplace.Mention}");
            if (Context.Elo.Lobby.UserLimit >= Context.Elo.Lobby.Game.QueuedPlayerIDs.Count)
            {
                // Game is ready to be played
                await FullGame.FullQueueAsync(Context);
            }
        }

        [Command("Pick")]
        [Alias("p")]
        [Summary("Pick a player for your team")]
        [Remarks("Must pick a player that is in the queue and isn't already on a team\nYou must be the captain of a team to run this command")]
        public async Task PickUserAsync(IGuildUser pickedUser)
        {
            if (!Context.Elo.Lobby.Game.IsPickingTeams)
            {
                throw new Exception("Lobby is not picking teams at the moment.");
            }

            if (Context.Elo.Lobby.Game.Team1.Captain != Context.User.Id && Context.Elo.Lobby.Game.Team2.Captain != Context.User.Id)
            {
                throw new Exception($"{Context.User.Mention} is not a captain");
            }

            if (!Context.Elo.Lobby.Game.QueuedPlayerIDs.Contains(pickedUser.Id))
            {
                throw new Exception($"{pickedUser.Mention} is not able to be picked");
            }

            int nextTeam;
            if (Context.Elo.Lobby.Game.Team1.TurnToPick)
            {
                if (Context.User.Id != Context.Elo.Lobby.Game.Team1.Captain)
                {
                    throw new Exception("It is not your turn to pick.");
                }

                Context.Elo.Lobby.Game.Team1.Players.Add(pickedUser.Id);
                nextTeam = 2;
                Context.Elo.Lobby.Game.Team2.TurnToPick = true;
                Context.Elo.Lobby.Game.Team1.TurnToPick = false;
            }
            else
            {
                if (Context.User.Id != Context.Elo.Lobby.Game.Team2.Captain)
                {
                    throw new Exception("It is not your turn to pick.");
                }

                Context.Elo.Lobby.Game.Team2.Players.Add(pickedUser.Id);
                nextTeam = 1;
                Context.Elo.Lobby.Game.Team2.TurnToPick = false;
                Context.Elo.Lobby.Game.Team1.TurnToPick = true;
            }

            Context.Elo.Lobby.Game.QueuedPlayerIDs.Remove(pickedUser.Id);

            if (Context.Elo.Lobby.Game.QueuedPlayerIDs.Count == 1)
            {
                var lastPlayer = Context.Elo.Lobby.Game.QueuedPlayerIDs.FirstOrDefault();
                if (Context.Elo.Lobby.Game.Team1.TurnToPick)
                {
                    Context.Elo.Lobby.Game.Team1.Players.Add(lastPlayer);
                }
                else
                {
                    Context.Elo.Lobby.Game.Team2.Players.Add(lastPlayer);
                }

                Context.Elo.Lobby.Game.QueuedPlayerIDs.Remove(lastPlayer);
            }

            if (Context.Elo.Lobby.Game.QueuedPlayerIDs.Count == 0)
            {
                Context.Elo.Lobby.GamesPlayed++;

                /*
                await ReplyAsync("**Game has Started**\n" +
                                 $"Team1: {string.Join(", ", Context.Elo.Lobby.Game.Team1.Players.Select(x => Context.Guild.GetUser(x)?.Mention).ToList())}\n" +
                                 $"Team2: {string.Join(", ", Context.Elo.Lobby.Game.Team2.Players.Select(x => Context.Guild.GetUser(x)?.Mention).ToList())}\n" +
                                 $"**Game #{Context.Elo.Lobby.GamesPlayed}**");
                                 */
                Context.Server.Results.Add(new GuildModel.GameResult
                {
                    Comments = new List<GuildModel.GameResult.Comment>(),
                    GameNumber = Context.Elo.Lobby.GamesPlayed,
                    LobbyID = Context.Elo.Lobby.ChannelID,
                    Result = GuildModel.GameResult._Result.Undecided,
                    Team1 = Context.Elo.Lobby.Game.Team1.Players,
                    Team2 = Context.Elo.Lobby.Game.Team2.Players,
                    Time = DateTime.UtcNow
                });
                await FullGame.AnnounceGameAsync(Context);
                Context.Elo.Lobby.Game = new GuildModel.Lobby.CurrentGame();
            }
            else
            {
                await SimpleEmbedAsync($"**Team1 Captain** {Context.Guild.GetUser(Context.Elo.Lobby.Game.Team1.Captain)?.Mention}\n" +
                                       $"**Team1:** {string.Join(", ", Context.Elo.Lobby.Game.Team1.Players.Select(x => Context.Guild.GetUser(x)?.Mention).ToList())}\n\n" +
                                       $"**Team2 Captain** {Context.Guild.GetUser(Context.Elo.Lobby.Game.Team2.Captain)?.Mention}\n" +
                                       $"**Team2:** {string.Join(", ", Context.Elo.Lobby.Game.Team2.Players.Select(x => Context.Guild.GetUser(x)?.Mention).ToList())}\n\n" +
                                       $"**Select Your Teams using `{Context.Prefix}pick <@user>`**\n" +
                                       $"**It is Captain {nextTeam}'s Turn to pick**\n\n" +
                                       "**Player Pool**\n" +
                                       $"{string.Join(" ", Context.Elo.Lobby.Game.QueuedPlayerIDs.Select(x => Context.Guild.GetUser(x)?.Mention))}");
            }

            Context.Server.Save();
        }

        [Command("ResultTypes", RunMode = RunMode.Async)]
        [Summary("list game result types")]
        public Task ResultTypesAsync()
        {
            return SimpleEmbedAsync($"**Game Results:**\n{string.Join("\n", EloInfo.GameResults())}");
        }

        [Command("GameResult")]
        [Summary("Vote for the result of a game in the current channel")]
        public Task GameResultAsync(int gameNumber, GuildModel.GameResult._Result result)
        {
            return GameResultAsync(Context.Channel.Id, gameNumber, result);
        }

        [Command("GameResult")]
        [Summary("Vote for the result of a game in the given channel")]
        public Task GameResultAsync(ITextChannel channel, int gameNumber, GuildModel.GameResult._Result result)
        {
            return GameResultAsync(channel.Id, gameNumber, result);
        }

        public async Task GameResultAsync(ulong lobbyChannel, int gameNumber, GuildModel.GameResult._Result result)
        {
            if (!Context.Server.Settings.GameSettings.AllowUserSubmissions)
            {
                throw new Exception("Users are not allowed to self submit game results in this server");
            }

            var selectedGame = Context.Server.Results.FirstOrDefault(x => x.LobbyID == lobbyChannel && x.GameNumber == gameNumber);
            if (selectedGame == null)
            {
                throw new Exception("Game Unavailable. Incorrect Data.");
            }

            if (selectedGame.Result != GuildModel.GameResult._Result.Undecided)
            {
                throw new Exception("Game must be undecided to submit player chosen result.");
            }

            if (result == GuildModel.GameResult._Result.Undecided)
            {
                throw new Exception("You cannot set the result to undecided");
            }

            if (selectedGame.Team1.Contains(Context.User.Id))
            {
                if (selectedGame.Proposal.P1 == 0)
                {
                    selectedGame.Proposal.P1 = Context.User.Id;
                    selectedGame.Proposal.R1 = result;
                }
                else
                {
                    throw new Exception("A player has already submitted a result from this team.");
                }
            }
            else if (selectedGame.Team2.Contains(Context.User.Id))
            {
                if (selectedGame.Proposal.P2 == 0)
                {
                    selectedGame.Proposal.P2 = Context.User.Id;
                    selectedGame.Proposal.R2 = result;
                }
                else
                {
                    throw new Exception("A player has already submitted a result from this team.");
                }
            }
            else
            {
                throw new Exception("You must be on either team to submit the game result.");
            }

            Context.Server.Save();
            await SimpleEmbedAsync("Result Proposal\n" +
                                   $"Team1 Submission: {selectedGame.Proposal.R1} Player: {Context.Guild.GetUser(selectedGame.Proposal.P1)?.Mention ?? "N/A"}\n" +
                                   $"Team2 Submission: {selectedGame.Proposal.R2} Player: {Context.Guild.GetUser(selectedGame.Proposal.P2)?.Mention ?? "N/A"}");

            if (selectedGame.Proposal.R1 == GuildModel.GameResult._Result.Undecided || selectedGame.Proposal.R2 == GuildModel.GameResult._Result.Undecided)
            {
                return;
            }

            if (selectedGame.Proposal.R1 == selectedGame.Proposal.R2)
            {
                await GameManagement.GameResultAsync(Context, selectedGame, result);
            }
            else
            {
                throw new Exception("Mismatched Game Result Proposals. Please allow an admin to manually submit a result");
            }
        }

        [CheckLobby]
        [Command("Maps", RunMode = RunMode.Async)]
        [Summary("Show a list of all maps for the current lobby")]
        public Task MapsAsync()
        {
            return SimpleEmbedAsync($"{string.Join("\n", Context.Elo.Lobby.Maps)}");
        }

        [CheckLobby]
        [Command("Map", RunMode = RunMode.Async)]
        [Summary("select a random map for the lobby")]
        public Task MapAsync()
        {
            if (Context.Elo.Lobby.Maps.Any())
            {
                var r = new Random();
                return SimpleEmbedAsync($"{Context.Elo.Lobby.Maps.OrderByDescending(m => r.Next()).FirstOrDefault()}");
            }

            return SimpleEmbedAsync("There are no maps set in this lobby");
        }
    }
}
