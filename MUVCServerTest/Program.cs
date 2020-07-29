using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MUVC.Server;
using MUVC.Server.Util;

namespace MUVCServerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.LOG = true;
            VirtualConsoleServer Server = new VirtualConsoleServer(53777);
            Server.timeToLiveSeconds = 10;
            Server.Start();
            Console.ReadLine();
            Server.Stop();
        }
    }
}
