using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Dynamic
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class DynamicViewEntity : Entity
    {
        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string ViewName { get; set; } = "Default";

        [NotNullable]
        [NotNullValidator]
        public TypeEntity EntityType { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        [StringLengthValidator(AllowNulls = false, Min = 3)]
        public string ViewContent { get; set; }
    }

    [AutoInit]
    public static class DynamicViewOperation
    {
        public static readonly ConstructSymbol<DynamicViewEntity>.From<DynamicViewEntity> Clone;
        public static readonly ExecuteSymbol<DynamicViewEntity> Save;
        public static readonly DeleteSymbol<DynamicViewEntity> Delete;
    }

    public enum DynamicViewMessage
    {
        AddChild,
        AddSibling,
        Remove,
        SelectATypeOfComponent,
    }
}
