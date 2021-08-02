using System.Runtime.InteropServices;

namespace DiscordAudioStreamer
{
    class Program
    {
        public static void Main(string[] args)
        {
            AllocConsole();

            var bot = new Bot();
            bot.Run().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
    }
}
