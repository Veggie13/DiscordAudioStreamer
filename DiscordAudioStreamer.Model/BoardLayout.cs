using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscordAudioStreamer
{
    public class BoardLayout
    {
        public static BoardLayout Deserialize(string content)
        {
            return JsonSerializer.Deserialize<BoardLayout>(content);
        }

        [JsonConstructor]
        public BoardLayout(List<BoardGroup> groups)
        {
            Groups.AddRange(groups);
        }

        public List<BoardGroup> Groups { get; } = new List<BoardGroup>();
    }
}
