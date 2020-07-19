using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MUVC.Server;
using MUVC.Class;

namespace MUVC.Executor
{
    class Executor
    {
        public Executor(VirtualConsoleServer Server)
        {
            server = Server;
            server.MessageRecieved += Server_MessageRecieved;
        }

        private void Server_MessageRecieved(string messageText, Sesion sesion)
        {
            foreach (Process process in Processors)
            {
                string res = process(messageText, sesion);
                if (res != null)
                {
                    server.WriteLine(res, sesion);
                }
            }
        }

        public delegate string Process(string messageText, Sesion sesion);

        private VirtualConsoleServer server;
        private List<Process> Processors = new List<Process>();

        public void addProcessor(Process p)
        {
            Processors.Add(p);
        }

        public void removeProcesssor(Process p)
        {
            Processors.Remove(p);
        }

    }
}
