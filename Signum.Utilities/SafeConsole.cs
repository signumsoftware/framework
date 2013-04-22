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
            string str = string.Format(format, parameters);

            if (needToClear)
                str = str.PadChopRight(Console.BufferWidth - 1);
            else
                str = str.TryStart(Console.BufferWidth - 1);

            Console.WriteLine(str);

            Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop - 1);
            needToClear = true;
        }

        public static void ClearSameLine()
        {
            if (needToClear)
            {
                Console.WriteLine(new string(' ', Console.BufferWidth - 1));

                Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop - 1);
            }
            needToClear = false;
        }


        public static void WriteColor(ConsoleColor color, string format, params object[] parameters)
        {
            string str = string.Format(format, parameters);
            ConsoleColor old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(str);
            Console.ForegroundColor = old;
        }

        public static void WriteLineColor(ConsoleColor color, string format, params object[] parameters)
        {
            string str = string.Format(format, parameters);
            ConsoleColor old = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(str);
            Console.ForegroundColor = old;
        }

        public static string AskString(string question, Func<string, bool> stringValidator)
        {
            Console.Write(question);
            do
            {
                var userAnswer = Console.ReadLine().ToLower();

                if (stringValidator(userAnswer))
                    return userAnswer;
            } while (true);
        }


        public static bool Ask(string question)
        {
            return Ask(question, "yes", "no") == "yes";
        }

        public static string Ask(string question, params string[] answers)
        {
            Console.Write(question + " ({0}) ".Formato(answers.ToString("/")));
            do
            {
                var userAnswer = Console.ReadLine().ToLower();
                var result = answers.FirstOrDefault(a => a.StartsWith(userAnswer, StringComparison.CurrentCultureIgnoreCase));
                if (result != null)
                    return result;

                Console.Write("Possible answers: {0} ".Formato(answers.ToString("/")));
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
                Console.Write(question + " ({0} - !forAll) ".Formato(answers.ToString("/")));
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

                    Console.Write("Possible answers: ({0} - !forAll)".Formato(answers.ToString("/")));
                } while (true);
            }
        }

        public static void Wait(string startingText, Func<int> updateOrDelete)
        {
            Wait(startingText, () =>
            {
                int result = updateOrDelete();

                SafeConsole.WriteColor(ConsoleColor.White, " {0} ", result);
                SafeConsole.WriteLineColor(ConsoleColor.DarkGray, "rows afected");
            }); 
        }

        public static void Wait(string startingText, Action action)
        {
            Console.Write(startingText);
            int? result = null;
            try
            {
                int left  = Console.CursorLeft;
               
                DateTime dt = DateTime.Now;

                Task t = Task.Factory.StartNew(() =>
                {
                    while (result == null)
                    { 
                        Console.SetCursorPosition(left, Console.CursorTop);

                        SafeConsole.WriteColor(ConsoleColor.DarkGray, " (" + (DateTime.Now - dt).NiceToString(DateTimePrecision.Seconds) + ")");

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
