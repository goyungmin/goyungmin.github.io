namespace ELO.Modules.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using ELO.Discord.Context;
    using ELO.Discord.Extensions;
    using ELO.Discord.Preconditions;
    using ELO.Models;

    using global::Discord;
    using global::Discord.Addons.Interactive;
    using global::Discord.Commands;

    [CustomPermissions(DefaultPermissionLevel.Administrators)]
    [Summary("Lobby creation and setup commands")]
    public class Lobby : Base
    {
        [CheckLobby]
        [Command("LobbyInfo", RunMode = RunMode.Async)]
        [Summary("View information about the current lobby")]
        public Task LobbyInfoAsync()
        {
            var l = Context.Elo.Lobby;
            return SimpleEmbedAsync($"**UserLimit:** {l.UserLimit}\n" +
                                    $"**MapMode:** {l.MapMode}\n" +
                                    $"**CaptainSortMode:** {l.CaptainSortMode}\n" +
                                    $"**PickMode:** {l.PickMode}\n" +
                                    $"**HostSelectionMode:** {l.HostSelectionMode}\n" +
                                    $"**GamesPlayed:** {l.GamesPlayed}\n" +
                                    $"**Maps:** {string.Join(", ", l.Maps)}\n" +
                                    $"**Description:** \n{l.Description}\n");
        }


        [Command("CreateLobby")]
        [Summary("Interactive lobby creation command")]
        [RequireBotPermission(ChannelPermission.AddReactions)]
        public Task CreateLobbyAsync()
        {
            if (Context.Elo.Lobby != null)
            {
                throw new Exception("Channel is already a lobby");
            }

            var lobby = new GuildModel.Lobby
                            {
                                ChannelID = Context.Channel.Id
                            };

            return InlineReactionReplyAsync(
                new ReactionCallbackData(
                        "",
                        new EmbedBuilder
                            {
                                Description =
                                    "Please react with the amount of players you would like **PER TEAM**\n" + 
                                    "ie. :two: = each team has two players (4 players total)"
                            }.Build(),
                        timeout: TimeSpan.FromMinutes(2), timeoutCallback: c => SimpleEmbedAsync("Command Timed Out"))
                    .WithCallback(new Emoji("1\u20e3"), (c, r) => SortModeAsync(lobby, 1))
                    .WithCallback(new Emoji("2\u20e3"), (c, r) => SortModeAsync(lobby, 2))
                    .WithCallback(new Emoji("3\u20e3"), (c, r) => SortModeAsync(lobby, 3))
                    .WithCallback(new Emoji("4\u20e3"), (c, r) => SortModeAsync(lobby, 4))
                    .WithCallback(new Emoji("5\u20e3"), (c, r) => SortModeAsync(lobby, 5))
                    .WithCallback(new Emoji("6\u20e3"), (c, r) => SortModeAsync(lobby, 6))
                    .WithCallback(new Emoji("7\u20e3"), (c, r) => SortModeAsync(lobby, 7))
                    .WithCallback(new Emoji("8\u20e3"), (c, r) => SortModeAsync(lobby, 8))
                    .WithCallback(new Emoji("9\u20e3"), (c, r) => SortModeAsync(lobby, 9)));
        }

        public Task SortModeAsync(GuildModel.Lobby lobby, int teamPlayers)
        {
            lobby.UserLimit = teamPlayers * 2;
            return InlineReactionReplyAsync(
                new ReactionCallbackData(
                        "",
                        new EmbedBuilder
                            {
                                Description =
                                    "Please react with the team sorting mode you would like for this lobby:\n" +
                                    "1\u20e3 `CompleteRandom` __**Completely Random Team sorting**__\n" +
                                    "All teams are chosen completely randomly\n\n" +
                                    "2\u20e3 `Captains` __**Captains Mode**__\n" +
                                    "Two team captains are chosen, they each take turns picking players until teams are both filled.\n\n" +
                                    "3\u20e3 `SortByScore` __**Score Balance Mode**__\n" +
                                    "Players will be automatically selected and teams will be balanced based on player scores"
                            }.Build(),
                        timeout: TimeSpan.FromMinutes(2), timeoutCallback: c => SimpleEmbedAsync("Command Timed Out"))
                    .WithCallback(new Emoji("1\u20e3"), (c, r) => CompleteLobbyCreationAsync(lobby, GuildModel.Lobby._PickMode.CompleteRandom))
                    .WithCallback(new Emoji("2\u20e3"), (c, r) => CompleteLobbyCreationAsync(lobby, GuildModel.Lobby._PickMode.Captains))
                    .WithCallback(new Emoji("3\u20e3"), (c, r) => CompleteLobbyCreationAsync(lobby, GuildModel.Lobby._PickMode.SortByScore)));
        }

        public Task CompleteLobbyCreationAsync(GuildModel.Lobby lobby, GuildModel.Lobby._PickMode pickMode)
        {
            lobby.PickMode = pickMode;
            Context.Server.Lobbies.Add(lobby);
            Context.Server.Save();
            return SimpleEmbedAsync(
                "Success, Lobby has been created.\n" + $"`Size:` {lobby.UserLimit}\n"
                                                      + $"`Team Size:` {lobby.UserLimit / 2}\n"
                                                      + $"`Team Mode:` {(lobby.PickMode == GuildModel.Lobby._PickMode.Captains ? $"Captains => {lobby.CaptainSortMode}" : $"{lobby.PickMode}")}\n"
                                                      + $"`Host Selection Mode:` {lobby.HostSelectionMode}\n\n"
                                                      + $"To Set Description: `{Context.Prefix}LobbyDescription <description>`\n"
                                                      + $"For More info, type `{Context.Prefix}help Lobby`");
        }

        [CheckLobby]
        [Command("RemoveLobby")]
        [Summary("Deletes the current lobby")]
        public Task RemoveLobbyAsync()
        {
            Context.Server.Lobbies.Remove(Context.Elo.Lobby);
            Context.Server.Save();
            return SimpleEmbedAsync("Success, Lobby has been removed.");
        }

        [CheckLobby]
        [Command("ClearQueue")]
        [Summary("Kick all users out of the current queue")]
        public Task ClearQueueAsync()
        {
            Context.Elo.Lobby.Game = new GuildModel.Lobby.CurrentGame();
            Context.Server.Save();
            return SimpleEmbedAsync("Queue has been cleared");
        }

        [CheckLobby]
        [Command("LobbyDescription")]
        [Summary("Set the description of the current lobby")]
        public Task SetDescriptionAsync([Remainder] string description)
        {
            if (description.Length > 200)
            {
                throw new Exception("Lobby description is limited to 200 characters or less.");
            }

            Context.Elo.Lobby.Description = description;
            Context.Server.Save();
            return SimpleEmbedAsync($"Success, Description is now:\n{description}");
        }

        [CheckLobby]
        [Command("LobbySortMode")]
        [Summary("Select hor players are sorted into teams")]
        public Task LobbySortModeAsync(GuildModel.Lobby._PickMode sortMode)
        {
            Context.Elo.Lobby.PickMode = sortMode;
            Context.Server.Save();

            return SimpleEmbedAsync("Success, lobby team sort mode has been modified to:\n" +
                                    $"{sortMode.ToString()}");
        }

        [CheckLobby]
        [Command("LobbySortMode", RunMode = RunMode.Async)]
        [Summary("Show sort modes for teams")]
        public Task LobbySortModeAsync()
        {
            return SimpleEmbedAsync($"Please use command `{Context.Prefix}LobbySortMode <mode>` with the selection mode you would like for this lobby:\n" +
                                    "`CompleteRandom` __**Completely Random Team sorting**__\n" +
                                    "All teams are chosen completely randomly\n\n" +
                                    "`Captains` __**Captains Mode**__\n" +
                                    "Two team captains are chosen, they each take turns picking players until teams are both filled.\n\n" +
                                    "`SortByScore` __**Score Balance Mode**__\n" +
                                    "Players will be automatically selected and teams will be balanced based on player scores");
        }

        [CheckLobby]
        [Command("CaptainSortMode")]
        [Summary("Select how captains are picked")]
        public Task CapSortModeAsync(GuildModel.Lobby.CaptainSort captainSortMode)
        {
            Context.Elo.Lobby.CaptainSortMode = captainSortMode;
            Context.Server.Save();

            return SimpleEmbedAsync("Success, captain sort mode has been modified to:\n" +
                                    $"{captainSortMode.ToString()}");
        }

        [CheckLobby]
        [Command("CaptainSortMode", RunMode = RunMode.Async)]
        [Summary("Show captain sort modes")]
        public Task CapSortModeAsync()
        {
            return SimpleEmbedAsync($"Please use command `{Context.Prefix}CapSortMode <mode>` with the captain selection mode you would like for this lobby:\n" +
                                    "`MostWins` __**Choose Two Players with Highest Wins**__\n" +
                                    "Selects the two players with the highest amount of Wins\n\n" +
                                    "`MostPoints` __**Choose Two Players with Highest Points**__\n" +
                                    "Selects the two players with the highest amount of Points\n\n" +
                                    "`HighestWinLoss` __**Selects the two players with the highest Win/Loss Ratio**__\n" +
                                    "Selects the two players with the highest win/loss ratio\n\n" +
                                    "`Random` __**Random**__\n" +
                                    "Selects Randomly\n\n" +
                                    "`RandomTop4MostPoints` __**Selects Random from top 4 Most Points**__\n" +
                                    "Selects Randomly from the top 4 highest ranked players based on points\n\n" +
                                    "`RandomTop4MostWins` __**Selects Random from top 4 Most Wins**__\n" +
                                    "Selects Randomly from the top 4 highest ranked players based on wins\n\n" +
                                    "`RandomTop4HighestWinLoss` __**Selects Random from top 4 Highest Win/Loss Ratio**__\n" +
                                    "Selects Randomly from the top 4 highest ranked players based on win/loss ratio");
        }

        [CheckLobby]
        [Command("MapMode")]
        [Summary("toggle whether to select a random map on game announcements")]
        public Task RandomMapAsync(GuildModel.Lobby.MapSelector mapMode)
        {
            Context.Elo.Lobby.MapMode = mapMode;
            Context.Server.Save();

            return SimpleEmbedAsync($"Map Selection mode: {Context.Elo.Lobby.MapMode}");
        }

        [CheckLobby]
        [Command("MapMode", RunMode = RunMode.Async)]
        [Summary("lists map mode types")]
        public Task MapModesAsync()
        {
            return SimpleEmbedAsync("Map Modes:\n" + 
                                    $"{string.Join("\n", EloInfo.MapTypes())}");
        }

        [CheckLobby]
        [Command("HostSelectionMode")]
        [Summary("Select how game hosts are chosen")]
        public Task HostModeAsync(GuildModel.Lobby.HostSelector hostSelectionMode)
        {
            Context.Elo.Lobby.HostSelectionMode = hostSelectionMode;
            Context.Server.Save();

            return SimpleEmbedAsync($"Host selection mode = {hostSelectionMode.ToString()}");
        }

        [CheckLobby]
        [Command("HostSelectionMode", RunMode = RunMode.Async)]
        [Summary("Display host selection modes")]
        public Task HostModeAsync()
        {
            return SimpleEmbedAsync($"Please use command `{Context.Prefix}HostSelectionMode <mode>` with the host selection mode you would like for this lobby:\n" +
                                    "`MostWins` __**Selects Player with Most Wins**__\n" +
                                    "`MostPoints` __**Selects Player with Most Points**__\n" +
                                    "`HighestWinLoss` __**Selects the Player with the highest Win/Loss Ratio**__\n" +
                                    "`Random` __**Random**__\n");
        }

        [CheckLobby]
        [Command("AddMap")]
        [Summary("Add a map to the current lobby")]
        public async Task AddMapAsync([Remainder] string mapName)
        {
            if (!Context.Elo.Lobby.Maps.Contains(mapName))
            {
                Context.Elo.Lobby.Maps.Add(mapName);
                Context.Server.Save();
                await SimpleEmbedAsync("Success, Lobby Map list is now:\n" +
                                       $"{string.Join("\n", Context.Elo.Lobby.Maps)}");
            }
            else
            {
                throw new Exception("Map has already been added to the lobby");
            }
        }

        [CheckLobby]
        [Command("DelMap")]
        [Summary("Remove a map from the current lobby")]
        public async Task DelMapAsync([Remainder] string mapName)
        {
            if (Context.Elo.Lobby.Maps.Contains(mapName))
            {
                Context.Elo.Lobby.Maps.Remove(mapName);
                Context.Server.Save();
                await SimpleEmbedAsync("Success, Lobby Map list is now:\n" +
                                       $"{string.Join("\n", Context.Elo.Lobby.Maps)}");
            }
            else
            {
                throw new Exception("Map is not in lobby");
            }
        }

        [CheckLobby]
        [Command("AddMaps")]
        [Summary("Add multiple maps to the current lobby")]
        [Remarks("Separate using commas ie. Map 1,Map 2,Map 3,")]
        public async Task AddMapsAsync([Remainder] string mapList)
        {
            var maps = mapList.Split(",");
            if (!Context.Elo.Lobby.Maps.Any(x => maps.Contains(x)))
            {
                Context.Elo.Lobby.Maps.AddRange(maps);
                Context.Server.Save();
                await SimpleEmbedAsync("Success, Lobby Map list is now:\n" +
                                       $"{string.Join("\n", Context.Elo.Lobby.Maps)}");
            }
            else
            {
                throw new Exception("One of the provided maps is already in the lobby");
            }
        }

        [CheckLobby]
        [Command("ClearMaps")]
        [Summary("Remove all maps from the current lobby")]
        public Task ClearMapsAsync()
        {
            Context.Elo.Lobby.Maps = new List<string>();
            Context.Server.Save();
            return SimpleEmbedAsync("Map List for this lobby has been reset.");
        }
    }
}