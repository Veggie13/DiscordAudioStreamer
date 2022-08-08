using Discord;
using Discord.Audio;
using Discord.WebSocket;
using NAudio.Wave;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;

namespace DiscordAudioStreamer
{
    class Bot
    {
        bool _running = false;
        bool _capturing = false;
        bool _streaming = false;
        int _userCount = 0;
        SocketVoiceChannel _currentChannel = null;
        Task _runTask;

        public IWaveProvider Input { get; set; }
        public Func<string> ListingProvider { get; set; } = () => "";

        public event Action<int, int> Triggered = (o, e) => { };

        public void Run()
        {
            _runTask = runAsync();
        }

        public void Stop()
        {
            EndStream();
            _running = false;
            _runTask = null;
        }

        private async Task runAsync()
        {
            _running = true;

            var client = new DiscordSocketClient();
            client.MessageReceived += Client_MessageReceived;
            client.Log += Client_Log;

            string token = ConfigurationManager.AppSettings.Get("token");

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            while (_running)
            {
                await Task.Delay(5000);
            }
        }

        private async Task JoinChannel(SocketVoiceChannel voiceChannel)
        {
            _userCount = 1;
            _currentChannel = voiceChannel;
            await ConnectToVoice();
        }

        private void EndStream()
        {
            _capturing = false;
            while (_streaming) ;
            _currentChannel = null;
        }

        private async Task ConnectToVoice()
        {
            if (_currentChannel == null)
            {
                return;
            }

            _capturing = true;
            _streaming = true;

            try
            {
                using (var connection = await getConnection())
                using (var waveOut = new DiscordWaveOut(connection))
                {
                    waveOut.Init(Input);
                    waveOut.Play();

                    while (_capturing) ;
                }
            }
            finally
            {
                Console.WriteLine($"Disconnecting from channel {_currentChannel.Id}");
                await _currentChannel.DisconnectAsync();
                Console.WriteLine($"Disconnected from channel {_currentChannel.Id}");
                _streaming = false;
                _capturing = false;
            }
        }

        private async Task<IAudioClient> getConnection()
        {
            Console.WriteLine($"Connecting to channel {_currentChannel.Id}");
            IAudioClient connection = null;
            while (connection == null)
            {
                try
                {
                    connection = await _currentChannel.ConnectAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.GetType().FullName} : {ex.Message}");
                }
            }
            Console.WriteLine($"Connected to channel {_currentChannel.Id}");
            return connection;
        }

        private Task Client_MessageReceived(SocketMessage msg)
        {
            _ = Task.Run(async () =>
            {
                var regex = new Regex("^!(.+?)( (.*))?$");
                var match = regex.Match(msg.Content);
                if (!match.Success)
                {
                    return;
                }

                switch (match.Groups[1].Value)
                {
                    case "endaudio":
                        {
                            EndStream();
                            _running = false;
                            break;
                        }
                    case "joinme":
                        {
                            var channel = msg.Author.MutualGuilds
                                .SelectMany(g => g.VoiceChannels)
                                .FirstOrDefault(vc => vc.Users.Contains(msg.Author));
                            await JoinChannel(channel);
                            break;
                        }
                    case "list":
                        {
                            string listing = ListingProvider();
                            await msg.Channel.SendMessageAsync(listing);
                            break;
                        }
                    case "play":
                        {
                            var args = match.Groups[3].Value.Split(' ');
                            if (args.Length >= 2)
                            {
                                int groupIndex = int.Parse(args[0]);
                                int resIndex = int.Parse(args[1]);
                                Triggered(groupIndex, resIndex);
                            }
                            break;
                        }
                    default:
                        break;
                }
            });
            return Task.CompletedTask;
        }

        private Task Client_Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
