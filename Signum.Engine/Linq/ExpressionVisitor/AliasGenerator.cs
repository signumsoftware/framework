using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Reflection;
using Signum.Utilities;

namespace Signum.Engine.Linq
{
    public class AliasGenerator
    {
        Dictionary<string, int> tablesCount = new Dictionary<string, int>() { { "s", 0 } };
        Dictionary<Type, string> baseAlias = new Dictionary<Type, string>();

        string GenerateBaseAlias(Type type)
        {
            string cleanName = Reflector.CleanTypeName(type);

            string result = cleanName.Where(c => char.IsUpper(c)).ToString("").ToLower();
            if (!tablesCount.ContainsKey(result))
                return result;

            for (int i = 1; i < cleanName.Length; i++)
            {
                result = cleanName.Substring(0, i).ToLower();
                if (!tablesCount.ContainsKey(result))
                    return result;
            }

            int count = 1;
            while (true)
            {
                result = cleanName.ToLower() + new string('_', count++);
                if (!tablesCount.ContainsKey(result))
                    return result;
            }
        }

        public string GetNextTableAlias(Type type)
        {
            string alias;
            if (!baseAlias.TryGetValue(type, out alias))
            {
                alias = GenerateBaseAlias(type);
                baseAlias[type] = alias;
            }


            int? count = tablesCount.TryGetS(alias);
            tablesCount[alias] = (count ?? 0) + 1;

            return alias + count;
        }



        int selectAliasCount = 0;
        public string GetNextSelectAlias()
        {
            return "s" + (selectAliasCount++);
        }
    }
}
