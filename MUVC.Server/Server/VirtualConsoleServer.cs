using MUVC.Core.Util;
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

        public VirtualConsoleServer(int port)
        {
            running = false;
            _Port = port;
            _BufferSize = CoreUtil.STANDARD_BUFFER_SIZE;
            _TimeToLiveSeconds = -1;
        }

        public VirtualConsoleServer(int port, int BS)
        {
            running = false;
            _Port = port;
            _BufferSize = BS;
            _TimeToLiveSeconds = -1;
        }

        public VirtualConsoleServer(int port, int BS, double TTL)
        {
            running = false;
            _Port = port;
            _BufferSize = BS;
            _TimeToLiveSeconds = TTL;
        }

        #endregion

        #region fields

        private bool running = false;
        private int _Port;
        private int _BufferSize;
        private double _TimeToLiveSeconds;
        private bool _EventPush = false;
        private ConcurrentMessageQueue INqueue = new ConcurrentMessageQueue();
        private Dictionary<Sesion, ConcurrentMessageQueue> OUTqueue = new Dictionary<Sesion, ConcurrentMessageQueue>();
        private List<Sesion> sesions = new List<Sesion>();

        public int Port
        {
            get
            {
                return _Port;
            }
            set
            {
                if (!running)
                {
                    _Port = value;
                }
                else
                {
                    throw new InvalidOperationException("Server is running");
                }
            }
        }

        public int BufferSize
        {
            get
            {
                return _BufferSize;
            }
            set
            {
                if (!running)
                {
                    _BufferSize = value;
                }
                else
                {
                    throw new InvalidOperationException("Server is running");
                }
            }
        }

        public double TimeToLiveSeconds
        {
            get
            {
                return _TimeToLiveSeconds;
            }
            set
            {
                if (!running)
                {
                    _TimeToLiveSeconds = value;
                }
                else
                {
                    throw new InvalidOperationException("Server is running");
                }
            }
        }

        public bool EventPush
        {
            get
            {
                return _EventPush;
            }
            set
            {
                if (!running)
                {
                    _EventPush = value;
                }
                else
                {
                    throw new InvalidOperationException("Server is running");
                }
            }
        }

        public delegate void recMessage(string messageText, Sesion sesion);
        public event recMessage MessageRecieved = null;

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

        public void Reset()
        {
            if (!running)
            {
                INqueue = new ConcurrentMessageQueue();
                OUTqueue = new Dictionary<Sesion, ConcurrentMessageQueue>();
                sesions = new List<Sesion>();
            }
            else
            {
                throw new InvalidOperationException("Server is running");
            }
        }

        public bool AvailableRead()
        {
            return !INqueue.IsEmpty();
        }

        public string ReadLine(out Sesion sesion)
        {
            Message m = INqueue.BlockingDequeue();
            sesion = m.Sesion;
            return m.Contents;
        }

        public bool AvailableReadFilter(Sesion sesion)
        {
            return INqueue.Contains(sesion);
        }

        public string ReadLineFilter(Sesion filter)
        {
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
            WriteLine(CoreUtil.DISCONNECT_STRING, sesion);
        }

        #endregion

        #region threads

        private void ServerThread()
        {
            TcpListener TcpServer = new TcpListener(IPAddress.Any, _Port);
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

        /*
        while (client.Connected && running)
            {
                int toRead = client.Available;
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

                        Log.WriteLine(sesion.Address + ">" + recString);

                        if (recString.StartsWith(CoreUtil.MUVC_STRING))
                        {
                            if (recString == CoreUtil.DISCONNECT_STRING)
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
                    if (trString == CoreUtil.DISCONNECT_STRING)
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
                if (TimeToLiveSeconds >= 0 && (DateTime.Now.Ticks - sesion.LastSeen) / CoreUtil.TICKS_PER_SECOND > TimeToLiveSeconds)
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
                        byte[] Ksendbuf = Encoding.ASCII.GetBytes(CoreUtil.KEEPALIVE_STRING + "\n");
                        stream.Write(Ksendbuf, 0, Ksendbuf.Length);
                        sesion.LastSeen = DateTime.Now.Ticks;
                        sesion.Notified = true;

                        Log.WriteLine("Client Notified:" + sesion.Address);
                    }
                }

            }
            byte[] Dsendbuf = Encoding.ASCII.GetBytes(CoreUtil.DISCONNECT_STRING + "\n");
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
        */

        private void ClientThread(object data)
        {
            TcpClient client = (TcpClient)data;
            Sesion sesion = new Sesion((IPEndPoint)client.Client.RemoteEndPoint);
            ConcurrentMessageQueue OUTQ = new ConcurrentMessageQueue();
            Random random = new Random();
            lock (sesion)
            {
                sesions.Add(sesion);
            }
            lock (OUTqueue)
            {
                OUTqueue[sesion] = OUTQ;
            }
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[_BufferSize];
            byte[] sendbuf;
            string msg;
            int bufPos = 0;
            sesion.LastSeen = DateTime.Now.Ticks;
            double cTTL = _TimeToLiveSeconds * (random.NextDouble() + 1);
            //sesion.Notified = false;

            Log.WriteLine("Client Opened:" + sesion.Address);

            while (client.Connected && running)
            {
                if (client.Available > 0)
                {
                    buffer[bufPos++] = (byte)stream.ReadByte();
                    if (buffer[bufPos - 1] == CoreUtil.MESSAGE_TERMINATOR)
                    {
                        sesion.LastSeen = DateTime.Now.Ticks;
                        cTTL = _TimeToLiveSeconds * (random.NextDouble() + 1);
                        //sesion.Notified = false;
                        msg = Encoding.ASCII.GetString(buffer, 0, bufPos - 1);
                        bufPos = 0;

                        Log.WriteLine(sesion.Address + ">" + msg);

                        if (msg.StartsWith(CoreUtil.MUVC_STRING))
                        {
                            if (msg == CoreUtil.DISCONNECT_STRING)
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
                            //else if (msg == CoreUtil.KEEPALIVE_STRING)
                            //{
                            //    sendbuf = Encoding.ASCII.GetBytes(CoreUtil.ACKNOWLEDGE_STRING + CoreUtil.MESSAGE_TERMINATOR);
                            //    safeWrite(stream, sendbuf, 0, sendbuf.Length);

                            //    Log.WriteLine("Responded to client keepalive:" + sesion.Address);
                            //}
                            else
                            {
                                //TODO handle other MUVC commands
                            }
                        }
                        else
                        {
                            recMessage handler = MessageRecieved;
                            if (handler == null || _EventPush)
                            {
                                INqueue.Enqueue(new Message(msg, sesion));
                            }
                            handler?.Invoke(msg, sesion);
                        }
                    }
                }
                if (!OUTQ.IsEmpty())
                {
                    msg = OUTQ.Dequeue().Contents;
                    sendbuf = Encoding.ASCII.GetBytes(msg + CoreUtil.MESSAGE_TERMINATOR);
                    SafeWrite(stream, sendbuf, 0, sendbuf.Length);
                    sesion.LastSeen = DateTime.Now.Ticks;
                    cTTL = _TimeToLiveSeconds * (random.NextDouble() + 1);
                    if (msg == CoreUtil.DISCONNECT_STRING)
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

                    Log.WriteLine(sesion.Address + "<" + msg);
                }
                if (_TimeToLiveSeconds >= 0 && (DateTime.Now.Ticks - sesion.LastSeen) / CoreUtil.TICKS_PER_SECOND > cTTL)
                {
                    //if (sesion.Notified)
                    //{
                    //    client.Close();
                    //    lock (OUTqueue)
                    //    {
                    //        OUTqueue.Remove(sesion);
                    //    }
                    //    lock (sesions)
                    //    {
                    //        sesions.Remove(sesion);
                    //    }

                    //    Log.WriteLine("Client Timed Out:" + sesion.Address);

                    //    return;
                    //}
                    //else
                    //{
                    sendbuf = Encoding.ASCII.GetBytes(CoreUtil.KEEPALIVE_STRING + CoreUtil.MESSAGE_TERMINATOR);
                    SafeWrite(stream, sendbuf, 0, sendbuf.Length);
                    sesion.LastSeen = DateTime.Now.Ticks;
                    cTTL = _TimeToLiveSeconds * (random.NextDouble() + 1);
                    //sesion.Notified = true;

                    Log.WriteLine("Client Notified:" + sesion.Address);
                    //}
                }
            }
            sendbuf = Encoding.ASCII.GetBytes(CoreUtil.DISCONNECT_STRING + CoreUtil.MESSAGE_TERMINATOR);
            SafeWrite(stream, sendbuf, 0, sendbuf.Length);
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

        private void SafeWrite(NetworkStream stream, byte[] buffer, int offset, int size)
        {
            try
            {
                stream.Write(buffer, offset, size);
            }
            catch
            {
                //Log.WriteLine("Failed to send");
            }
        }

        #endregion
    }
}
