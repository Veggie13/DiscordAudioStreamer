namespace DiscordAudioStreamer
{
    internal class HttpBoardResourceClient : IBoardResource
    {
        HttpClient _client;

        public HttpBoardResourceClient(HttpClient client, BoardResource resource)
        {
            _client = client;
            Resource = resource;
        }

        public BoardResource Resource { get; }

        public void Trigger()
        {
            _client.PostAsync($"RES {Resource.ID}").Wait();
        }
    }
}
