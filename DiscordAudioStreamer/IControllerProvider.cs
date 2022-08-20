namespace DiscordAudioStreamer
{
    interface IControllerProvider
    {
        string Name { get; }
        IBoardLayout GetLayoutController();
        void Reload();
        void Shutdown();
    }
}
