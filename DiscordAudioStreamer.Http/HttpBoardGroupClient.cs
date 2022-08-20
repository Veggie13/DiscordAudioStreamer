using System;
using System.Collections.Generic;

namespace DiscordAudioStreamer
{
    internal class HttpBoardGroupClient : IBoardGroup
    {
        HttpClient _client;
        Dictionary<Guid, HttpBoardResourceClient> _resourceClients = new Dictionary<Guid, HttpBoardResourceClient>();

        public HttpBoardGroupClient(HttpClient client, BoardGroup group)
        {
            _client = client;
            Group = group;

            foreach (var resource in Group.Resources)
            {
                _resourceClients[resource.ID] = new HttpBoardResourceClient(_client, resource);
            }
        }

        public BoardGroup Group { get; }

        public int Volume
        {
            set
            {
                _client.PostAsync($"VOL {Group.ID} {value}").Wait();
            }
        }

        public HttpBoardResourceClient GetResourceController(Guid id)
        {
            return _resourceClients[id];
        }
        IBoardResource IBoardGroup.GetResourceController(Guid id) => GetResourceController(id);

        public void StopEarly()
        {
            _client.PostAsync($"STOP {Group.ID}").Wait();
        }
    }
}
