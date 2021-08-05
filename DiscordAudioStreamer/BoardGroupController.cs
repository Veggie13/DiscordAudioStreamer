using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordAudioStreamer
{
    public class BoardGroupController
    {
        MixingSampleProvider _mixer;
        VolumeSampleProvider _volume;

        public BoardGroupController(BoardGroup group)
        {
            Group = group;

            Group.Start += group_Start;
            Group.Stop += group_Stop;

            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(22050, 2))
            {
                ReadFully = true
            };
            _volume = new VolumeSampleProvider(_mixer)
            {
                Volume = 1.0f
            };
        }

        public BoardGroup Group { get; }
        public ISampleProvider Mixer { get { return _volume; } }
        public int Volume
        {
            set
            {
                _volume.Volume = (value / 100.0f);
            }
        }

        private void group_Stop()
        {
            _mixer.RemoveAllMixerInputs();
        }

        private void group_Start(bool looping, BoardResource resource)
        {
            var reader = new AudioFileReader(resource.Filename);
            var stream = new LoopStream(reader)
            {
                EnableLooping = looping
            };
            var resampler = new MediaFoundationResampler(stream, _mixer.WaveFormat);
            _mixer.AddMixerInput(resampler);
        }
    }
}
