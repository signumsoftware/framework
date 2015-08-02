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
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string UniqueName { get; set; }

        [NotNullable]
        [NotNullValidator]
        public CultureInfoEntity Culture { get; set; }

        [NotNullable, SqlDbType(Size = 300)]
        public string Title { get; set; }

        [SqlDbType(Size = int.MaxValue)]
		[StringLengthValidator(AllowNulls = true, Min = 3, MultiLine = true)]
        public string Description { get; set; }

        public override string ToString()
        {
            return Title?.ToString();
        }
    }

    [AutoInit]
    public static class AppendixHelpOperation
    {
        public static ExecuteSymbol<AppendixHelpEntity> Save;
    }
}
