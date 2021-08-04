using Discord.Audio;
using NAudio.Utils;
using NAudio.Wave;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DiscordAudioStreamer
{
    class DiscordWaveOut : IWavePlayer
    {
        AudioOutStream _voiceStream;

        IWaveProvider _waveProvider;
        Task _outputTask;
        bool _running = false;

        public DiscordWaveOut(IAudioClient client)
        {
            _voiceStream = client.CreatePCMStream(AudioApplication.Music);
        }

        public float Volume { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public PlaybackState PlaybackState { get; private set; } = PlaybackState.Stopped;

        public event EventHandler<StoppedEventArgs> PlaybackStopped;

        public void Dispose()
        {
            _running = false;
            _outputTask?.Wait();
            _voiceStream?.Dispose();
        }

        public void Init(IWaveProvider waveProvider)
        {
            _waveProvider = waveProvider;
            _outputTask = Task.Run(outputTask);
        }

        public void Pause()
        {
            if (PlaybackState == PlaybackState.Playing)
            {
                PlaybackState = PlaybackState.Paused;
            }
        }

        public void Play()
        {
            PlaybackState = PlaybackState.Playing;
        }

        public void Stop()
        {
            if (PlaybackState != PlaybackState.Stopped)
            {
                PlaybackState = PlaybackState.Stopped;
                PlaybackStopped(this, new StoppedEventArgs());
            }
        }

        void outputTask()
        {
            const int blockRateOut = 48000;
            const int channelsOut = 2;
            const int bytesPerSampleOut = 2;
            const int blockSizeOut = channelsOut * bytesPerSampleOut;
            const int bitsPerSampleOut = bytesPerSampleOut * 8;
            const int sampleRateOut = blockRateOut * channelsOut;
            const int byteRateOut = sampleRateOut * bytesPerSampleOut;

            _running = true;

            try
            {
                var voiceFormat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, blockRateOut, channelsOut, byteRateOut, blockSizeOut, bitsPerSampleOut);
                using (var resampler = new MediaFoundationResampler(_waveProvider, voiceFormat))
                using (var writer = new BinaryWriter(new IgnoreDisposeStream(_voiceStream)))
                {
                    var transcodeBuf = new byte[256];

                    while (_running)
                    {
                        if (PlaybackState == PlaybackState.Playing)
                        {
                            try
                            {
                                int transcodeBytes = resampler.Read(transcodeBuf, 0, transcodeBuf.Length);
                                if (transcodeBytes > 0)
                                {
                                    writer.Write(transcodeBuf, 0, transcodeBytes);
                                }
                            }
                            catch
                            { }
                        }
                    }
                }
            }
            finally
            {
                PlaybackState = PlaybackState.Stopped;
            }
        }
    }
}
