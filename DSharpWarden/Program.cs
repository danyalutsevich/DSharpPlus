using System;
using System.Threading;
using System.IO;

using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.VoiceNext;
using DSharpWarden;

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
                Token = "MTAwMDAzNTU5OTkyOTA1MzIxNA.GTEvFI.PVLaho7quwMzsVqOIGydHtGY4DxvHvhHIzLc5M",
                Intents = DiscordIntents.AllUnprivileged,
                Ending = " \nthis is ending",
                BlackList = Utilities.GetBlackList(@"BlackList.txt"),

            });

            var commandsConfig =new CommandsNextConfiguration
            {
                StringPrefixes = new string[]{"."},
                CaseSensitive = false,
                EnableDms = false,
                EnableMentionPrefix = true,
                DmHelp = true
            };


            var commands=discordClient.UseCommandsNext(commandsConfig);

            commands.RegisterCommands<Commands>();


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

            var voice = discordClient.UseVoiceNext(new VoiceNextConfiguration { });


            await discordClient.ConnectAsync();
            await Task.Delay(-1);

        }

    }
}
