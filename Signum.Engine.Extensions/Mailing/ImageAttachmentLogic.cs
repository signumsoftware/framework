using Signum.Engine.DynamicQuery;
using Signum.Engine.Files;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Files;
using Signum.Entities.Mailing;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Mailing
{
    public static class ImageAttachmentLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            sb.Settings.AssertImplementedBy((EmailTemplateEntity e) => e.Attachments.First(), typeof(ImageAttachmentEntity));

            sb.Include<ImageAttachmentEntity>()
                .WithQuery(dqm, () => s => new
                {
                    Entity = s,
                    s.Id,
                    s.FileName,
                    s.ContentId
                });

            Validator.PropertyValidator((ImageAttachmentEntity e) => e.FileName).StaticPropertyValidation = ImageAttachmentFileName_StaticPropertyValidation;

            EmailTemplateLogic.FillAttachmentTokens.Register((ImageAttachmentEntity wa, EmailTemplateLogic.FillAttachmentTokenContext ctx) =>
            {
                if (wa.FileName != null)
                    EmailTemplateParser.Parse(wa.FileName, ctx.QueryDescription, ctx.ModelType).FillQueryTokens(ctx.QueryTokens);
            });
            
            EmailTemplateLogic.GenerateAttachment.Register((ImageAttachmentEntity a, EmailTemplateLogic.GenerateAttachmentContext ctx) =>
            {
                using (CultureInfoUtils.ChangeBothCultures(ctx.Culture))
                {
                    var fileName = a.FileName.IsEmpty() ? a.File.FileName : GetTemplateString(a.FileName, ref a.FileNameNode, ctx);
                    
                    return new List<EmailAttachmentEmbedded>
                    {
                        new EmailAttachmentEmbedded
                        {
                            File = new FilePathEmbedded(EmailFileType.Attachment, fileName, a.File.BinaryFile),
                            Type = a.Type,
                            ContentId = a.ContentId,
                        }
                    };
                }
            });
        }

        private static string GetTemplateString(string title, ref object titleNode, EmailTemplateLogic.GenerateAttachmentContext ctx)
        {
            var block = titleNode != null ? (EmailTemplateParser.BlockNode)titleNode :
                (EmailTemplateParser.BlockNode)(titleNode = EmailTemplateParser.Parse(title, ctx.QueryDescription, ctx.ModelType));

            return block.Print(new EmailTemplateParameters(ctx.Entity, ctx.Culture, ctx.ResultColumns, ctx.CurrentRows) { SystemEmail = ctx.SystemEmail });
        }

        static string ImageAttachmentFileName_StaticPropertyValidation(ImageAttachmentEntity WordAttachment, PropertyInfo pi)
        {
            var template = (EmailTemplateEntity)WordAttachment.GetParentEntity();
            if (template != null && WordAttachment.FileNameNode as EmailTemplateParser.BlockNode == null)
            {
                try
                {
                    WordAttachment.FileNameNode = EmailTemplateLogic.ParseTemplate(template, WordAttachment.FileName, out string errorMessage);
                    return errorMessage.DefaultText(null);
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }

            return null;
        }
    }
}
