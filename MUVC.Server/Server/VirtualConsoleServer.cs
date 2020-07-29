using MUVC.Server.Class;
using MUVC.Server.Util;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MUVC.Server
{
    public class VirtualConsoleServer
    {
        #region ctor

        public VirtualConsoleServer(int Port)
        {
            running = false;
            port = Port;
            timeToLiveSeconds = -1;
        }

        public VirtualConsoleServer(int Port, int BS)
        {
            running = false;
            port = Port;
            BUF_SIZE = BS;
            timeToLiveSeconds = -1;
        }

        #endregion

        #region fields

        private bool running = false;
        private int port;
        private int BUF_SIZE = 10007;
        private ConcurrentMessageQueue INqueue = new ConcurrentMessageQueue();
        private Dictionary<Sesion, ConcurrentMessageQueue> OUTqueue = new Dictionary<Sesion, ConcurrentMessageQueue>();
        private List<Sesion> sesions = new List<Sesion>();
        private bool eventpush = false;

        public int timeToLiveSeconds { get; set; }
        public delegate void recMessage(string messageText, Sesion sesion);
        public event recMessage MessageRecieved;


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
            running = false;
        }

        public bool AvailableRead()
        {
            //TODO
            return true;
        }

        public string ReadLine(out Sesion sesion)
        {
            CheckEx();
            Message m = INqueue.BlockingDequeue();
            sesion = m.Sesion;
            return m.Contents;
        }

        public bool AvailableReadFilter()
        {
            //TODO
            return true;
        }

        public string ReadLineFilter(Sesion filter)
        {
            CheckEx();
            return INqueue.BlockingFilteredDequeue(filter).Contents;
        }

        public void WriteLine(string line, Sesion destination)
        {
            CheckEx();
            CheckDestination(destination);
            lock (OUTqueue)
            {
                OUTqueue[destination].Enqueue(new Message(line, destination));
            }
        }

        public void BroadcastLine(string line)
        {
            CheckEx();
            lock (OUTqueue)
            {
                foreach (KeyValuePair<Sesion, ConcurrentMessageQueue> kvp in OUTqueue)
                    kvp.Value.Enqueue(new Message(line, kvp.Key));
            }
        }

        public Sesion[] GetSesions()
        {
            CheckEx();
            lock (sesions)
            {
                return sesions.ToArray();
            }
        }

        public void TerminateSesion(Sesion sesion)
        {
            CheckEx();
            WriteLine(ServerUtil.DISCONNECT_STRING, sesion);
        }

        public void SetEventPush(bool ep)
        {
            eventpush = ep;
        }

        #endregion

        #region threads

        private void ServerThread()
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

        //TODO: Optimize
        private void ClientThread(object data)
        {
            TcpClient client = (TcpClient)data;
            Sesion sesion = new Sesion((IPEndPoint)client.Client.RemoteEndPoint);
            ConcurrentMessageQueue OUTQ = new ConcurrentMessageQueue();
            lock (sesion)
            {
                sesions.Add(sesion);
            }
            lock (OUTqueue)
            {
                OUTqueue[sesion] = OUTQ;
            }
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[BUF_SIZE];
            int bufPos = 0;
            int toRead = 0;
            sesion.LastSeen = DateTime.Now.Ticks;
            sesion.Notified = false;

            Log.WriteLine("Client Opened:" + sesion.Address);

            while (client.Connected && running)
            {
                toRead = client.Available;
                if (toRead > 0)
                {
                    sesion.LastSeen = DateTime.Now.Ticks;
                    sesion.Notified = false;
                    buffer[bufPos++] = (byte)stream.ReadByte();
                    if (buffer[bufPos - 1] == '\n')
                    {
                        string recString = Encoding.ASCII.GetString(buffer, 0, bufPos - 1);
                        bufPos = 0;
                        recString = recString.Trim();

                        //TODO: Da li je prazna poruka valida i da li last seen zavisi od poslednjeg karaktera ili poruke also keepalive u jedinicama koje nisu sekunde

                        Log.WriteLine(sesion.Address + ">" + recString);

                        if (recString.StartsWith(ServerUtil.MUVC_STRING))
                        {
                            if (recString == ServerUtil.DISCONNECT_STRING)
                            {
                                client.Close();
                                lock (OUTqueue)
                                {
                                    OUTqueue.Remove(sesion);
                                }
                                lock (sesions)
                                {
                                    sesions.Remove(sesion);
                                }

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
                            if (MessageRecieved == null || eventpush)
                            {
                                INqueue.Enqueue(new Message(recString, sesion));
                            }
                            MessageRecieved?.Invoke(recString, sesion);

                        }
                    }
                }
                if (!OUTQ.IsEmpty())
                {
                    string trString = OUTQ.Dequeue().Contents;
                    byte[] sendbuf = Encoding.ASCII.GetBytes(trString + "\n");
                    stream.Write(sendbuf, 0, sendbuf.Length);
                    if (trString == ServerUtil.DISCONNECT_STRING)
                    {
                        client.Close();
                        lock (OUTqueue)
                        {
                            OUTqueue.Remove(sesion);
                        }
                        lock (sesions)
                        {
                            sesions.Remove(sesion);
                        }

                        Log.WriteLine("Client Terminated:" + sesion.Address);

                        return;
                    }

                    Log.WriteLine(sesion.Address + "<" + trString);
                }
                if (timeToLiveSeconds >= 0 && (DateTime.Now.Ticks - sesion.LastSeen) / ServerUtil.SECOND_TICKS > timeToLiveSeconds)
                {
                    if (sesion.Notified)
                    {
                        client.Close();
                        lock (OUTqueue)
                        {
                            OUTqueue.Remove(sesion);
                        }
                        lock (sesions)
                        {
                            sesions.Remove(sesion);
                        }

                        Log.WriteLine("Client Timed Out:" + sesion.Address);

                        return;
                    }
                    else
                    {
                        byte[] Ksendbuf = Encoding.ASCII.GetBytes(ServerUtil.KEEPALIVE_STRING + "\n");
                        stream.Write(Ksendbuf, 0, Ksendbuf.Length);
                        sesion.LastSeen = DateTime.Now.Ticks;
                        sesion.Notified = true;

                        Log.WriteLine("Client Notified:" + sesion.Address);
                    }
                }

            }
            byte[] Dsendbuf = Encoding.ASCII.GetBytes(ServerUtil.DISCONNECT_STRING + "\n");
            stream.Write(Dsendbuf, 0, Dsendbuf.Length);
            client.Close();
            lock (OUTqueue)
            {
                OUTqueue.Remove(sesion);
            }
            lock (sesions)
            {
                sesions.Remove(sesion);
            }

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
            lock (sesions)
            {
                if (!sesions.Contains(destination))
                {
                    throw new NoDestinationException();
                }
            }
        }

        #endregion
    }
}
