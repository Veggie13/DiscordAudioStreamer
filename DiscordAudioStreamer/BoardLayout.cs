using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
