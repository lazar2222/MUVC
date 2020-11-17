using MUVC.Client;
using MUVC.Core.Util;
using System;
using System.Net;

namespace MUVCClientTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.LOG = true;
            VirtualConsoleClient Client = new VirtualConsoleClient
            {
                TimeToLiveSeconds = 10
            };
            Client.MessageRecieved += Client_MessageRecieved;
            Client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 53777));
            while (true)
            {
                string c = Console.ReadLine();
                if (c == "EXIT") { break; }
                Client.WriteLine(c);
            }
            Client.Disconnect();
            Console.ReadLine();
        }

        private static void Client_MessageRecieved(string messageText)
        {
            Console.WriteLine(messageText);
        }
    }
}
