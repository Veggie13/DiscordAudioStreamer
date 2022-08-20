namespace DiscordAudioStreamer
{
    class RemoteControllerProvider : IControllerProvider
    {
        HttpClient _httpClient;

        public RemoteControllerProvider(string remote)
        {
            _httpClient = new HttpClient(remote);
            Name = remote;
        }

        public string Name { get; }

        public IBoardLayout GetLayoutController() => _httpClient.GetLayoutControllerAsync().Result;

        public void Reload()
        {
            _httpClient.ReloadAsync().Wait();
        }

        public void Shutdown()
        {

        }
    }
}
