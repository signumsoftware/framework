using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Signum.Engine;
using Signum.Utilities;
using $custommessage$.Load.Properties;
using $custommessage$.Logic;
using $custommessage$.Entities;

namespace $custommessage$.Load
{
    class Program
    {
        public enum LoadCommand
        {
            CreateNewDatabase = 1,
            GenerateSyncronizationScript = 2,
            LoadData = 3, 

            Exit = 99
        }

        static Dictionary<LoadCommand, Action> actions = new Dictionary<LoadCommand, Action>
        {
            {LoadCommand.CreateNewDatabase,CreateNewDatabase },
            {LoadCommand.GenerateSyncronizationScript,CreateSincroizationScript },
            {LoadCommand.LoadData,LoadData },
        }; 

        static void Main(string[] args)
        {
            Starter.Start(Settings.Default.ConnectionString);

            Console.WriteLine("..:: Welcome to $custommessage$ Loading Application ::.."); 
            Console.WriteLine("Database: {0}", Regex.Match(((Connection)ConnectionScope.Current).ConnectionString, @"Initial Catalog\=(?<db>.*)\;").Groups["db"].Value);
            Console.WriteLine();

            while(true)
            {
                Console.WriteLine("Choose one of the following options:");
                EnumExtensions.GetValues<LoadCommand>().ToConsole(a => " {0,2} {1}".Formato( (int)a, EnumExtensions.NiceToString(a)));

                int val;
                if(!int.TryParse(Console.ReadLine(), out val))
                {
                    Console.WriteLine("Invalid Format"); 
                    continue; 
                }

                if ((LoadCommand)val == LoadCommand.Exit)
                    break;

                Action act = actions.TryGetC((LoadCommand)val);

                if (act == null)
                {
                    Console.WriteLine("No action with number {0}", val);
                    continue; 
                }

                act(); 
            }
        }

        static void CreateNewDatabase()
        {
            Console.WriteLine("You will lose all your data. Sure? (Y/N)");
            string val = Console.ReadLine(); 
            if(!val.StartsWith("y")  && !val.StartsWith("Y"))
                return;

            Console.WriteLine("Creating new database...");
            Administrator.NewDatabaseBasic();
            Console.WriteLine("Done."); 
        }

        static void CreateSincroizationScript()
        {
            Console.WriteLine("Check and Modify the synchronization script before");
            Console.WriteLine("executing it in SQL Server Management Studio: ");
            Console.WriteLine();

            SqlPreCommand command = Administrator.SynchronizeAllScript(); 

            Console.WriteLine(command.PlainSql());
        }

        static void LoadData()
        {
            Console.Write("Loading some example data..."); 
            new[]
            {
                "Example 1" , 
                "Example 2" 
            }.Select(s=>new MyEntityDN{ Name = s}).SaveList();
            Console.WriteLine("Done"); 
        }
    }

}
