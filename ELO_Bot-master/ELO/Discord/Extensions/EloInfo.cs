namespace ELO.Discord.Extensions
{
    using System;

    using ELO.Models;

    public class EloInfo
    {
        public static string[] GameResults()
        {
            return Enum.GetNames(typeof(GuildModel.GameResult._Result));
        }

        public static string[] MapTypes()
        {
            return Enum.GetNames(typeof(GuildModel.Lobby.MapSelector));
        }
    }
}
