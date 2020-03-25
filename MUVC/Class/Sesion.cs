using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MUVC.Class
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

    }
}
