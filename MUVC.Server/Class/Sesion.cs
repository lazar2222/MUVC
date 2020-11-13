using System.Collections.Generic;
using System.Net;

namespace MUVC.Server.Class
{
    public class Sesion
    {
        public Sesion(IPEndPoint ipep)
        {
            identifier = ipep;
            Context = new Dictionary<string, object>();
        }

        IPEndPoint identifier;

        public Dictionary<string, object> Context { get; }
        public int Port { get { return identifier.Port; } }
        public IPAddress Address { get { return identifier.Address; } }
        public long LastSeen { get; set; }
        public bool Notified { get; set; }
    }
}
