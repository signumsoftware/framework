using Signum.Engine.DynamicQuery;
using Signum.Engine.Mailing;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Engine.UserQueries;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Word;
using Signum.Entities.Mailing;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Signum.Engine.Templating;

namespace Signum.Engine.Word
{
    public class WordAttachmentLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            sb.Include<WordAttachmentEntity>()
                .WithQuery(dqm, () => s => new
                {
                    Entity = s,
                    s.Id,
                    s.FileName,
                    s.WordTemplate,
                    s.OverrideModel,
                });
            
            EmailTemplateLogic.FillAttachmentTokens.Register((WordAttachmentEntity wa, EmailTemplateLogic.FillAttachmentTokenContext ctx) =>
            {
                if (wa.FileName != null)
                    EmailTemplateParser.Parse(wa.FileName, ctx.QueryDescription, ctx.ModelType).FillQueryTokens(ctx.QueryTokens);
            });

            Validator.PropertyValidator((WordAttachmentEntity e) => e.FileName).StaticPropertyValidation = WordAttachmentFileName_StaticPropertyValidation;

            EmailTemplateLogic.GenerateAttachment.Register((WordAttachmentEntity wa, EmailTemplateLogic.GenerateAttachmentContext ctx) =>
            {
                var entity = wa.OverrideModel?.Retrieve() ??  (Entity)ctx.Entity ?? ctx.SystemEmail.UntypedEntity;

                if (wa.ModelConverter != null)
                    entity = wa.ModelConverter.Convert(entity);
                
                using (CultureInfoUtils.ChangeBothCultures(ctx.Culture))
                {
                    WordTemplateEntity template = WordTemplateLogic.GetFromCache(wa.WordTemplate);

                    var fileName = GetTemplateString(wa.FileName, ref wa.FileNameNode, ctx);

                    var systemWordTemplate = template.SystemWordTemplate != null && !SystemWordTemplateLogic.RequiresExtraParameters(template.SystemWordTemplate) ?
                    SystemWordTemplateLogic.CreateDefaultSystemWordTemplate(template.SystemWordTemplate, entity) : null;

                    var bytes = WordTemplateLogic.CreateReport(template, entity, systemWordTemplate);

                    return new List<EmailAttachmentEmbedded>
                    {
                        new EmailAttachmentEmbedded
                        {
                            File = Files.FilePathEmbeddedLogic.SaveFile(new Entities.Files.FilePathEmbedded(EmailFileType.Attachment, fileName, bytes)),
                            Type = EmailAttachmentType.Attachment,
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

        static string WordAttachmentFileName_StaticPropertyValidation(WordAttachmentEntity WordAttachment, PropertyInfo pi)
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
