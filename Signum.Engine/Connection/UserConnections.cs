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
                return new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            return File.ReadAllLines(FileName).Where(s=> !string.IsNullOrWhiteSpace(s) && !s.StartsWith("-") && !s.StartsWith("/")).ToDictionary(a => a.Before('>'), a => a.After('>'));          
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
