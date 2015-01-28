using Signum.Engine.Maps;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Engine.Linq
{
    public class AliasGenerator
    {
        HashSet<string> usedAliases = new HashSet<string>();

        int selectAliasCount = 0;
        public Alias NextSelectAlias()
        {
            return GetUniqueAlias("s" + (selectAliasCount++));
        }

        public Alias GetUniqueAlias(string baseAlias)
        {
            if (usedAliases.Add(baseAlias))
                return new Alias(baseAlias);

            for (int i = 1; ; i++)
            {
                string alias = baseAlias + i;

                if (usedAliases.Add(alias))
                    return new Alias(alias);

            }
        }

        public Alias Table(ObjectName name)
        {
            return new Alias(name.ToString());
        }

        public Alias Raw(string name)
        {
            return new Alias(name);
        }

        public Alias NextTableAlias(string tableName)
        {
            string abv = tableName.Any(char.IsUpper) ? new string(tableName.Where(c => char.IsUpper(c)).ToArray()) :
                tableName.Any(a => a == '_') ? new string(tableName.SplitNoEmpty('_' ).Select(s => s[0]).ToArray()) : null;
            
            if (string.IsNullOrEmpty(abv))
                abv = tableName.TryStart(3);
            else
                abv = abv.ToLower();

            return GetUniqueAlias(abv);
        }

        public Alias CloneAlias(Alias alias)
        {
            return GetUniqueAlias(alias.Name + "b");
        }
    }


    public class Alias
    {
        public static readonly Alias Unknown = new Alias("Unknown");

        public readonly string Name;

        internal Alias(string name)
        {
            this.Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
