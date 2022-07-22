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
                Token = "...your token",
                Intents = DiscordIntents.AllUnprivileged,
                Ending = " \nthis is ending",
                BlackList = Utilities.GetBlackList(@"BlackList.txt"),
                
            });

            discordClient.MessageCreated += async (s, e) =>
            {
                if (e.Message.Content.ToLower().Contains("hello"))
                {
                    await e.Message.RespondAsync(e.Author.Username);
                    
                }
            };

            discordClient.MessageInBlackList += async (s, e) =>
            {

                await e.Message.RespondAsync("you violating our rules you should be cancelled");

            };
           

            
            await discordClient.ConnectAsync();
            await Task.Delay(-1);

        }

    }
}
