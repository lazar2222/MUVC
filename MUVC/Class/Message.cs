using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MUVC.Class
{
    class Message
    {
        public Message() { }
        public Message(string text, Sesion s)
        {
            Contents = text;
            Sesion = s;
        }

        public string Contents { get; set; }
        public Sesion Sesion { get; set; }
    }
}
