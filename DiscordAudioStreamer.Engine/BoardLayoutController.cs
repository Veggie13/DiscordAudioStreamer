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
    public class BoardLayoutController : IBoardLayout
    {
        MixingSampleProvider _mixer;
        Dictionary<Guid, BoardGroupController> _groupControllers = new Dictionary<Guid, BoardGroupController>();
        Dictionary<Guid, BoardResourceController> _resourceControllers = new Dictionary<Guid, BoardResourceController>();

        public BoardLayoutController(BoardLayout layout)
        {
            Layout = layout;

            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(22050, 2))
            {
                ReadFully = true
            };
            WaveProvider = _mixer.ToWaveProvider();

            foreach (var group in Layout.Groups)
            {
                var groupController = new BoardGroupController(group);
                _mixer.AddMixerInput(groupController.Mixer);
                _groupControllers[groupController.Group.ID] = groupController;

                foreach (var resource in group.Resources)
                {
                    _resourceControllers[resource.ID] = groupController.GetResourceController(resource.ID);
                }
            }
        }

        public BoardLayout Layout { get; }
        public IWaveProvider WaveProvider { get; }

        public BoardGroupController GetGroupController(Guid id)
        {
            return _groupControllers[id];
        }
        IBoardGroup IBoardLayout.GetGroupController(Guid id) => GetGroupController(id);

        public BoardResourceController GetResourceController(Guid id)
        {
            return _resourceControllers[id];
        }
        IBoardResource IBoardLayout.GetResourceController(Guid id) => GetResourceController(id);
    }
}
