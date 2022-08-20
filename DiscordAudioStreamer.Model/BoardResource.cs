using System;
using System.Text.Json.Serialization;

namespace DiscordAudioStreamer
{
    public class BoardResource
    {
        [JsonConstructor]
        public BoardResource(string text, string filename, Guid id)
        {
            Text = text;
            Filename = filename;
            ID = id;
        }

        public string Text { get; }
        public string Filename { get; }
        public Guid ID { get; }
    }
}
