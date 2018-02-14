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
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string UniqueName { get; set; }

        [NotNullValidator]
        public CultureInfoEntity Culture { get; set; }

        public string Title { get; set; }

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
