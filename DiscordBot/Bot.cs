using Discord;
using Discord.Audio;
using Discord.WebSocket;
using NAudio.Wave;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordBot
{
    public abstract class Bot
    {
        bool _running = false;
        Task _runTask;

        bool _capturing = false;
        WaveOut _capture = null;

        bool _streaming = false;

        public SocketVoiceChannel CurrentVoiceChannel { get; private set; } = null;
        public ISocketMessageChannel CurrentMessageChannel { get; private set; } = null;

        IWaveProvider _input = null;
        public IWaveProvider Input
        {
            get { return _input; }
            set
            {
                if (_capturing)
                {
                    _capture.Stop();
                    _capture.Init(value);
                    _capture.Play();
                }

                _input = value;
            }
        }

        public void Run(string token)
        {
            _runTask = runAsync(token);
        }

        public void Stop()
        {
            endStream();
            _running = false;
            _runTask = null;
        }

        protected async Task joinChannelAsync(ISocketMessageChannel msgChannel, SocketVoiceChannel voiceChannel)
        {
            CurrentVoiceChannel = voiceChannel;
            CurrentMessageChannel = msgChannel;
            await connectToVoiceAsync();
        }

        protected void endStream()
        {
            _capturing = false;
            while (_streaming) ;
            CurrentVoiceChannel = null;
            CurrentMessageChannel = null;
        }

        protected async Task sayToRoomAsync(string msg)
        {
            if (CurrentMessageChannel != null)
            {
                await CurrentMessageChannel.SendMessageAsync(msg);
            }
        }

        protected SocketVoiceChannel findUserVoiceChannel(SocketUser user)
        {
            return user.MutualGuilds
                .SelectMany(g => g.VoiceChannels)
                .FirstOrDefault(vc => vc.Users.Contains(user));
        }

        private async Task runAsync(string token)
        {
            _running = true;

            var client = new DiscordSocketClient();
            client.MessageReceived += client_MessageReceived;
            client.Log += client_Log;

            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            while (_running)
            {
                await Task.Delay(5000);
            }
        }

        private async Task connectToVoiceAsync()
        {
            if (CurrentVoiceChannel == null)
            {
                return;
            }

            _capturing = true;
            _streaming = true;

            try
            {
                using (var connection = await getConnection())
                using (_capture = new WaveOut(connection))
                {
                    _capture.Init(Input);
                    _capture.Play();

                    while (_capturing) ;
                }
            }
            finally
            {
                Console.WriteLine($"Disconnecting from channel {CurrentVoiceChannel.Id}");
                await CurrentVoiceChannel.DisconnectAsync();
                Console.WriteLine($"Disconnected from channel {CurrentVoiceChannel.Id}");
                _streaming = false;
                _capturing = false;
                _capture = null;
            }
        }

        private async Task<IAudioClient> getConnection()
        {
            Console.WriteLine($"Connecting to channel {CurrentVoiceChannel.Id}");
            IAudioClient connection = null;
            while (connection == null)
            {
                try
                {
                    connection = await CurrentVoiceChannel.ConnectAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.GetType().FullName} : {ex.Message}");
                }
            }
            Console.WriteLine($"Connected to channel {CurrentVoiceChannel.Id}");
            return connection;
        }

        private Task client_MessageReceived(SocketMessage msg)
        {
            _ = Task.Run(async () =>
            {
                var commandRegex = new Regex("^!(.+?)( (.*))?$");
                var match = commandRegex.Match(msg.Content);
                if (!match.Success)
                {
                    return;
                }

                string command = match.Groups[1].Value;
                var args = match.Groups[3].Value.Split(' ');

                await handleCommandAsync(msg.Author, msg.Channel, command, args);
            });
            return Task.CompletedTask;
        }

        private Task client_Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        protected abstract Task handleCommandAsync(SocketUser user, ISocketMessageChannel channel, string command, string[] args);
    }
}
