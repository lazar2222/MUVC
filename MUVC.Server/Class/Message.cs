namespace MUVC.Server.Class
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
