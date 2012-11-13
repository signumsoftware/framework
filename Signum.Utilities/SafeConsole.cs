using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Utilities
{
    public static class SafeConsole
    {
        public static readonly object SyncKey = new object();
        public static bool needToClear = false;

        public static void WriteLine()
        {
            WriteLine("");
        }

        public static void WriteLine(string format, params object[] parameters)
        {
            string str = FormatString(format, parameters);
            lock (SyncKey)
            {
                Console.WriteLine(str);
                needToClear = false;
            }
        }

        public static void Write(string format, params object[] parameters)
        {
            string str = FormatString(format, parameters);

            lock (SyncKey)
                Console.Write(str);
        }

        public static void WriteLineColor(ConsoleColor color, string format, params object[] parameters)
        {
            string str = FormatString(format, parameters);
            lock (SyncKey)
            {
                ConsoleColor old = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(str);
                Console.ForegroundColor = old;
            }
        }

        public static void WriteSameLine(string format, params object[] parameters)
        {
            string str = FormatString(format, parameters);
            lock (SyncKey)
            {
                Console.WriteLine(str.PadChopRight(Console.WindowWidth));
          
                Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop - 1);
                needToClear = true;
            }
        }

        static string FormatString(string format, object[] parameters)
        {
            string s = string.Format(format, parameters);
            if (needToClear)
                s = s.PadRight(Console.WindowWidth - 2);
            return s;
        }

        public static bool Ask(string question)
        {
            return Ask(question, "yes", "no") == "yes";
        }

        public static string Ask(string question, params string[] answers)
        {
            Console.WriteLine(question + " " + answers.ToString("/"));
            var answer = Console.ReadLine().ToLower();

            do
            {

                var result = answers.FirstOrDefault(a => a.StartsWith(answer, StringComparison.CurrentCultureIgnoreCase));
                if (result != null)
                    return result;

                Console.WriteLine("Possible answers: " + answers.ToString("/"));
            } while (true);
        }
    }
}
