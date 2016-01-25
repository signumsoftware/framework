using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Entities.Files;
using System.Linq.Expressions;
using System.ComponentModel;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.UserQueries;
using Signum.Entities.Mailing;

namespace Signum.Entities.Excel
{
    [Serializable, EntityKind(EntityKind.Shared, EntityData.Master)]
    public class ExcelAttachmentEntity : Entity, IAttachmentGeneratorEntity
    {
        [NotNullable, SqlDbType(Size = 100), FileNameValidator]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string FileName { get; set; } = "Report.xlsx";

        [NotNullable]
        [NotNullValidator]
        public Lite<UserQueryEntity> UserQuery { get; set; }

        
        [ImplementedByAll]
        public Lite<Entity> Related { get; set; }


        static Expression<Func<ExcelAttachmentEntity, string>> ToStringExpression = @this => @this.FileName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class ExcelAttachmentOperation
    {
        public static readonly ExecuteSymbol<ExcelAttachmentEntity> Save;
    }
}
