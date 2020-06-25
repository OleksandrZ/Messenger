using MessengerServer;

namespace ConsoleApp3
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Server server = new Server();
            server.Start();
        }
    }
}