using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Signum.Engine;
using System.Data.SqlClient;
using System.Diagnostics;
using Signum.Engine.Maps;
using Signum.Utilities;

namespace Signum.Engine
{
    public static class UserConnections
    {
        public static string FileName = @"C:\UserConnections.txt"; 
        
        static Dictionary<string, string> replacements = null;

        public static Dictionary<string, string> LoadReplacements()
        {
            if (!File.Exists(FileName))
                return new Dictionary<string, string>();

            Debug.WriteLine("UserConnections file found on {0}".Formato(FileName));

            return File.ReadAllLines(FileName).Select(s => s.Split('>')).ToDictionary(a => a[0], a => a[1]);          
        }

        public static string Replace(string connectionString)
        {
            if (replacements == null)
                replacements = LoadReplacements();

            string schemaName = new SqlConnection(connectionString).Database;

            return replacements.TryGetC(schemaName) ?? connectionString;
        }
    }
}
