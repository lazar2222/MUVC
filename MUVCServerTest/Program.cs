using MUVC.Core.Util;
using MUVC.Server;
using System;

namespace MUVCServerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.LOG = true;
            VirtualConsoleServer Server = new VirtualConsoleServer(53777)
            {
                TimeToLiveSeconds = 10
            };
            Server.Start();
            Console.ReadLine();
            Server.Stop();
        }
    }
}
