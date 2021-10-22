using CSCore.SoundIn;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
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

            using (var capture = new WasapiLoopbackCapture())
            {
                //initialize the selected device for recording
                capture.Initialize();
                int bytesPerBlockIn = capture.WaveFormat.BlockAlign;
                int bytesPerSampleIn = capture.WaveFormat.BytesPerSample;
                int channelsIn = capture.WaveFormat.Channels;
                int rateIn = capture.WaveFormat.SampleRate;

                using (var connection = await getConnection())
                using (var voiceStream = connection.CreatePCMStream(Discord.Audio.AudioApplication.Music))
                {
                    var transcodeBuf = new byte[8 * 1024];
                    int channelsOut = 2;
                    int bytesPerSampleOut = 2;
                    int bytesPerBlockOut = bytesPerSampleOut * channelsOut;
                    int rateOut = rateIn/*voiceChannel.Bitrate / 8 / bytesPerBlockOut*/;
                    int rateRatio = rateIn / rateOut;
                    int bytesPerUsedBlockIn = bytesPerBlockIn * rateRatio;
                    Console.WriteLine($"In:  BLOCK[{bytesPerBlockIn}] SAMPLE[{bytesPerSampleIn}] CHANNELS[{channelsIn}] RATE[{rateIn}]");
                    Console.WriteLine($"Out: BLOCK[{bytesPerBlockOut}] SAMPLE[{bytesPerSampleOut}] CHANNELS[{channelsOut}] RATE[{rateOut}]");
                    Console.WriteLine($"LE: {BitConverter.IsLittleEndian}");

                    //setup an eventhandler to receive the recorded data
                    capture.DataAvailable += (s, e) =>
                    {
                        int transcodeBytes = Math.Min(transcodeBuf.Length, (e.ByteCount / bytesPerUsedBlockIn) * bytesPerBlockOut);
                        for (int i = 0, j = e.Offset; i < transcodeBytes; i += bytesPerBlockOut, j += bytesPerUsedBlockIn)
                        {
                            for (int k = 0; k < channelsOut; k++)
                            {
                                var sample = BitConverter.ToSingle(e.Data, j + k * bytesPerSampleIn);
                                short transcode = (short)(sample * Int16.MaxValue);
                                Array.Copy(BitConverter.GetBytes(transcode), 0, transcodeBuf, i + k * bytesPerSampleOut, bytesPerSampleOut);
                            }
                        }

                        //save the recorded audio
                        try
                        {
                            voiceStream.WriteAsync(transcodeBuf, 0, transcodeBytes).Wait();
                            voiceStream.FlushAsync();
                        }
                        catch
                        {
                        }
                    };

                    //start recording
                    Console.WriteLine("Starting capture.");
                    capture.Start();
                    Console.WriteLine("Capture started.");

                    while (_capturing) ;

                    Console.WriteLine("Stopping capture.");
                    capture.Stop();
                    Console.WriteLine("Capture stopped.");
                }
            }

            Console.WriteLine($"Disconnecting from channel {_currentChannel.Id}");
            await _currentChannel.DisconnectAsync();
            Console.WriteLine($"Disconnected from channel {_currentChannel.Id}");
            _streaming = false;
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
