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
    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class ExcelAttachmentEntity : Entity, IAttachmentGeneratorEntity
    {
        string fileName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100), FileNameValidator]
        public string FileName
        {
            get { return fileName; }
            set
            {
                if (Set(ref fileName, value))
                    FileNameNode = null;
            }
        }

        [Ignore]
        internal object FileNameNode;

        string title;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 300)]
        public string Title
        {
            get { return title; }
            set
            {
                if (Set(ref title, value))
                    TitleNode = null;
            }
        }


        [Ignore]
        internal object TitleNode;

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
}
