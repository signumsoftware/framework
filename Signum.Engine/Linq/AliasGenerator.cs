using Signum.Engine.Maps;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Engine.Linq
{
    public class AliasGenerator
    {
        readonly HashSet<string> usedAliases = new HashSet<string>();
        public bool isPostgres = Schema.Current.Settings.IsPostgres;

        int selectAliasCount = 0;
        public Alias NextSelectAlias()
        {
            return GetUniqueAlias("s" + (selectAliasCount++));
        }

        public Alias GetUniqueAlias(string baseAlias)
        {
            if (usedAliases.Add(baseAlias))
                return new Alias(baseAlias, isPostgres);

            for (int i = 1; ; i++)
            {
                string alias = baseAlias + i;

                if (usedAliases.Add(alias))
                    return new Alias(alias, isPostgres);

            }
        }

        public Alias Table(ObjectName objectName)
        {
            return new Alias(objectName);
        }

        public Alias Raw(string name)
        {
            return new Alias(name, isPostgres);
        }

        public Alias NextTableAlias(string tableName)
        {
            string? abv = tableName.Any(char.IsUpper) ? new string(tableName.Where(c => char.IsUpper(c)).ToArray()) :
                tableName.Any(a => a == '_') ? new string(tableName.SplitNoEmpty('_' ).Select(s => s[0]).ToArray()) : null;

            if (!abv.HasText())
                abv = tableName.TryStart(3).ToLower();
            else
                abv = abv.ToLower();

            return GetUniqueAlias(abv);
        }

        public Alias CloneAlias(Alias alias)
        {
            if (alias.Name == null)
                throw new InvalidOperationException("Alias should have a name");

            return GetUniqueAlias(alias.Name + "b");
        }
    }


    public class Alias: IEquatable<Alias>
    {
        public readonly bool isPostgres;
        public readonly string? Name; //Mutually exclusive
        public readonly ObjectName? ObjectName; //Mutually exclusive

        internal Alias(string name, bool isPostgres)
        {
            this.Name = name;
            this.isPostgres = isPostgres;
        }

        internal Alias(ObjectName objectName)
        {
            this.ObjectName = objectName;
        }

        public bool Equals(Alias? other)
        {
            if (other == null)
                return false;

            return this.Name == other.Name && object.Equals(this.ObjectName, other.ObjectName);
        }

        public override bool Equals(object? obj)
        {
            return obj is Alias && base.Equals((Alias)obj);
        }

        public override int GetHashCode()
        {
            return this.Name == null? this.ObjectName!.GetHashCode(): this.Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name?.SqlEscape(isPostgres) ?? ObjectName!.ToString();
        }
    }
}
