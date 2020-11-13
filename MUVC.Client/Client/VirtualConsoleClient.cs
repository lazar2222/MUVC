using MUVC.Client.Util;
using MUVC.Core.Util;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace MUVC.Client
{
    class VirtualConsoleClient
    {
        #region ctor

        public VirtualConsoleClient()
        {
            running = false;
            _bufferSize = CoreUtil.STANDARD_BUFFER_SIZE;
            _timeoutTicks = -1;
            EventPush = false;
            MessageRecieved = null;
        }

        #endregion

        #region fields

        volatile private bool running;
        private int _bufferSize;
        private long _timeoutTicks;
        private Queue<string> InQueue;
        private Queue<string> OutQueue;

        public bool EventPush { get; set; }
        public int BufferSize
        {
            get { return _bufferSize; }
            set
            {
                if (!running)
                {
                    _bufferSize = value;
                }
                else
                {
                    throw new InvalidOperationException("Buffer size can't be changed while connected.");
                }
            }
        }
        public double Timeout
        {
            get
            {
                return ((double)_timeoutTicks) / CoreUtil.TICKS_PER_SECOND;
            }
            set
            {
                if (!running)
                {
                    _timeoutTicks = (long)(value * CoreUtil.TICKS_PER_SECOND);
                }
                else
                {
                    throw new InvalidOperationException("Timeout can't be changed while connected.");
                }
            }
        }
        public delegate void MessageRecievedEvent(string messageText);
        public event MessageRecievedEvent MessageRecieved;

        #endregion

        #region methods

        public void Connect(IPAddress ipa, int port)
        {
            if (!running)
            {
                InQueue = new Queue<string>();
                OutQueue = new Queue<string>();
                new Thread(ClientThread).Start();
            }
            else
            {
                throw new AlreadyConnectedException();
            }
        }

        public void Disconnect()
        {
            running = false;
        }

        public bool AvalableRead()
        {
            CheckEx();
            lock (InQueue)
            {
                return InQueue.Count > 0;
            }
        }

        public string ReadLine()
        {
            CheckEx();
            while (true)
            {
                lock (InQueue)
                {
                    if (InQueue.Count > 0)
                    {
                        return InQueue.Dequeue();
                    }
                }
            }
        }

        public void WriteLine(string line)
        {
            CheckEx();
            lock (OutQueue)
            {
                OutQueue.Enqueue(line);
            }
        }

        #endregion

        #region threads

        private void ClientThread()
        {

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

        #endregion
    }
}
