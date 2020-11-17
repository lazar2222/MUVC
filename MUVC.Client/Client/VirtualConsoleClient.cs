using MUVC.Client.Util;
using MUVC.Core.Util;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MUVC.Client
{
    public class VirtualConsoleClient
    {
        #region ctor

        public VirtualConsoleClient()
        {
            running = false;
            _BufferSize = CoreUtil.STANDARD_BUFFER_SIZE;
            _TimeToLiveSeconds = -1;
        }

        public VirtualConsoleClient(int BS)
        {
            running = false;
            _BufferSize = BS;
            _TimeToLiveSeconds = -1;
        }

        public VirtualConsoleClient(int BS, double TTL)
        {
            running = false;
            _BufferSize = BS;
            _TimeToLiveSeconds = TTL;
        }

        #endregion

        #region fields

        private bool running = false;
        private int _BufferSize;
        private double _TimeToLiveSeconds;
        private bool _EventPush = false;
        private Queue<string> INqueue = new Queue<string>();
        private Queue<string> OUTqueue = new Queue<string>();
        private Random random = new Random();

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
                    throw new AlreadyConnectedException("Cannot modify while connected");
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
                    throw new AlreadyConnectedException("Cannot modify while connected");
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
                    throw new AlreadyConnectedException("Cannot modify while connected");
                }
            }
        }

        public delegate void recMessage(string messageText);
        public event recMessage MessageRecieved = null;

        #endregion

        #region methods

        public void Connect(IPEndPoint ipe)
        {
            if (!running)
            {
                running = true;
                new Thread(ClientThread).Start(ipe);
            }
        }

        public void Disconnect()
        {
            running = false;
        }

        public void Reset()
        {
            if (!running)
            {
                INqueue = new Queue<string>();
                OUTqueue = new Queue<string>();
            }
            else
            {
                throw new AlreadyConnectedException("Cannot reset while connected");
            }
        }

        public bool AvailableRead()
        {
            lock (INqueue)
            {
                return INqueue.Count != 0;
            }
        }

        public string ReadLine()
        {
            while (true)
            {
                lock (INqueue)
                {
                    if (INqueue.Count > 0)
                    {
                        return INqueue.Dequeue();
                    }
                }
            }
        }

        public void WriteLine(string line)
        {
            CheckEx();
            lock (OUTqueue)
            {
                OUTqueue.Enqueue(line);
            }
        }

        #endregion

        #region threads

        private void ClientThread(object data)
        {
            TcpClient client = new TcpClient();
            IPEndPoint ipe = (IPEndPoint)data;

            try
            {
                client.Connect(ipe);
            }
            catch
            {
                throw new FailedToConnectException("Socket error");
            }
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[_BufferSize];
            byte[] sendbuf;
            string msg;
            int bufPos = 0;
            long LastSeen = DateTime.Now.Ticks;
            random.NextDouble();
            double cTTL = _TimeToLiveSeconds * (random.NextDouble() + 1);
            //bool Notified = false;

            Log.WriteLine("Connected to:" + ipe.Address);

            while (client.Connected && running)
            {
                if (client.Available > 0)
                {
                    buffer[bufPos++] = (byte)stream.ReadByte();
                    if (buffer[bufPos - 1] == CoreUtil.MESSAGE_TERMINATOR)
                    {
                        LastSeen = DateTime.Now.Ticks;
                        cTTL = _TimeToLiveSeconds * (random.NextDouble() + 1);
                        //Notified = false;
                        msg = Encoding.ASCII.GetString(buffer, 0, bufPos - 1);
                        bufPos = 0;

                        Log.WriteLine(ipe.Address + ">" + msg);

                        if (msg.StartsWith(CoreUtil.MUVC_STRING))
                        {
                            if (msg == CoreUtil.DISCONNECT_STRING)
                            {
                                client.Close();

                                Log.WriteLine("Server DSC:" + ipe.Address);

                                return;
                            }
                            //else if (msg == CoreUtil.KEEPALIVE_STRING)
                            //{
                            //    sendbuf = Encoding.ASCII.GetBytes(CoreUtil.ACKNOWLEDGE_STRING + CoreUtil.MESSAGE_TERMINATOR);
                            //    safeWrite(stream, sendbuf, 0, sendbuf.Length);

                            //    Log.WriteLine("Responded to server keepalive:" + ipe.Address);
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
                                lock (INqueue)
                                {
                                    INqueue.Enqueue(msg);
                                }
                            }
                            handler?.Invoke(msg);
                        }
                    }
                }
                lock (OUTqueue)
                {
                    if (OUTqueue.Count != 0)
                    {
                        msg = OUTqueue.Dequeue();
                        sendbuf = Encoding.ASCII.GetBytes(msg + CoreUtil.MESSAGE_TERMINATOR);
                        SafeWrite(stream, sendbuf, 0, sendbuf.Length);
                        LastSeen = DateTime.Now.Ticks;
                        cTTL = _TimeToLiveSeconds * (random.NextDouble() + 1);
                        if (msg == CoreUtil.DISCONNECT_STRING)
                        {
                            client.Close();

                            Log.WriteLine("Client sent disconnect (this should not have happened):" + ipe.Address);

                            return;
                        }

                        Log.WriteLine(ipe.Address + "<" + msg);
                    }
                }
                if (_TimeToLiveSeconds >= 0 && (DateTime.Now.Ticks - LastSeen) / CoreUtil.TICKS_PER_SECOND > cTTL)
                {
                    //if (Notified)
                    //{
                    //    client.Close();

                    //    Log.WriteLine("Server Timed Out:" + ipe.Address);

                    //    return;
                    //}
                    //else
                    //{
                    sendbuf = Encoding.ASCII.GetBytes(CoreUtil.KEEPALIVE_STRING + CoreUtil.MESSAGE_TERMINATOR);
                    SafeWrite(stream, sendbuf, 0, sendbuf.Length);
                    LastSeen = DateTime.Now.Ticks;
                    cTTL = _TimeToLiveSeconds * (random.NextDouble() + 1);
                    //Notified = true;

                    Log.WriteLine("Server Notified:" + ipe.Address);
                    //}
                }
            }
            sendbuf = Encoding.ASCII.GetBytes(CoreUtil.DISCONNECT_STRING + CoreUtil.MESSAGE_TERMINATOR);
            SafeWrite(stream, sendbuf, 0, sendbuf.Length);
            client.Close();

            Log.WriteLine("Client Closed:" + ipe.Address);
        }

        #endregion

        #region misc

        private void CheckEx()
        {
            if (!running)
            {
                throw new NotConnectedException();
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
