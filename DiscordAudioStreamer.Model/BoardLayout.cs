using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DiscordAudioStreamer
{
    public class BoardLayout
    {
        [JsonConstructor]
        public BoardLayout(List<BoardGroup> groups)
        {
            Groups.AddRange(groups);
        }

        public List<BoardGroup> Groups { get; } = new List<BoardGroup>();
    }
}
