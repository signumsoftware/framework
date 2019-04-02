using Signum.Entities.Files;
using Signum.Utilities;
using System;
using System.Linq.Expressions;

namespace Signum.Entities.Mailing
{
    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class ImageAttachmentEntity : Entity, IAttachmentGeneratorEntity
    {
        [Ignore]
        internal object? FileNameNode;

        string? fileName;
        [StringLengthValidator(Min = 3, Max = 100), FileNameValidator]
        public string? FileName
        {
            get { return fileName; }
            set
            {
                if (Set(ref fileName, value))
                    FileNameNode = null;
            }
        }

        [StringLengthValidator(Min = 1, Max = 300)]
        public string ContentId { get; set; }

        public EmailAttachmentType Type { get; set; }

        
        public FileEmbedded File { get; set; }

        static Expression<Func<ImageAttachmentEntity, string>> ToStringExpression = @this => @this.FileName ?? @this.File.FileName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }
}
