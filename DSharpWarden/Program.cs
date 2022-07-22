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
                Token = "MTAwMDAzNTU5OTkyOTA1MzIxNA.GdmBqr.nn-LijJUQlpn1__bKZttGbfINoowNmjt2vxooI",
                Intents = DiscordIntents.AllUnprivileged,
                Ending = "das",
            });

            //new DiscordConfiguration()
            //{
                
            //}


            discordClient.MessageCreated += async (s, e) =>
            {

                if (e.Message.Content.ToLower().Contains("hello"))
                {
                    await e.Message.RespondAsync(e.Author.Username);
                    //s.SendMessageAsync()

                }

            };



            await discordClient.ConnectAsync();
            await Task.Delay(-1);





        }

    }
}
