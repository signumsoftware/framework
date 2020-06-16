using System;
using Signum.Entities.Basics;

namespace Signum.Entities.Help
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class AppendixHelpEntity : Entity
    {
        [StringLengthValidator(Min = 3, Max = 100)]
        public string UniqueName { get; set; }
        
        public CultureInfoEntity Culture { get; set; }

        [StringLengthValidator(Max = 200)]
        public string Title { get; set; }

        [StringLengthValidator(Min = 3, MultiLine = true)]
        public string? Description { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }

    [AutoInit]
    public static class AppendixHelpOperation
    {
        public static ExecuteSymbol<AppendixHelpEntity> Save;
        public static DeleteSymbol<AppendixHelpEntity> Delete;
    }
}
