using System.IO;

namespace DiscordAudioStreamer
{
    class LocalControllerProvider : IControllerProvider
    {
        string _layoutFile;
        BoardLayoutController _boardLayoutController;
        Bot _bot;
        HttpServer _httpServer;

        public LocalControllerProvider(string layoutFile, string botToken)
        {
            _layoutFile = layoutFile;
            Reload();
            
            _bot = new Bot()
            {
                Input = _boardLayoutController.WaveProvider,
                LayoutController = _boardLayoutController
            };
            _bot.Run(botToken);

            _httpServer = new HttpServer(_boardLayoutController);
            _httpServer.Run();
        }

        public string Name => "server";

        public IBoardLayout GetLayoutController() => _boardLayoutController;

        public void Reload()
        {
            string content = File.ReadAllText(_layoutFile);
            var layout = BoardLayout.Deserialize(content);
            _boardLayoutController = new BoardLayoutController(layout);
        }

        public void Shutdown()
        {
            _bot?.Stop();
            _httpServer?.Stop();
        }
    }
}
