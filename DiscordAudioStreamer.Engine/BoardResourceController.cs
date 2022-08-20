﻿using System;

namespace DiscordAudioStreamer
{
    public class BoardResourceController
    {
        public BoardResourceController(BoardResource resource)
        {
            Resource = resource;
        }

        public BoardResource Resource { get; }

        public event Action<BoardResource> Triggered = _ => { };

        public void Trigger()
        {
            Triggered(Resource);
        }
    }
}