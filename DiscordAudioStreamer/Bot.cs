using Discord;
using Discord.Audio;
using Discord.WebSocket;
using NAudio.Wave;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Linq;

namespace DiscordAudioStreamer
{
    class Bot
    {
        bool _running = true;
        bool _capturing = false;
        bool _streaming = false;
        int _userCount = 0;
        SocketVoiceChannel _currentChannel = null;

        public async Task Run()
        {
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
                using (var capture = new WasapiLoopbackCapture())
                {
                    //start recording
                    Console.WriteLine("Starting capture.");
                    capture.StartRecording();
                    Console.WriteLine("Capture started.");

                    using (var connection = await getConnection())
                    using (var waveOut = new DiscordWaveOut(connection))
                    {
                        var captureProvider = new WaveInProvider(capture);
                        waveOut.Init(captureProvider);
                        waveOut.Play();

                        while (_capturing) ;
                    }

                    Console.WriteLine("Stopping capture.");
                    capture.StopRecording();
                    Console.WriteLine("Capture stopped.");
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
            if (msg.Content == "!endaudio")
            {
                EndStream();
                _running = false;
            }
            if (msg.Content == "!joinme")
            {
                var channel = msg.Author.MutualGuilds
                    .SelectMany(g => g.VoiceChannels)
                    .FirstOrDefault(vc => vc.Users.Contains(msg.Author));
                JoinChannel(channel);
            }
            return Task.CompletedTask;
        }

        private Task Client_Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
