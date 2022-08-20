using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordAudioStreamer
{
    public interface IBoardLayout
    {
        BoardLayout Layout { get; }

        IBoardGroup GetGroupController(Guid id);
        IBoardResource GetResourceController(Guid id);
    }
}
