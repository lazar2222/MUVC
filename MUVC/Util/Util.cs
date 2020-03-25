using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MUVC.UTIL
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

    public class Util
    {
        public const string MUVC_STRING= "MUVC";
        public const string DISCONNECT_STRING= "MUVC DSC";

        public static string[] SmartSplit(string input)
        {
            List<string> split = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            List<string> res = new List<string>();

            for (int i = 0; i < split.Count; i++)
            {
                if (split[i][0] == '"' || split[i][0] == '\'' || split[i][0] == '`')
                {
                    char del = split[i][0];
                    string merge = split[i].Substring(1).Trim();
                    i++;
                    while (i < split.Count)
                    {
                        if (split[i][split[i].Length - 1] == del)
                        {
                            merge += split[i].Substring(0, split[i].Length - 1).Trim();
                            i++;
                            break;
                        }
                        else
                        {
                            merge += split[i];
                        }
                    }
                    res.Add(merge);
                }
                else
                {
                    res.Add(split[i]);
                }
            }

            return res.ToArray();
        }
    }

    public class Log
    {
        public static bool LOG = false;

        public static void WriteLine(string line)
        {
            if (LOG)
            {
                ConsoleColor pre = Console.ForegroundColor;
                Console.ForegroundColor=ConsoleColor.Green;
                Console.WriteLine("LOG: " + line);
                Console.ForegroundColor = pre;
            }
        }
    }
}
