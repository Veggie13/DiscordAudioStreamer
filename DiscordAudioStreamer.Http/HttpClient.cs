using System;
using System.Threading.Tasks;
using Client = System.Net.Http.HttpClient;
using Request = System.Net.Http.HttpRequestMessage;
using Method = System.Net.Http.HttpMethod;

namespace DiscordAudioStreamer
{
    public class HttpClient
    {

        public HttpClient(string remote)
            : this(new Uri(remote))
        { }

        public HttpClient(Uri uri)
        {
            Uri = uri;
        }

        public Uri Uri { get; }

        public async Task<IBoardLayout> GetLayoutControllerAsync()
        {
            var client = getClient();
            var request = new Request(Method.Get, Uri);
            var response = await client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();

            var layout = BoardLayout.Deserialize(responseContent);
            return new HttpBoardLayoutClient(this, layout);
        }

        public async Task ReloadAsync()
        {
            await PostAsync("RELOAD"); 
        }

        internal async Task PostAsync(string content)
        {
            var client = getClient();
            var request = new Request(Method.Post, Uri)
            {
                Content = new System.Net.Http.StringContent(content)
            };
            await client.SendAsync(request);
        }

        private Client getClient()
        {
            return new Client()
            {
                BaseAddress = Uri
            };
        }
    }
}
