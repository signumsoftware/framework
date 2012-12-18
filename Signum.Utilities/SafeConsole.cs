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
            lock(SyncKey)
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
    }
}
