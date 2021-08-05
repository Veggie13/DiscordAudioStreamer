using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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

        public event Action<BoardResource> Triggered = _ => { };

        public void Trigger()
        {
            Triggered(this);
        }
    }
}
