using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Entities.Help
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class AppendixHelpEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 100)]
        string uniqueName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string UniqueName
        {
            get { return uniqueName; }
            set { Set(ref uniqueName, value); }
        }

        [NotNullable]
        CultureInfoEntity culture;
        [NotNullValidator]
        public CultureInfoEntity Culture
        {
            get { return culture; }
            set { Set(ref culture, value); }
        }

        [NotNullable, SqlDbType(Size = 300)]
        string title;
        public string Title
        {
            get { return title; }
            set { Set(ref title, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string description;
        [StringLengthValidator(AllowNulls = true, Min = 3, MultiLine = true)]
        public string Description
        {
            get { return description; }
            set { Set(ref description, value); }
        }

        public override string ToString()
        {
            return title.TryToString();
        }
    }

    public static class AppendixHelpOperation
    {
        public static readonly ExecuteSymbol<AppendixHelpEntity> Save = OperationSymbol.Execute<AppendixHelpEntity>();
    }
}
