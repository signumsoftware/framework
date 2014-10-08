using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Signum.Engine.Maps;
using Signum.Utilities;

namespace Signum.Engine.CodeGeneration
{
    public static class CodeGenerator
    {
        public static EntityCodeGenerator Entities = new EntityCodeGenerator();

        public static void GenerateCodeConsole()
        {
            while (true)
            {
                var action = new ConsoleSwitch<string, Action>("What do you want to generate today?")
                {
                    {"E", Entities.GenerateEntitiesFromDatabaseTables, "Entities (from Database tables)"},
                    {"L", LogicFromEntites, "Logic (from entites)"},
                    {"Win", WindowsFromEntites, "Logic (from entites)"},
                    {"Web", WebFromEntites, "Logic (from entites)"}
                }.Choose();

                if (action == null)
                    return;

                action();

                if (action == Entities.GenerateEntitiesFromDatabaseTables)
                    return;
            }
        }

        public static void LogicFromEntites()
        {

        }

        public static void WindowsFromEntites()
        {

        }

        public static void WebFromEntites()
        {

        }

        internal static void GetSolutionInfo(out string solutionFolder, out string solutionName)
        {
            var m = Regex.Match(Environment.CurrentDirectory, @"(?<solutionFolder>.*)\\(?<solutionName>.*).Load\\bin\\(Debug|Release)", RegexOptions.ExplicitCapture);

            if (!m.Success)
                throw new InvalidOperationException("Unable to GetSolutionInfo from non-standart path " + Environment.CurrentDirectory + ". Override GetSolutionInfo");

            solutionFolder = m.Groups["solutionFolder"].Value;
            solutionName = m.Groups["solutionName"].Value;
        }
    }
}
