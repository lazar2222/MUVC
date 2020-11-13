using System;
using System.Collections.Generic;
using System.Linq;

namespace MUVC.Core.Util
{
    public class CoreUtil
    {
        public static int STANDARD_BUFFER_SIZE = 10007;
        public static int TICKS_PER_SECOND = 1000 * 10000;
        public const string MUVC_STRING = "MUVC";
        public const string DISCONNECT_STRING = "MUVC DSC";
        public const string KEEPALIVE_STRING = "MUVC KAL";
        public const string ACKNOWLEDGE_STRING = "MUVC ACK";
        public const char MESSAGE_TERMINATOR = (char)4;

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
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("LOG: " + line);
                Console.ForegroundColor = pre;
            }
        }
    }
}
