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
        HashSet<string> usedAliases = new HashSet<string>();

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

        int selectAliasCount = 0;
        public Alias NextSelectAlias()
        {
            return GetUniqueAlias("s" + (selectAliasCount++));
        }

        public Alias NextTableAlias(string tableName)
        {
            string abv = new string(tableName.Where(c => char.IsUpper(c)).ToArray());

            return GetUniqueAlias(abv);
        }

        internal Alias CloneAlias(Alias alias)
        {
            return GetUniqueAlias(alias.Name + "b");
        }
    }
}
