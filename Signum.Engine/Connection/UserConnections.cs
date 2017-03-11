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
using System.Text.RegularExpressions;

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

            return File.ReadAllLines(FileName).Where(s=> !string.IsNullOrWhiteSpace(s) && !s.StartsWith("-") && !s.StartsWith("/")).ToDictionaryEx(a => a.Before('>'), a => a.After('>'), "UserConnections");          
        }

        public static string Replace(string connectionString)
        {
            if (replacements == null)
                replacements = LoadReplacements();

            Match m = Regex.Match(connectionString, @"(Initial Catalog|Database)\s*=\s*(?<databaseName>[^;]*)\s*;?", RegexOptions.IgnoreCase);

            if (!m.Success)
                throw new InvalidOperationException("Database name not found");

            string databaseName = m.Groups["databaseName"].Value;

            return replacements.TryGetC(databaseName) ?? connectionString;
        }

        public static string GetCustomReplacement(string key)
        {
            return replacements.TryGetC(key);
        }
    }
}
