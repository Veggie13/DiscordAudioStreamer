using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DiscordAudioStreamer
{
    public class BoardGroup
    {
        [JsonConstructor]
        public BoardGroup(string heading, bool canPlaySimultaneously, bool looped, Guid id, List<BoardResource> resources)
        {
            Heading = heading;
            CanPlaySimultaneously = canPlaySimultaneously;
            Looped = looped;
            ID = id;
            Resources.AddRange(resources);
        }

        public string Heading { get; }
        public bool CanPlaySimultaneously { get; } = false;
        public bool Looped { get; } = false;
        public Guid ID { get; }
        public List<BoardResource> Resources { get; } = new List<BoardResource>();
    }
}
