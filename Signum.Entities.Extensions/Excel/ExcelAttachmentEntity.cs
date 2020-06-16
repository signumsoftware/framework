using System;
using Signum.Utilities;
using System.Linq.Expressions;
using Signum.Entities.UserQueries;
using Signum.Entities.Mailing;

namespace Signum.Entities.Excel
{
    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class ExcelAttachmentEntity : Entity, IAttachmentGeneratorEntity
    {
        string fileName;
        [StringLengthValidator(Min = 3, Max = 100), FileNameValidator]
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
        internal object? FileNameNode;

        string? title;
        [StringLengthValidator(Min = 3, Max = 300)]
        public string? Title
        {
            get { return title; }
            set
            {
                if (Set(ref title, value))
                    TitleNode = null;
            }
        }


        [Ignore]
        internal object? TitleNode;

        
        public Lite<UserQueryEntity> UserQuery { get; set; }

        [ImplementedByAll]
        public Lite<Entity> Related { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => FileName);
    }
}
