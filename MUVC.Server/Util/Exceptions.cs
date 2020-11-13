using System;

namespace MUVC.Server.Util
{
    [Serializable]
    public class NotStartedException : Exception
    {
        public NotStartedException() { }
        public NotStartedException(string message) : base(message) { }
        public NotStartedException(string message, Exception inner) : base(message, inner) { }
        protected NotStartedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class NoDestinationException : Exception
    {
        public NoDestinationException() { }
        public NoDestinationException(string message) : base(message) { }
        public NoDestinationException(string message, Exception inner) : base(message, inner) { }
        protected NoDestinationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
