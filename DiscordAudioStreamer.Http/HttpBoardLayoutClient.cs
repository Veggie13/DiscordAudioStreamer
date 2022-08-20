using System;
using System.Collections.Generic;

namespace DiscordAudioStreamer
{
    internal class HttpBoardLayoutClient : IBoardLayout
    {
        HttpClient _client;
        Dictionary<Guid, HttpBoardGroupClient> _groupClients = new Dictionary<Guid, HttpBoardGroupClient>();
        Dictionary<Guid, HttpBoardResourceClient> _resourceClients = new Dictionary<Guid, HttpBoardResourceClient>();

        public HttpBoardLayoutClient(HttpClient client, BoardLayout layout)
        {
            _client = client;
            Layout = layout;

            foreach (var group in Layout.Groups)
            {
                var groupClient = new HttpBoardGroupClient(_client, group);
                _groupClients[group.ID] = groupClient;

                foreach (var resource in group.Resources)
                {
                    _resourceClients[resource.ID] = groupClient.GetResourceController(resource.ID);
                }
            }
        }

        public BoardLayout Layout { get; }

        public IBoardGroup GetGroupController(Guid id)
        {
            return _groupClients[id];
        }

        public IBoardResource GetResourceController(Guid id)
        {
            return _resourceClients[id];
        }
    }
}
