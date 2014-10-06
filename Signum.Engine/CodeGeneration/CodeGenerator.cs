using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Signum.Engine.Maps;
using Signum.Utilities;

namespace Signum.Engine.CodeGeneration
{
    public class CodeGenerator
    {   
        public static void GenerateCodeConsole()
        {
            while (true)
            {
                var action = new ConsoleSwitch<string, Action>("What do you want to generate today?")
                {
                    {"E", EntitiesFromDatabaseTables, "Entities (from Database tables)"},
                    {"L", LogicFromEntites, "Logic (from entites)"},
                    {"Win", WindowsFromEntites, "Logic (from entites)"},
                    {"Web", WebFromEntites, "Logic (from entites)"}
                }.Choose();

                if (action == null)
                    return;

                action();

                if (action == EntitiesFromDatabaseTables)
                    return;
            }
        }

        public static void EntitiesFromDatabaseTables()
        {
            var dic = SchemaSynchronizer.DefaultGetDatabaseDescription(Schema.Current.DatabaseNames());

            dic.RemoveRange(Schema.Current.GetDatabaseTables().Select(a => a.Name.ToString()));

            Dictionary<DiffTable, TableOptions> options = new Dictionary<DiffTable, TableOptions>(); 
        }

        public class TableOptions
        {
            public string MListParentColumnName;
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
    }
}
