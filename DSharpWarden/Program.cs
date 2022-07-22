using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;



namespace DHashtagWarden
{
    class Program
    {
        static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {

            var discordClient = new DiscordClient(new DiscordConfiguration()
            {
                Token = "MTAwMDAzNTU5OTkyOTA1MzIxNA.GbK2is.UO-yUR4dFxj57FjgGmWTIi3TvfJddRq1QIZLyY",
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged
            });


            discordClient.MessageCreated += async (s, e) =>
            {

                if (e.Message.Content.ToLower().Contains("hello"))
                {
                    await e.Message.RespondAsync(e.Author.Username);

                }

            };



            await discordClient.ConnectAsync();
            await Task.Delay(-1);





        }

    }
}
