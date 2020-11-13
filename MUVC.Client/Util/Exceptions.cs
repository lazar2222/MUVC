using System;

namespace MUVC.Client.Util
{
    [Serializable]
    public class NotConnectedException : Exception
    {
        public NotConnectedException() { }
        public NotConnectedException(string message) : base(message) { }
        public NotConnectedException(string message, Exception inner) : base(message, inner) { }
        protected NotConnectedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class AlreadyConnectedException : Exception
    {
        public AlreadyConnectedException() { }
        public AlreadyConnectedException(string message) : base(message) { }
        public AlreadyConnectedException(string message, Exception inner) : base(message, inner) { }
        protected AlreadyConnectedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
