using System;
using System.Collections.Generic;

namespace MUVC.Server.Class
{
    class ConcurrentMessageQueue
    {
        public ConcurrentMessageQueue()
        {
            ll = new LinkedList<Message>();
        }

        private LinkedList<Message> ll;

        public bool IsEmpty()
        {
            lock (ll)
            {
                return ll.Count == 0;
            }
        }

        public bool Contains(Sesion target)
        {
            lock (ll)
            {
                LinkedListNode<Message> node = ll.First;
                while (node != null)
                {
                    if (node.Value.Sesion == target) { return true; }
                    node = node.Next;
                }
                return false;
            }
        }

        public void Enqueue(Message msg)
        {
            lock (ll)
            {
                ll.AddLast(msg);
            }
        }

        public Message Dequeue()
        {
            lock (ll)
            {
                if (ll.Count == 0) { throw new InvalidOperationException("Queue is empty"); }
                Message msg;
                msg = ll.First.Value;
                ll.RemoveFirst();
                return msg;
            }
        }

        public Message BlockingDequeue()
        {
            while (true)
            {
                lock (ll)
                {
                    if (ll.First != null)
                    {
                        Message msg;
                        msg = ll.First.Value;
                        ll.RemoveFirst();
                        return msg;
                    }
                }
            }
        }

        public Message FilteredDequeue(Sesion target)
        {
            lock (ll)
            {
                if (ll.Count == 0) { throw new InvalidOperationException("Queue is empty"); }
                Message msg;
                LinkedListNode<Message> node = ll.First;
                while (node != null)
                {
                    if (node.Value.Sesion == target)
                    {
                        msg = node.Value;
                        ll.Remove(node);
                        return msg;
                    }
                    node = node.Next;
                }
                throw new InvalidOperationException("Queue does not contain target");
            }
        }

        public Message BlockingFilteredDequeue(Sesion target)
        {
            Message msg;
            LinkedListNode<Message> node;
            while (true)
            {
                lock (ll)
                {

                    node = ll.First;
                    while (node != null)
                    {
                        if (node.Value.Sesion == target)
                        {
                            msg = node.Value;
                            ll.Remove(node);
                            return msg;
                        }
                        node = node.Next;
                    }
                }
            }
        }
    }
}
