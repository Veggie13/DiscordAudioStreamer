using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
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

        public BoardLayoutController LayoutController { get; set; }
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
            string listing = getListing();
            await sayToRoomAsync(listing);
        }

        Task playAsync(SocketUser user, ISocketMessageChannel channel, string[] args)
        {
            if (args.Length >= 2)
            {
                int groupIndex = int.Parse(args[0]);
                int resIndex = int.Parse(args[1]);
                var layout = LayoutController.Layout;
                if (groupIndex < layout.Groups.Count && resIndex < layout.Groups[groupIndex].Resources.Count)
                {
                    var resourceController = LayoutController.GetResourceController(layout.Groups[groupIndex].Resources[resIndex].ID);
                    resourceController.Trigger();
                }
            }

            return Task.CompletedTask;
        }

        private string getListing()
        {
            var layout = LayoutController.Layout;
            var sb = new StringBuilder();
            for (int j = 0; j < layout.Groups.Count; j++)
            {
                var group = layout.Groups[j];

                sb.AppendLine($"{j} - {group.Heading}:");
                for (int i = 0; i < group.Resources.Count; i++)
                {
                    sb.AppendLine($" - - {i} - {group.Resources[i].Text}");
                }
            }

            return sb.ToString();
        }
    }
}
