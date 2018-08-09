namespace ELO.Discord.Extensions
{
    using global::Discord;
    using ELO.Discord.Context;
    
    public class BotInfo
    {
        public static string GetInvite(Context context)
        {
            return GetInvite(context.Client);
        }

        public static string GetInvite(IDiscordClient client)
        {
            return $"https://discordapp.com/oauth2/authorize?client_id={client.CurrentUser.Id}&scope=bot&permissions=2146958591";
        }
    }
}