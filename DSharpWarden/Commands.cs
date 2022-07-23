// This file is part of the DSharpPlus project.
//
// Copyright (c) 2015 Mike Santiago
// Copyright (c) 2016-2022 DSharpPlus Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


using System.Speech;
using System.Speech.Recognition;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;



using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.EventArgs;


namespace DSharpWarden
{
    public class Commands : BaseCommandModule
    {
        [Command("connect")]
        public async Task ConnectAsync(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("testing testing 12 12").ConfigureAwait(false);

            await ctx.Member.VoiceState.Channel.ConnectAsync().ConfigureAwait(false);

            var vnext = ctx.Client.GetVoiceNext();

            var connection = vnext.GetConnection(ctx.Guild);
            connection.VoiceReceived += this.VoiceReceiveHandler;
            var transmit = connection.GetTransmitSink();

        }

        [Command("disconnect")]
        public async Task DisconnectAsync(CommandContext ctx)
        {

            var vnext = ctx.Client.GetVoiceNext();
            var connection = vnext.GetConnection(ctx.Guild);
            connection.Disconnect();


            connection.VoiceReceived -= this.VoiceReceiveHandler;
            connection.Dispose();

        }

        private async Task VoiceReceiveHandler(VoiceNextConnection connection, VoiceReceiveEventArgs args)
        {
            var transmit = connection.GetTransmitSink();
            await transmit.WriteAsync(args.PcmData);
        }
    }
}
