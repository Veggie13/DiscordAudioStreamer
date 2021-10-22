using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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

            foreach (var resource in Resources)
            {
                resource.Triggered += resource_Triggered;
            }
        }

        public string Heading { get; }
        public bool CanPlaySimultaneously { get; } = false;
        public bool Looped { get; } = false;
        public Guid ID { get; }
        public List<BoardResource> Resources { get; } = new List<BoardResource>();

        public event Action Stop = () => { };
        public event Action<bool, BoardResource> Start = (_, _) => { };

        public void StopEarly()
        {
            Stop();
        }

        private void resource_Triggered(BoardResource resource)
        {
            if (!CanPlaySimultaneously)
            {
                Stop();
            }

            Start(Looped, resource);
        }
    }
}
