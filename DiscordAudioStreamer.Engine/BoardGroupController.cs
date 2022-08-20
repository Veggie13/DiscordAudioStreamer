using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;

namespace DiscordAudioStreamer
{
    public class BoardGroupController
    {
        MixingSampleProvider _mixer;
        VolumeSampleProvider _volume;

        Dictionary<Guid, BoardResourceController> _resourceControllers = new Dictionary<Guid, BoardResourceController>();

        public BoardGroupController(BoardGroup group)
        {
            Group = group;

            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(22050, 2))
            {
                ReadFully = true
            };
            _volume = new VolumeSampleProvider(_mixer)
            {
                Volume = 1.0f
            };

            foreach (var resource in Group.Resources)
            {
                var resourceController = new BoardResourceController(resource);
                resourceController.Triggered += resource_Triggered;
                _resourceControllers[resource.ID] = resourceController;
            }
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

        public BoardResourceController GetResourceController(Guid id)
        {
            return _resourceControllers[id];
        }

        public void StopEarly()
        {
            stop();
        }

        private void resource_Triggered(BoardResource resource)
        {
            if (!Group.CanPlaySimultaneously)
            {
                stop();
            }

            start(Group.Looped, resource);
        }

        private void stop()
        {
            _mixer.RemoveAllMixerInputs();
        }

        private void start(bool looping, BoardResource resource)
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
