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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net;
using DSharpPlus.Net.Abstractions;
using DSharpPlus.Net.Serialization;
using DSharpPlus.VoiceNext.Opus;
using DSharpPlus.VoiceNext.VoiceGateway.Entities;
using DSharpPlus.VoiceNext.VoiceGateway.Entities.Payloads;

namespace DSharpPlus.VoiceNext
{
    public sealed class VoiceNextExtension : BaseExtension
    {
        public VoiceNextConfiguration Configuration { get; }
        public Dictionary<ulong, VoiceNextConnection> Connections => new(this._connections);
        internal ConcurrentDictionary<ulong, VoiceNextConnection> _connections { get; } = new();

        internal ConcurrentDictionary<ulong, TaskCompletionSource<DiscordVoiceStateUpdate>> _voiceStateUpdates = new();
        internal ConcurrentDictionary<ulong, TaskCompletionSource<DiscordVoiceServerUpdatePayload>> _voiceServerUpdates = new();

        internal VoiceNextExtension(VoiceNextConfiguration? configuration)
        {
            this.Configuration = configuration ?? new(OpusAudioFormat.BestQuality);
        }

        protected internal override void Setup(DiscordClient client)
        {
            if (this.Client != null)
                throw new InvalidOperationException("Extension has already been setup.");
            this.Client = client;
            this.Client.VoiceServerUpdated += this.HandleVoiceServerUpdateAsync;
            this.Client.VoiceStateUpdated += this.HandleVoiceStateUpdateAsync;
        }

        public async Task<VoiceNextConnection> ConnectAsync(DiscordChannel voiceChannel, bool selfMuted = false, bool selfDeafened = true)
        {
            var botPermissions = voiceChannel.PermissionsFor(voiceChannel.Guild.CurrentMember);
            if (voiceChannel.Type != ChannelType.Voice && voiceChannel.Type != ChannelType.Stage)
            {
                throw new ArgumentException($"Cannot connect to a {voiceChannel.Type} channel. The channel type must be {ChannelType.Voice} or {ChannelType.Stage}.", nameof(voiceChannel));
            }
            else if (voiceChannel.Guild == null)
            {
                throw new ArgumentException("Bots can only connect to guild channels.", nameof(voiceChannel));
            }
            else if (this._connections.ContainsKey(voiceChannel.Guild.Id))
            {
                throw new InvalidOperationException("Bots may only join 1 voice or stage channel per guild.");
            }
            else if (!botPermissions.HasPermission(Permissions.AccessChannels | Permissions.UseVoice))
            {
                throw new InvalidOperationException($"The bot must have the {Permissions.AccessChannels} and {Permissions.UseVoice} permissions to connect to a channel.");
            }
            else if (!botPermissions.HasPermission(Permissions.Speak) && !selfMuted)
            {
                throw new InvalidOperationException($"The bot must have the {Permissions.Speak} permission to speak in a voice channel.");
            }
            else if (voiceChannel.UserLimit > voiceChannel.Users.Count && !botPermissions.HasPermission(Permissions.ManageChannels))
            {
                throw new InvalidOperationException($"The voice channel is full and the bot must have the {Permissions.ManageChannels} permission to connect to override the channel user limit.");
            }

            // From the Discord Docs (https://discord.com/developers/docs/topics/voice-connections#connecting-to-voice):
            // > If our request succeeded, the gateway will respond with two events—a Voice State Update event and a Voice Server Update event—meaning your
            // > library must properly wait for both events before continuing. The first will contain a new key, session_id, and the second will provide
            // > voice server information we can use to establish a new voice connection:
            // As such, we will pre-emptively create an event handler for both events, and wait for both to be received before continuing.
            var voiceStateUpdateEvent = new TaskCompletionSource<DiscordVoiceStateUpdate>();
            var voiceServerUpdateEvent = new TaskCompletionSource<DiscordVoiceServerUpdatePayload>();
            this._voiceStateUpdates[voiceChannel.Guild.Id] = voiceStateUpdateEvent;
            this._voiceServerUpdates[voiceChannel.Guild.Id] = voiceServerUpdateEvent;

            // Let the server know we're connecting to a voice channel.
            var voiceDispatch = new GatewayPayload
            {
                OpCode = GatewayOpCode.VoiceStateUpdate,
                Data = new DiscordVoiceStateUpdate
                {
                    GuildId = voiceChannel.Guild.Id,
                    ChannelId = voiceChannel.Id,
                    SelfMute = selfMuted,
                    SelfDeaf = selfDeafened
                }
            };
            await this.Client.WsSendAsync(DiscordJson.SerializeObject(voiceDispatch)).ConfigureAwait(false);

            // Wait for the Voice State Update and Voice Server Update events to be received.
            var voiceStateUpdateTask = await voiceStateUpdateEvent.Task.ConfigureAwait(false); // Task<DiscordVoiceStateUpdate>
            var voiceServerUpdateTask = await voiceServerUpdateEvent.Task.ConfigureAwait(false); // Task<DiscordVoiceServerUpdatePayload>

            var voiceNextConnection = new VoiceNextConnection(this.Client, voiceChannel, this.Configuration, voiceStateUpdateTask, voiceServerUpdateTask);
            await voiceNextConnection.ConnectAsync().ConfigureAwait(false);
            return voiceNextConnection;
        }

        /// <summary>
        /// Executed on joining, moving or disconnecting from voice channels.
        /// </summary>
        private Task HandleVoiceStateUpdateAsync(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
        {
            if (eventArgs.Guild == null || eventArgs.User == null)
            {
                return Task.CompletedTask;
            }
            else if (eventArgs.User.Id == client.CurrentUser.Id)
            {
                if (eventArgs.After.Channel == null && this._connections.TryRemove(eventArgs.Guild.Id, out var activeConnection))
                    activeConnection.Dispose();
                else if (this._connections.TryGetValue(eventArgs.Guild.Id, out var voiceNextConnection))
                    voiceNextConnection.Channel = eventArgs.Channel;
                else if (!string.IsNullOrWhiteSpace(eventArgs.SessionId) && eventArgs.Channel != null && this._voiceStateUpdates.TryRemove(eventArgs.Guild.Id, out var voiceStateUpdateEvent))
                    voiceStateUpdateEvent.SetResult(eventArgs.After);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Executed upon joining a voice channel or when the voice servers have to change.
        /// </summary>
        /// <remarks>
        /// A null endpoint means that the voice server allocated has gone away and is trying to be reallocated. You should attempt to disconnect from the currently connected voice server, and not attempt to reconnect until a new voice server is allocated.
        /// </remarks>
        private Task HandleVoiceServerUpdateAsync(DiscordClient client, VoiceServerUpdateEventArgs eventArgs)
        {
            if (eventArgs.Guild == null)
            {
                return Task.CompletedTask;
            }
            else if (this._connections.TryGetValue(eventArgs.Guild.Id, out var voiceNextConnection))
            {
                voiceNextConnection._voiceServerUpdate = eventArgs;

                // Setup endpoint
                if (eventArgs.Endpoint == null)
                {
                    return voiceNextConnection.DisconnectAsync();
                }
                var endpointIndex = eventArgs.Endpoint.LastIndexOf(':');
                var endpointPort = 443;
                string? endpointHost;
                if (endpointIndex != -1) // Determines if the endpoint is a ip address or a hostname
                {
                    endpointHost = eventArgs.Endpoint.Substring(0, endpointIndex);
                    endpointPort = int.Parse(eventArgs.Endpoint.Substring(endpointIndex + 1));
                }
                else
                {
                    endpointHost = eventArgs.Endpoint;
                }

                voiceNextConnection._webSocketEndpoint = new ConnectionEndpoint
                {
                    Hostname = endpointHost,
                    Port = endpointPort
                };

                voiceNextConnection._shouldResume = false;
                return voiceNextConnection.ReconnectAsync();
            }
            else if (this._voiceServerUpdates.TryRemove(eventArgs.Guild.Id, out var voiceServerUpdateEvent))
            {
                voiceServerUpdateEvent.SetResult(eventArgs);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}
