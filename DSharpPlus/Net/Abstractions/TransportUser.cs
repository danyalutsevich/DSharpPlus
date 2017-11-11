﻿using Newtonsoft.Json;

namespace DSharpPlus.Net.Abstractions
{
    internal class TransportUser
    {
        [JsonProperty("id")]
        public ulong Id { get; internal set; }

        [JsonProperty("username", NullValueHandling = NullValueHandling.Ignore)]
        public string Username { get; internal set; }

        [JsonProperty("discriminator", NullValueHandling = NullValueHandling.Ignore)]
        internal string Discriminator { get; set; }

        [JsonProperty("avatar", NullValueHandling = NullValueHandling.Ignore)]
        public string AvatarHash { get; internal set; }

        [JsonProperty("bot", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsBot { get; internal set; }

        [JsonProperty("mfa_enabled", NullValueHandling = NullValueHandling.Ignore)]
        public bool? MfaEnabled { get; internal set; }

        [JsonProperty("verified", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Verified { get; internal set; }

        [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email { get; internal set; }

        internal TransportUser() { }

        internal TransportUser(TransportUser other)
        {
            this.Id = other.Id;
            this.Username = other.Username;
            this.Discriminator = other.Discriminator;
            this.AvatarHash = other.AvatarHash;
            this.IsBot = other.IsBot;
            this.MfaEnabled = other.MfaEnabled;
            this.Verified = other.Verified;
            this.Email = other.Email;
        }
    }
}
