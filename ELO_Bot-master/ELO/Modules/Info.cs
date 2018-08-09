namespace ELO.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using ELO.Discord.Context;
    using ELO.Discord.Extensions;
    using ELO.Discord.Preconditions;
    using ELO.Models;

    using global::Discord;
    using global::Discord.Addons.Interactive;
    using global::Discord.Commands;

    [CustomPermissions(DefaultPermissionLevel.AllUsers)]
    [RequireContext(ContextType.Guild)]
    [Summary("User Registration and Info")]
    public class Info : Base
    {
        public enum LeaderboardSortMode
        {
            Win,
            Loss,
            Points,
            GamesPlayed,
            Kills,
            Deaths,
            Draws
        }

        [Command("Register")]
        [Summary("Register by tagging yourself")]
        public Task RegisterAsync(IUser user)
        {
            if (user.Id == Context.User.Id)
            {
                return UserManagement.RegisterAsync(Context, Context.Server, Context.User, Context.User.Username);
            }

            throw new Exception("You cannot register by tagging another user");
        }

        [Command("Register")]
        [Summary("Register using your username")]
        public Task RegisterAsync()
        {
            return UserManagement.RegisterAsync(Context, Context.Server, Context.User, Context.User.Username);
        }

        [Command("Register")]
        [Summary("Register using a specified nickname")]
        public Task RegisterAsync([Remainder] string name)
        {
            return UserManagement.RegisterAsync(Context, Context.Server, Context.User, name);
        }

        [Command("Invite", RunMode = RunMode.Async)]
        [Summary("Invite the bot")]
        public Task InviteAsync()
        {
            return SimpleEmbedAsync($"You may invite the bot to your own server using the following URL: {BotInfo.GetInvite(Context)}");
        }
        
        [Command("GetUser", RunMode = RunMode.Async)]
        [Alias("userStats", "StatsUser", "stats")]
        [Summary("Get information about the specified user profile")]
        public Task GetUserAsync(IUser user = null)
        {
            if (user == null)
            {
                user = Context.User;
            }

            var eloProfile = Context.Server.Users.FirstOrDefault(x => x.UserID == user.Id);
            if (eloProfile == null)
            {
                throw new Exception("user is not registered");
            }

            var embed = new EmbedBuilder
                {
                    Color = Color.Blue,
                    Title = $"{eloProfile.Username} Profile"
                }
                .AddField("Points", eloProfile.Stats.Points.ToString())
                .AddField("Rank", Context.Guild.GetRole(UserManagement.MaxRole(Context, eloProfile).RoleID)?.Mention ?? "N/A")
                .AddField("Games", "**Games Played**\n" +
                                   $"{eloProfile.Stats.GamesPlayed}\n" +
                                   "**Wins**\n" +
                                   $"{eloProfile.Stats.Wins}\n" +
                                   "**Losses**\n" +
                                   $"{eloProfile.Stats.Losses}\n" +
                                   "**WLR**\n" +
                                   $"{(double) eloProfile.Stats.Wins / eloProfile.Stats.Losses}");

            if (Context.Server.Settings.GameSettings.UseKd)
            {
                embed.AddField("K/D", "**Kills**\n" +
                                      $"{eloProfile.Stats.Kills}\n" +
                                      "**Deaths**\n" +
                                      $"{eloProfile.Stats.Deaths}\n" +
                                      "**KDR**\n" +
                                      $"{(double) eloProfile.Stats.Kills / eloProfile.Stats.Deaths}");
            }

            return ReplyAsync(embed);
        }
        
        [Command("LeaderBoard", RunMode = RunMode.Async)]
        [Alias("SortLeaderboard", "SortLb", "LeaderboardSort")]
        [Summary("Displays leaderboard sort modes")]
        public Task LeaderboardAsync()
        {
            return SimpleEmbedAsync("Leader board Sort Options:\n" +
                                    "Win\r\n" +
                                    "Loss\r\n" +
                                    "Points\r\n" +
                                    "GamesPlayed\r\n" +
                                    "Kills\r\n" +
                                    "Deaths\r\n" +
                                    "Draws");
        }
        
        [Command("Leaderboard", RunMode = RunMode.Async)]
        [Alias("SortLeaderboard", "SortLb", "LeaderboardSort")]
        [Summary("Displays the leaderboard")]
        public Task LeaderboardAsync(LeaderboardSortMode sortMode = LeaderboardSortMode.Points)
        {
            var rgx = new Regex("[^a-zA-Z0-9 -#]");
            List<string> userStrings;
            switch (sortMode)
            {
                case LeaderboardSortMode.Points:
                {
                    var users = Context.Server.Users.OrderByDescending(x => x.Stats.Points).Where(x => Context.Guild.GetUser(x.UserID) != null).ToList();
                    userStrings = users.Select(x => $"`{$"#{users.IndexOf(x) + 1} - {rgx.Replace(x.Username, "")}".PadRight(33)}\u200B || Points: {x.Stats.Points}`").ToList();
                    break;
                }

                case LeaderboardSortMode.Win:
                {
                    var users = Context.Server.Users.OrderByDescending(x => x.Stats.Wins).Where(x => Context.Guild.GetUser(x.UserID) != null).ToList();
                    userStrings = users.Select(x => $"`{$"#{users.IndexOf(x) + 1} - {rgx.Replace(x.Username, "")}".PadRight(33)}\u200B || Wins: {x.Stats.Wins}`").ToList();
                    break;
                }

                case LeaderboardSortMode.Loss:
                {
                    var users = Context.Server.Users.OrderByDescending(x => x.Stats.Losses).Where(x => Context.Guild.GetUser(x.UserID) != null).ToList();
                    userStrings = users.Select(x => $"`{$"#{users.IndexOf(x) + 1} - {rgx.Replace(x.Username, "")}".PadRight(33)}\u200B || Losses: {x.Stats.Losses}`").ToList();
                    break;
                }

                case LeaderboardSortMode.Kills:
                {
                    var users = Context.Server.Users.OrderByDescending(x => x.Stats.Kills).Where(x => Context.Guild.GetUser(x.UserID) != null).ToList();
                    userStrings = users.Select(x => $"`{$"#{users.IndexOf(x) + 1} - {rgx.Replace(x.Username, "")}".PadRight(33)}\u200B || Kills: {x.Stats.Kills}`").ToList();
                    break;
                }

                case LeaderboardSortMode.Deaths:
                {
                    var users = Context.Server.Users.OrderByDescending(x => x.Stats.Deaths).Where(x => Context.Guild.GetUser(x.UserID) != null).ToList();
                    userStrings = users.Select(x => $"`{$"#{users.IndexOf(x) + 1} - {rgx.Replace(x.Username, "")}".PadRight(33)}\u200B || Deaths: {x.Stats.Deaths}`").ToList();
                    break;
                }

                case LeaderboardSortMode.Draws:
                {
                    var users = Context.Server.Users.OrderByDescending(x => x.Stats.Draws).Where(x => Context.Guild.GetUser(x.UserID) != null).ToList();
                    userStrings = users.Select(x => $"`{$"#{users.IndexOf(x) + 1} - {rgx.Replace(x.Username, "")}".PadRight(33)}\u200B || Draws: {x.Stats.Draws}`").ToList();
                    break;
                }

                case LeaderboardSortMode.GamesPlayed:
                {
                    var users = Context.Server.Users.OrderByDescending(x => x.Stats.GamesPlayed).Where(x => Context.Guild.GetUser(x.UserID) != null).ToList();
                    userStrings = users.Select(x => $"`{$"#{users.IndexOf(x) + 1} - {rgx.Replace(x.Username, "")}".PadRight(33)}\u200B || Games: {x.Stats.GamesPlayed}`").ToList();
                    break;
                }

                default:
                {
                    var users = Context.Server.Users.OrderByDescending(x => x.Stats.Points).Where(x => Context.Guild.GetUser(x.UserID) != null).ToList();
                    userStrings = users.Select(x => $"`{$"#{users.IndexOf(x) + 1} - {rgx.Replace(x.Username, "")}".PadRight(33)}\u200B || Points: {x.Stats.Points}`").ToList();
                    break;
                }
            }

            var pages = userStrings.SplitList(20).Select(x => new PaginatedMessage.Page
            {
                Description = string.Join("\n", x)
            });

            var pager = new PaginatedMessage
            {
                Title = "ELO Bot Leader board",
                Pages = pages,
                Color = Color.Blue
            };

            return PagedReplyAsync(pager, new ReactionList
                                              {
                                                  Forward = true,
                                                  Backward = true,
                                                  Trash = true
                                              });
        }
        
        [Command("Ranks", RunMode = RunMode.Async)]
        [Summary("Display all ranks")]
        public Task ViewRanksAsync()
        {
            var rankList = Context.Server.Ranks.OrderByDescending(r => r.Threshold).Select(
                r =>
                    {
                        var name = Context.Guild.GetRole(r.RoleID)?.Mention ?? $"[{r.RoleID}]";
                        var scoreInfo = $"{r.Threshold.ToString().PadRight(10)} +{(r.WinModifier == 0 ? Context.Server.Settings.Registration.DefaultWinModifier : r.WinModifier).ToString().PadRight(10)} -{(r.LossModifier == 0 ? Context.Server.Settings.Registration.DefaultLossModifier : r.LossModifier).ToString().PadRight(10)}\u200B";
                        return $"`{scoreInfo}` - {name}";
                    }).ToList();

            return SimpleEmbedAsync($"`Threshold  +Win        -Lose      \u200B `\n{string.Join("\n", rankList)}");
        }

        [CheckLobby]
        [Command("LastGame", RunMode = RunMode.Async)]
        [Summary("Displays the last game in the current lobby")]
        public Task LastGameAsync()
        {
            return ShowGameAsync(Context.Channel.Id, Context.Server.Results.Where(x => x.LobbyID == Context.Channel.Id).Max(x => x.GameNumber));
        }
        
        [Command("ShowGame", RunMode = RunMode.Async)]
        [Summary("Displays the a game from the given lobby")]
        public Task ShowGameAsync(ITextChannel lobbyChannel, int gameNumber)
        {
            return ShowGameAsync(lobbyChannel.Id, gameNumber);
        }

        [CheckLobby]
        [Command("ShowGame", RunMode = RunMode.Async)]
        [Summary("Displays the a game from the current lobby")]
        public Task ShowGameAsync(int gameNumber)
        {
            return ShowGameAsync(Context.Channel.Id, gameNumber);
        }
        
        [Command("GameList", RunMode = RunMode.Async)]
        [Summary("Shows all games from the given lobby")]
        public Task GameListAsync(ITextChannel lobbyChannel)
        {
            return GameListAsync(lobbyChannel.Id);
        }

        [CheckLobby]
        [Command("GameList", RunMode = RunMode.Async)]
        [Summary("Shows all games from the current lobby")]
        public Task GameListAsync()
        {
            return GameListAsync(Context.Channel.Id);
        }

        public Task GameListAsync(ulong channelId)
        {
            var lobbyResults = Context.Server.Results.Where(x => x.LobbyID == channelId);
            if (!lobbyResults.Any())
            {
                throw new Exception("There are no game results for the supplied channel");
            }

            var games = Context.Server.Results.Where(x => x.LobbyID == channelId).OrderByDescending(x => x.GameNumber).ToList().SplitList(20);
            var pages = games.Select(x => { return new PaginatedMessage.Page { Description = string.Join("\n", x.Select(g => $"`#{g.GameNumber.ToString()}` - {g.Result}")) }; });
            return PagedReplyAsync(new PaginatedMessage { Pages = pages, Title = $"{Context.Guild.GetChannel(channelId)?.Name} Games" }, new ReactionList { Forward = true, Backward = true, Trash = true });
        }

        [CheckLobby]
        [Command("Comment")]
        [Summary("Comments on a game from the current lobby")]
        public Task CommentAsync(int gameNumber, [Remainder] string comment)
        {
            var game = Context.Server.Results.FirstOrDefault(l => l.LobbyID == Context.Channel.Id && l.GameNumber == gameNumber);

            if (game == null)
            {
                throw new Exception("Invalid Game Number or lobby");
            }

            if (comment.Length > 150)
            {
                throw new Exception($"Comments must be less than 150 characters long (Current = {comment.Length})");
            }

            game.Comments.Add(new GuildModel.GameResult.Comment
                                  {
                                      Content = comment,
                                      CommenterID = Context.User.Id,
                                      ID = game.Comments.Count
                                  });

            Context.Server.Save();
            return SimpleEmbedAsync($"Success, {Context.User.Mention} commented on game #{gameNumber}\n" + $"{comment}");
        }

        public Task ShowGameAsync(ulong lobbyId, int gameNumber)
        {
            var game = Context.Server.Results.FirstOrDefault(l => l.LobbyID == lobbyId && l.GameNumber == gameNumber);
            if (game == null)
            {
                throw new Exception("Invalid game");
            }

            string resultProposalInfo = null;
            if (game.Proposal != new GuildModel.GameResult.ResultProposal())
            {
                if (game.Proposal.R1 != GuildModel.GameResult._Result.Undecided || game.Proposal.R2 != GuildModel.GameResult._Result.Undecided)
                {
                    resultProposalInfo = "**Result Proposal:**\n" + $"{Context.Guild.GetUser(game.Proposal.P1)?.Mention ?? $"[{game.Proposal.P1}]"} {game.Proposal.R1}\n" + $"{Context.Guild.GetUser(game.Proposal.P2)?.Mention ?? $"[{game.Proposal.P2}]"} {game.Proposal.R2}\n\n";
                }
            }

            if (game.Comments.Any())
            {
                var pages = new List<PaginatedMessage.Page>
                                {
                                    new PaginatedMessage.Page
                                        {
                                            Color = Color.Blue,
                                            Title = $"{Context.Channel.Name} - #{game.GameNumber}",
                                            Description = $"**Team 1:**\n{string.Join(" ", game.Team1.Select(x => Context.Guild.GetUser(x)?.Mention ?? $"[{x}]"))}\n\n" + 
                                                          $"**Team 2:**\n{string.Join(" ", game.Team2.Select(x => Context.Guild.GetUser(x)?.Mention ?? $"[{x}]"))}\n\n" +
                                                          $"Result:\n{game.Result}\n\n" + 
                                                          $"{resultProposalInfo}" + 
                                                          "**For Comments, react with arrows.** (:arrow_backward: :arrow_forward:)"
                                        }
                                };
                foreach (var commentGroup in game.Comments.OrderByDescending(x => x.ID).ToList().SplitList(5))
                {
                    pages.Add(new PaginatedMessage.Page
                                  {
                                      Description = $"{string.Join("\n", commentGroup.Select(c => $"`{c.ID}` - {Context.Guild.GetUser(c.CommenterID)?.Mention}\n{c.Content}"))}"
                                  });
                }

                return PagedReplyAsync(new PaginatedMessage
                                            {
                                                Pages = pages
                                            }, new ReactionList
                                                  {
                                                      Forward = true,
                                                      Backward = true,
                                                      Trash = true
                                                  }, false);
            }

            return ReplyAsync(
                new EmbedBuilder
                    {
                        Color = Color.Blue,
                        Title = $"{Context.Channel.Name} - #{game.GameNumber}",
                        Description =
                            $"**Team 1:** {string.Join(" ", game.Team1.Select(x => Context.Guild.GetUser(x)?.Mention ?? $"[{x}]"))}\n\n"
                            + $"**Team 2:** {string.Join(" ", game.Team2.Select(x => Context.Guild.GetUser(x)?.Mention ?? $"[{x}]"))}\n\n"
                            + $"Result:\n{game.Result}\n\n" + $"{resultProposalInfo}"
                    });
        }
    }
}
