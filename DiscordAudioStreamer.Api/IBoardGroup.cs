using System;

namespace DiscordAudioStreamer
{
    public interface IBoardGroup
    {
        BoardGroup Group { get; }
        int Volume { set; }

        IBoardResource GetResourceController(Guid id);

        void StopEarly();
    }
}
