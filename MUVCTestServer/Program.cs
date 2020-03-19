using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MUVC.Server;

namespace MUVCTestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            VirtualConsoleServer virtualConsole = new VirtualConsoleServer(1010);
            virtualConsole.Start();
        }
    }
}
