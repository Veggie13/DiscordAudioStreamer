namespace DiscordAudioStreamer
{
    class Program
    {
        public static void Main(string[] args)
        {
            var bot = new Bot();
            bot.Run().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
