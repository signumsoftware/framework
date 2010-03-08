using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Signum.Engine;
using Signum.Engine.Maps;
using Signum.Utilities;
using $custommessage$.Load.Properties;
using $custommessage$.Logic;
using $custommessage$.Entities;

namespace $custommessage$.Load
{
    class Program
    {
        static void Main(string[] args)
        {
            Starter.Start(Settings.Default.ConnectionString);

            Console.WriteLine("..:: Welcome to $custommessage$ Loading Application ::.."); 
            Console.WriteLine("Database: {0}", Regex.Match(((Connection)ConnectionScope.Current).ConnectionString, @"Initial Catalog\=(?<db>.*)\;").Groups["db"].Value);
            Console.WriteLine();

            while(true)
            {
                 Action action = new ConsoleSwitch<string, Action>
                {
                    {"N", NewDatabase},
                    {"S", Synchronize},
                    {"L", null},
                }.Choose();

                if (action == null)
                    break;

                action();
            }

            Schema.Current.Initialize(InitLevel.Level0SyncEntities );

            while (true)
            {
                Action[] actions = new ConsoleSwitch<int, Action>
                {
                    {0, LoadXXX},
                }.ChooseMultiple();

                if (actions == null)
                    return;

                foreach (var acc in actions)
                {
                    Console.WriteLine("------- Ejecutando {0} ".Formato(acc.Method.Name.SpacePascal(true)).PadRight(Console.WindowWidth - 2, '-'));
                    acc();
                }
            }
        }

        public static void NewDatabase()
        {
            Console.WriteLine("You will lose all your data. Sure? (Y/N)");
            string val = Console.ReadLine();
            if (!val.StartsWith("y") && !val.StartsWith("Y"))
                return;

            Console.WriteLine("Creating new database...");
            Administrator.TotalGeneration();
            Console.WriteLine("Done.");
        }


        static void Synchronize()
        {
            Console.WriteLine("Check and Modify the synchronization script before");
            Console.WriteLine("executing it in SQL Server Management Studio: ");
            Console.WriteLine();

            SqlPreCommand command = Administrator.TotalSynchronizeScript();
            command.OpenSqlFile();
            Console.WriteLine("Open again?");
            string val = Console.ReadLine();
            if (!val.StartsWith("y") && !val.StartsWith("Y"))
                return;

            command.OpenSqlFile();
        }

        static void LoadXXX()
        {

        }
    }

}
