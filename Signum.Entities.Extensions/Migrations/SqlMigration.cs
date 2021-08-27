using System;
using System.Linq.Expressions;
using Signum.Utilities;

namespace Signum.Entities.Migrations
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
    public class SqlMigrationEntity : Entity
    {
        [UniqueIndex]
        [StringLengthValidator(Max = 200)]
        public string VersionNumber { get; set; }

        [StringLengthValidator(Min = 0, Max = 400)]
        public string? Comment { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => VersionNumber);
    }
}
