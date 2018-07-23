using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Signum.Utilities
{
    public static class SafeConsole
    {
        public static readonly object SyncKey = new object();
        static bool needToClear = false;

        public static void WriteSameLine(string format, params object[] parameters)
        {
            WriteSameLine(string.Format(format, parameters));
        }

        public static void WriteSameLine(string str)
        {
            if (needToClear)
                str = str.PadChopRight(Console.BufferWidth - 1);
            else
                str = str.TryStart(Console.BufferWidth - 1);

            Console.WriteLine(str);

            Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop==0? 0: Console.CursorTop - 1);
            needToClear = true;
        }

        public static void ClearSameLine()
        {
            if (needToClear)
            {
                Console.WriteLine(new string(' ', Console.BufferWidth - 1));

                Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop == 0 ? 0 : Console.CursorTop - 1);
            }
            needToClear = false;
        }

        public static void WriteColor(ConsoleColor color, string format, params object[] parameters)
        {
            WriteColor(color, string.Format(format, parameters));
        }

        public static void WriteColor(ConsoleColor color, char c) => WriteColor(color, c.ToString());
        public static void WriteColor(ConsoleColor color, string str)
        {
            ConsoleColor old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(str);
            Console.ForegroundColor = old;
        }



        public static void WriteLineColor(ConsoleColor color, string format, params object[] parameters)
        {
            WriteLineColor(color, string.Format(format, parameters));
        }

        public static void WriteLineColor(ConsoleColor color, string str)
        {
            ConsoleColor old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(str);
            Console.ForegroundColor = old;
        }

        public static void WriteSameLineColor(ConsoleColor color, string str)
        {
            ConsoleColor old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            WriteSameLine(str);
            Console.ForegroundColor = old;
        }

        public static string AskString(string question, Func<string, string> stringValidator = null)
        {
            Console.Write(question);
            do
            {
                var userAnswer = Console.ReadLine();

                string error = stringValidator == null ? null : stringValidator(userAnswer);
                if (error == null)
                    return userAnswer;

                Console.Write(error);
            } while (true);
        }

        public static bool Ask(string question)
        {
            return Ask(question, "yes", "no") == "yes";
        }

        public static string Ask(string question, params string[] answers)
        {
            Console.Write(question + " ({0}) ".FormatWith(answers.ToString("/")));
            do
            {
                var userAnswer = Console.ReadLine().ToLower();
                var result = answers.FirstOrDefault(a => a.StartsWith(userAnswer, StringComparison.CurrentCultureIgnoreCase));
                if (result != null)
                    return result;

                Console.Write("Possible answers: {0} ".FormatWith(answers.ToString("/")));
            } while (true);
        }

        public static string AskMultiLine(string question, params string[] answers)
        {
            Console.WriteLine(question);

            foreach (var item in answers)
            {
                Console.WriteLine(" - " + item);
            }

            do
            {
                var userAnswer = Console.ReadLine().ToLower();
                var result = answers.FirstOrDefault(a => a.StartsWith(userAnswer, StringComparison.CurrentCultureIgnoreCase));
                if (result != null)
                    return result;

                Console.WriteLine("Possible answers:");

                foreach (var item in answers)
                {
                    Console.WriteLine(" - " + item);
                }
            } while (true);
        }


        public static bool Ask(ref bool? rememberedAnswer, string question)
        {
            if (rememberedAnswer != null)
                return rememberedAnswer.Value;

            string answerString = null;

            string result = Ask(ref answerString, question, "yes", "no");

            if (answerString.HasText())
                rememberedAnswer = answerString == "yes";

            return result == "yes";
        }

        public static string Ask(ref string rememberedAnswer, string question, params string[] answers)
        {
            if (rememberedAnswer != null)
                return rememberedAnswer;

            lock (SyncKey)
            {
                Console.Write(question + " ({0} - use '!' for all) ".FormatWith(answers.ToString("/")));
                do
                {
                    var userAnswer = Console.ReadLine().ToLower();
                    bool remember = userAnswer.Contains("!");
                    if (remember)
                        userAnswer = userAnswer.Replace("!", "");

                    var result = answers.FirstOrDefault(a => a.StartsWith(userAnswer, StringComparison.CurrentCultureIgnoreCase));
                    if (result != null)
                    {
                        if (remember)
                            rememberedAnswer = result;

                        return result;
                    }

                    Console.Write("Possible answers: ({0} - use '!' for all)".FormatWith(answers.ToString("/")));
                } while (true);
            }
        }

        public static string AskSwitch(string question, List<string> options)
        {
            var cs = new ConsoleSwitch<int, string>();

            for (int i = 0; i < options.Count; i++)
                cs.Add(i, options[i]);

            return cs.Choose(question);
        }

        public static int WaitRows(string startingText, Func<int> updateOrDelete)
        {
            SafeConsole.WriteColor(ConsoleColor.Gray, startingText);
            int result = 0;
            WaitExecute(() =>
            {
                result = updateOrDelete();

                lock (SafeConsole.SyncKey)
                {
                    SafeConsole.WriteColor(ConsoleColor.White, " {0} ", result);
                    SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "rows afected");
                }
            });
            return result;
        }

        public static T WaitQuery<T>(string startingText, Func<T> query)
        {
            T result = default(T);
            SafeConsole.WriteColor(ConsoleColor.Yellow, startingText);
            WaitExecute(() => { result = query(); Console.WriteLine(); });
            return result;
        }

        public static void WaitExecute(string startingText, Action action)
        {
            Console.Write(startingText);
            WaitExecute(() => { action(); Console.WriteLine(); }); 
        }

        public static void WaitExecute(Action action)
        {
            if (Console.IsOutputRedirected)
            {
                action();
                return;
            }

            int? result = null;
            try
            {
                int left = Console.CursorLeft;
              
               
                DateTime dt = DateTime.Now;

                Task t = Task.Factory.StartNew(() =>
                {
                    while (result == null)
                    {
                        var str = " (" + (DateTime.Now - dt).NiceToString(DateTimePrecision.Seconds) + ")";
                        Console.SetCursorPosition(Math.Max(0,Math.Min(left, Console.WindowWidth - str.Length - 1)), Console.CursorTop);

                        lock (SafeConsole.SyncKey)
                            SafeConsole.WriteColor(ConsoleColor.DarkGray,str);

                        Thread.Sleep(1000);
                    }
                });

                action(); 
            }
            finally
            {
                result = -1;
            }
        }

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler handler, bool add);

        public delegate bool ConsoleCtrlHandler(CtrlType sig);

        public enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

    }
}
