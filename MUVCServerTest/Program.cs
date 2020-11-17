using MUVC.Core.Util;
using MUVC.Server;
using MUVC.Server.Class;
using System;

namespace MUVCServerTest
{
    class Program
    {
        public static VirtualConsoleServer Server;

        static void Main(string[] args)
        {
            Log.LOG = true;
            Server = new VirtualConsoleServer(53777)
            {
                TimeToLiveSeconds = 10
            };
            Server.MessageRecieved += Server_MessageRecieved;
            Server.Start();
            Console.ReadLine();
            Server.Stop();
            Console.ReadLine();
        }

        private static void Server_MessageRecieved(string messageText, Sesion sesion)
        {
            if (messageText.StartsWith("nick:"))
            {
                sesion.Context["nick"] = messageText.Substring(messageText.IndexOf(':') + 1);
            }
            else if (sesion.Context.ContainsKey("nick"))
            {

                if (messageText.StartsWith("dm"))
                {
                    string target = messageText.Substring(messageText.IndexOf(' ') + 1, messageText.IndexOf(':') - messageText.IndexOf(' ') - 1);
                    string message = messageText.Substring(messageText.IndexOf(':') + 1);
                    foreach (Sesion item in Server.GetSesions())
                    {
                        if ((string)item.Context["nick"] == target)
                        {
                            Server.WriteLine("DM from " + sesion.Context["nick"] + ":" + message, item);
                        }
                    }
                }
                else
                {
                    Server.BroadcastLine(sesion.Context["nick"] + ":" + messageText);
                }
            }
            else
            {
                Server.WriteLine("You must set a nickname", sesion);
            }
        }
    }
}
