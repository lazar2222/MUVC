using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MUVC.Util;

namespace MUVC.Server
{
    public class VirtualConsoleServer
    {
        #region ctor

        public VirtualConsoleServer(int Port)
        {
            running = false;
            port = Port;
        }

        public VirtualConsoleServer(int Port, int BS)
        {
            running = false;
            port = Port;
            BUF_SIZE = BS;
        }

        #endregion

        #region fields

        private bool running;
        private int port;
        private int BUF_SIZE = 10007;

        #endregion

        #region methods

        public void Start()
        {
            if (!running)
            {
                running = true;
                new Thread(ServerThread).Start();
            }
        }

        public void Stop()
        {
            if (running)
            {
                running = false;
            }
        }

        #endregion

        #region threads

        void ServerThread()
        {
            TcpListener TcpServer = new TcpListener(IPAddress.Any, port);
            TcpServer.Start();
            //Console.WriteLine("TCP Opened");
            while (running)
            {
                if (TcpServer.Pending())
                {
                    TcpClient client = TcpServer.AcceptTcpClient();
                    new Thread(ClientThread).Start(client);
                }
            }
            TcpServer.Stop();
            //Console.WriteLine("TCP Closed");
        }

        void ClientThread(object data)
        {
            TcpClient client = (TcpClient)data;
            IPAddress address = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
            //Console.WriteLine("Client Opened:" + address);
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[BUF_SIZE];
            int bufPos = 0;
            int toRead = 0;
            string res = null;
            while (client.Connected && running)
            {
                toRead = client.Available;
                if (toRead > 0)
                {
                    buffer[bufPos++] = (byte)stream.ReadByte();
                    if (buffer[bufPos - 1] == '\n')
                    {
                        string recString = Encoding.ASCII.GetString(buffer, 0, bufPos - 1);
                        bufPos = 0;
                        recString = recString.Trim();
                        //Console.WriteLine(((IPEndPoint)client.Client.RemoteEndPoint).Address + ">" + s);
                        if (recString == "MUVC DSC")
                        {
                            client.Close();
                        }
                        else
                        {
                            //res = Executor.Execute(s);
                            //if (res != null)
                            //{
                            //    Console.WriteLine(((IPEndPoint)client.Client.RemoteEndPoint).Address + "<" + res);
                            //    byte[] bb = Encoding.ASCII.GetBytes(res + "\r\n");
                            //    ns.Write(bb, 0, bb.Length);

                            //}
                        }
                    }
                }
            }
            client.Close();
            //Console.WriteLine("Client Closed:" + address);
        }

        #endregion

        #region misc

        private void CheckEx()
        {
            if (!running)
            {
                throw new NotStartedException();
            }
        }

        #endregion
    }
}
