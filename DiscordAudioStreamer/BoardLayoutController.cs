using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscordAudioStreamer
{
    class BoardLayoutController
    {
        MixingSampleProvider _mixer;
        Dictionary<Guid, BoardGroupController> _groupControllers = new Dictionary<Guid, BoardGroupController>();
        Dictionary<Guid, BoardResource> _resources = new Dictionary<Guid, BoardResource>();

        public BoardLayoutController(string content)
        {
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(22050, 2))
            {
                ReadFully = true
            };
            WaveProvider = _mixer.ToWaveProvider();

            Deserialize(content);
        }

        public BoardLayout Layout { get; private set; }
        public IWaveProvider WaveProvider { get; }

        public BoardGroupController GetGroupController(Guid id)
        {
            return _groupControllers[id];
        }

        public BoardResource GetResource(Guid id)
        {
            return _resources[id];
        }

        public void Deserialize(string content)
        {
            Layout = JsonSerializer.Deserialize<BoardLayout>(content);

            foreach (var group in Layout.Groups)
            {
                var groupController = new BoardGroupController(group);
                _mixer.AddMixerInput(groupController.Mixer);
                _groupControllers[groupController.Group.ID] = groupController;

                foreach (var resource in group.Resources)
                {
                    _resources[resource.ID] = resource;
                }
            }
        }
    }
}
