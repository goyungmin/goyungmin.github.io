namespace ELO.Discord.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ELO.Discord.Context;
    using ELO.Models;

    public class AnnouncementManager
    {
        public static GuildModel.Lobby._MapInfo MapField(GuildModel.Lobby lobby)
        {
            var map = lobby.MapInfo;
            if (!lobby.Maps.Any())
            {
                return map;
            }

            if (lobby.MapMode == GuildModel.Lobby.MapSelector.None)
            {
                return map;
            }
            
            if (lobby.MapMode == GuildModel.Lobby.MapSelector.Random)
            {
                var rnd = new Random();
                map.LastMap = lobby.Maps.OrderByDescending(x => rnd.Next()).FirstOrDefault();
            }

            if (lobby.MapMode == GuildModel.Lobby.MapSelector.NoRepeat)
            {
                var rnd = new Random();
                map.LastMap = lobby.Maps.OrderByDescending(x => rnd.Next()).FirstOrDefault(x => x != map.LastMap) ?? map.LastMap;
            }

            if (lobby.MapMode == GuildModel.Lobby.MapSelector.Cycle)
            {
                map.LastMapIndex++;
                if (map.LastMapIndex >= lobby.Maps.Count)
                {
                    map.LastMapIndex = 0;
                }
                try
                {
                    map.LastMap = lobby.Maps[map.LastMapIndex];
                }
                catch
                {
                    map.LastMap = lobby.Maps[0];
                    map.LastMapIndex = 0;
                }
            }

            return map;
        }

        public static string HostField(Context context, GuildModel.Lobby lobby)
        {
            var ulongs = new List<ulong>();
            ulongs.AddRange(lobby.Game.Team1.Players);
            ulongs.AddRange(lobby.Game.Team2.Players);

            var ePlayers = ulongs.Select(x => context.Server.Users.FirstOrDefault(u => u.UserID == x)).Where(x => x != null).ToList();
            string player;
            switch (lobby.HostSelectionMode)
            {
                case GuildModel.Lobby.HostSelector.None:
                    return null;
                case GuildModel.Lobby.HostSelector.MostPoints:
                    player = context.Guild.GetUser(ePlayers.OrderByDescending(x => x.Stats.Points).FirstOrDefault().UserID)?.Mention;
                    break;
                case GuildModel.Lobby.HostSelector.MostWins:
                    player = context.Guild.GetUser(ePlayers.OrderByDescending(x => x.Stats.Wins).FirstOrDefault().UserID)?.Mention;
                    break;
                case GuildModel.Lobby.HostSelector.HighestWinLoss:
                    player = context.Guild.GetUser(ePlayers.OrderByDescending(x => (double)x.Stats.Wins / x.Stats.Losses).FirstOrDefault().UserID)?.Mention;
                    break;
                case GuildModel.Lobby.HostSelector.Random:
                    player = context.Guild.GetUser(ePlayers.OrderByDescending(x => new Random().Next()).FirstOrDefault().UserID)?.Mention;
                    break;
                default:
                    return null;
            }

            return player ?? "N/A";
        }
    }
}
