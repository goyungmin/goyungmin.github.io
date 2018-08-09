namespace ELO.Modules
{
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using ELO.Discord.Context;
    using ELO.Models;

    using global::Discord;
    using global::Discord.Addons.Interactive;
    using global::Discord.Commands;

    [RequireOwner]
    public class BotOwner : Base
    {
        /*[Command("Migrate", RunMode = RunMode.Async)]
        [Summary("Migrates from the last file based config system (350) to RavenDB")]
        public Task Migrate()
        {
            ConfigParser.Parse();
            return Task.CompletedTask;
        }*/

        [Command("CreateKeys", RunMode = RunMode.Async)]
        [Summary("Creates premium keys")]
        public Task CreateKeysAsync(int keyCount, int days)
        {
                if (keyCount > 100)
                {
                    throw new Exception("Cannot Create more than 100 keys at a time");
                }

                return InlineReactionReplyAsync(
                    new ReactionCallbackData(
                        "",
                        new EmbedBuilder
                            {
                                Description =
                                    $"Do you wish to create {keyCount} keys, each with {days} days?"
                            }.Build(),
                        true,
                        true,
                        TimeSpan.FromSeconds(30)).WithCallback(
                        new Emoji("✅"),
                        async (c, r) =>
                            {
                                var tokenModel = TokenModel.Load();
                                var sb = new StringBuilder();
                                for (var i = 0; i < keyCount; i++)
                                {
                                    var token =
                                        $"{GenerateRandomNo()}-{GenerateRandomNo()}-{GenerateRandomNo()}-{GenerateRandomNo()}";
                                    if (tokenModel.TokenList.Any(x => x.Token == token))
                                    {
                                        continue;
                                    }

                                    tokenModel.TokenList.Add(
                                        new TokenModel.TokenClass { Token = token, Days = days });
                                    sb.AppendLine(token);
                                }

                                tokenModel.Save();
                                await SimpleEmbedAsync("Complete");
                                await SimpleEmbedAsync($"New Tokens\n```\n{sb.ToString()}\n```");
                                sb.Clear();
                            }).WithCallback(
                        new Emoji("❎"),
                        (c, r) => SimpleEmbedAsync("Exited Token Task")));
        }

        private readonly Random random = new Random();

        public string GenerateRandomNo()
        {
            return random.Next(0, 9999).ToString("D4");
        }
    }
}
