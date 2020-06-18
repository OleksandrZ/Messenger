using MessengerServer;
using System;

namespace ConsoleApp3
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Server server = new Server();
            server.Start();
            Console.ReadKey();
            server.Stop();
        }
    }
}