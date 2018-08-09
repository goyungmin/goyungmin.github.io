namespace ELO.Models.Parser
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using ELO.Handlers;

    using Newtonsoft.Json;

    public class ConfigParser
    {
        public static void Parse()
        {
            var serversave = File.ReadAllText(Servers.EloFile);
            Servers.ServerList = JsonConvert.DeserializeObject<List<Servers.Server>>(serversave);
            foreach (var server in Servers.ServerList)
            {
                try
                {
                    var ranks = server.Ranks.Select(r => new GuildModel.Rank { RoleID = r.RoleId, Threshold = r.Points, LossModifier = r.LossModifier, WinModifier = r.WinModifier, IsDefault = false }).ToList();
                    ranks.Add(new GuildModel.Rank { RoleID = server.RegisterRole, IsDefault = true, LossModifier = 0, WinModifier = 0, Threshold = server.registerpoints });
                    var newObject = new GuildModel();
                    newObject.ID = server.ServerId;
                    newObject.Settings = new GuildModel.GuildSettings { GameSettings = new GuildModel.GuildSettings._GameSettings { AllowNegativeScore = server.AllowNegativeScore, AnnouncementsChannel = server.AnnouncementsChannel, BlockMultiQueuing = server.BlockMultiQueueing, DMAnnouncements = true, RemoveOnAfk = server.Autoremove, UseKd = server.showkd }, Registration = new GuildModel.GuildSettings._Registration { DefaultLossModifier = server.Lossamount, DefaultWinModifier = server.Winamount, DeleteProfileOnLeave = server.DeleteProfileOnLeave, Message = server.Registermessage, RegistrationBonus = server.registerpoints }, Premium = new GuildModel.GuildSettings._Premium { Expiry = server.Expiry == DateTime.MaxValue ? DateTime.MinValue : server.Expiry, IsPremium = server.IsPremium, PremiumKeys = new List<GuildModel.GuildSettings._Premium.Key>() }, Moderation = new GuildModel.GuildSettings._Moderation { AdminRoles = new List<ulong> { server.AdminRole }, ModRoles = new List<ulong> { server.ModRole } } };
                    
                    var lobbies = new List<GuildModel.Lobby>();
                    foreach (var q in server.Queue)
                    {
                        var item = new GuildModel.Lobby();
                        item.CaptainSortMode = Enum.TryParse(q.CaptainSortMode.ToString(), true, out GuildModel.Lobby.CaptainSort cm) ? cm : GuildModel.Lobby.CaptainSort.RandomTop4MostPoints;
                        item.ChannelID = q.ChannelId;
                        item.Description = q.ChannelGametype ?? "ELO LOBBY";
                        item.HostSelectionMode = GuildModel.Lobby.HostSelector.MostPoints;
                        item.Maps = q.Maps?.ToList() ?? new List<string>();
                        item.PickMode = Enum.TryParse(q.PickMode.ToString(), true, out GuildModel.Lobby._PickMode pm) ? pm : GuildModel.Lobby._PickMode.CompleteRandom;
                        item.Game = new GuildModel.Lobby.CurrentGame
                                        {
                                            IsPickingTeams = false,
                                            QueuedPlayerIDs =
                                                new List<ulong>(),
                                            Team1 =
                                                new GuildModel.
                                                    Lobby.
                                                    CurrentGame.
                                                    Team(),
                                            Team2 =
                                                new GuildModel.
                                                    Lobby.
                                                    CurrentGame.
                                                    Team()
                                        };
                        item.GamesPlayed = 0;
                        item.MapMode = GuildModel.Lobby.MapSelector.Random;
                        item.UserLimit = q.UserLimit;
                        lobbies.Add(item);
                    }

                    newObject.Lobbies = lobbies;
                    newObject.Users = server.UserList.Select(u => new GuildModel.User { UserID = u.UserId, Username = u.Username, Stats = new GuildModel.User.Score { Deaths = u.deaths, Draws = u.draws, Kills = u.kills, Losses = u.Losses, Points = u.Points, Wins = u.Wins, GamesPlayed = 0 } }).ToList();
                    newObject.Ranks = ranks;
                    using (var session = DatabaseHandler.Store.OpenSession("ELO_BOT"))
                    {
                        var load = session.Load<GuildModel>($"{server.ServerId}");
                        if (session.Advanced.IsLoaded($"{server.ServerId}") && load != newObject)
                        {
                            session.Advanced.Evict(load);
                            session.Store(newObject, $"{newObject.ID}");
                            session.SaveChanges();
                            Console.WriteLine("Updated " + server.ServerId);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            Console.WriteLine("Complete");
        }
    }
}