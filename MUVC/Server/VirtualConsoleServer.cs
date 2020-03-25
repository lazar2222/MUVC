using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MUVC.Class;
using MUVC.UTIL;

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

        private bool running = false;
        private int port;
        private int BUF_SIZE = 10007;
        private ConcurrentQueue<Message> INqueue = new ConcurrentQueue<Message>();
        private ConcurrentDictionary<Sesion, ConcurrentQueue<string>> OUTqueue = new ConcurrentDictionary<Sesion, ConcurrentQueue<string>>();
        private ConcurrentQueue<string> BCSqueue = new ConcurrentQueue<string>();
        private List<Sesion> sesions = new List<Sesion>();

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

        public string ReadLine(out Sesion sesion)
        {
            CheckEx();
            while (INqueue.IsEmpty) { }
            Message m;
            INqueue.TryDequeue(out m);
            sesion = m.Sesion;
            return m.Contents;
        }

        public string ReadLineFilter(Sesion filter)
        {
            CheckEx();
            while (INqueue.Where((x) => x.Sesion == filter).Count() < 1) { }
            return INqueue.Where((x) => x.Sesion == filter).First().Contents;
        }

        public void WriteLine(string line,Sesion destination)
        {
            CheckEx();
            CheckDestination(destination);
            OUTqueue[destination].Enqueue(line);
        }

        public void BroadcastLine(string line)
        {
            CheckEx();
            BCSqueue.Enqueue(line);
        }

        public List<Sesion> GetSesions()
        {
            CheckEx();
            return sesions;
        }
        
        public void TerminateSesion(Sesion sesion)
        {
            CheckEx();
            WriteLine(Util.DISCONNECT_STRING, sesion);
        }

        #endregion

        #region threads

        void ServerThread()
        {
            TcpListener TcpServer = new TcpListener(IPAddress.Any, port);
            TcpServer.Start();

            Log.WriteLine("TCP Opened");

            while (running)
            {
                if (TcpServer.Pending())
                {
                    TcpClient client = TcpServer.AcceptTcpClient();
                    new Thread(ClientThread).Start(client);
                }
            }
            TcpServer.Stop();

            Log.WriteLine("TCP Closed");
        }

        void ClientThread(object data)
        {
            TcpClient client = (TcpClient)data;
            Sesion sesion = new Sesion((IPEndPoint)client.Client.RemoteEndPoint);
            sesions.Add(sesion);
            OUTqueue[sesion] = new ConcurrentQueue<string>();
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[BUF_SIZE];
            int bufPos = 0;
            int toRead = 0;

            Log.WriteLine("Client Opened:" + sesion.Address);

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

                        Log.WriteLine(sesion.Address + ">" + recString);

                        if (recString.StartsWith(Util.MUVC_STRING))
                        {
                            if (recString == Util.DISCONNECT_STRING)
                            {
                                client.Close();
                                ConcurrentQueue<string> b;
                                OUTqueue.TryRemove(sesion, out b);
                                sesions.Remove(sesion);

                                Log.WriteLine("Client DSC:" + sesion.Address);

                                return;
                            }
                            else
                            {
                                //handle MUVC commands
                            }
                        }
                        else
                        {
                            INqueue.Enqueue(new Message(recString, sesion));
                        }
                    }
                }
                if (OUTqueue[sesion].Count > 0)
                {
                    string trString;
                    OUTqueue[sesion].TryDequeue(out trString);
                    byte[] sendbuf = Encoding.ASCII.GetBytes(trString + "\n");
                    stream.Write(sendbuf, 0, sendbuf.Length);
                    if (trString == Util.DISCONNECT_STRING)
                    {
                        client.Close();
                        ConcurrentQueue<string> c;
                        OUTqueue.TryRemove(sesion, out c);
                        sesions.Remove(sesion);

                        Log.WriteLine("Client Terminated:" + sesion.Address);
                    }

                    Log.WriteLine(sesion.Address + "<" + trString);
                }
                if (BCSqueue.Count > 0)
                {
                    string trString;
                    BCSqueue.TryDequeue(out trString);
                    byte[] sendbuf = Encoding.ASCII.GetBytes(trString + "\n");
                    stream.Write(sendbuf, 0, sendbuf.Length);

                    Log.WriteLine(sesion.Address + "<" + trString);
                }

            }
            byte[] Dsendbuf = Encoding.ASCII.GetBytes(Util.DISCONNECT_STRING + "\n");
            stream.Write(Dsendbuf, 0, Dsendbuf.Length);
            client.Close();
            ConcurrentQueue<string> a;
            OUTqueue.TryRemove(sesion,out a);
            sesions.Remove(sesion);

            Log.WriteLine("Client Closed:" + sesion.Address);
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

        private void CheckDestination(Sesion destination)
        {
            if (!sesions.Contains(destination))
            {
                throw new NoDestinationException();
            }
        }

        #endregion
    }
}
