using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordAudioStreamer
{
    class Bot : DiscordBot.Bot
    {
        Dictionary<string, Func<SocketUser, ISocketMessageChannel, string[], Task>> _commands;

        public Bot()
        {
            _commands = new Dictionary<string, Func<SocketUser, ISocketMessageChannel, string[], Task>>()
            {
                { "endaudio", endAudioAsync },
                { "joinme", joinMeAsync },
                { "list", listAsync },
                { "play", playAsync }
            };
        }

        public Func<string> ListingProvider { get; set; } = () => "";

        public event Action<int, int> Triggered = (_, _) => { };
        protected override async Task handleCommandAsync(SocketUser user, ISocketMessageChannel channel, string command, string[] args)
        {
            if (_commands.ContainsKey(command))
            {
                await _commands[command](user, channel, args);
            }
        }

        async Task endAudioAsync(SocketUser user, ISocketMessageChannel channel, string[] args)
        {
            await sayToRoomAsync("Bye!");
            Stop();
        }

        async Task joinMeAsync(SocketUser user, ISocketMessageChannel channel, string[] args)
        {
            var voiceChannel = findUserVoiceChannel(user);
            await joinChannelAsync(channel, voiceChannel);
            await sayToRoomAsync("Hi!");
        }

        async Task listAsync(SocketUser user, ISocketMessageChannel channel, string[] args)
        {
            string listing = ListingProvider();
            await sayToRoomAsync(listing);
        }

        Task playAsync(SocketUser user, ISocketMessageChannel channel, string[] args)
        {
            if (args.Length >= 2)
            {
                int groupIndex = int.Parse(args[0]);
                int resIndex = int.Parse(args[1]);
                Triggered(groupIndex, resIndex);
            }

            return Task.CompletedTask;
        }
    }
}
