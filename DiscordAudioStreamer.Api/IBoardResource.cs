namespace DiscordAudioStreamer
{
    public interface IBoardResource
    {
        BoardResource Resource { get; }

        void Trigger();
    }
}
