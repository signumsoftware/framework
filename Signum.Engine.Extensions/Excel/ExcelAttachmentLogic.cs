using Signum.Engine.DynamicQuery;
using Signum.Engine.Mailing;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Engine.UserQueries;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Excel;
using Signum.Entities.Mailing;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Signum.Engine.Excel
{
    public class ExcelAttachmentLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            sb.Include<ExcelAttachmentEntity>()
                .WithQuery(dqm, () => s => new
                {
                    Entity = s,
                    s.Id,
                    s.FileName,
                    s.UserQuery,
                    s.Related,
                });
            
            EmailTemplateLogic.FillAttachmentTokens.Register((ExcelAttachmentEntity ea, EmailTemplateLogic.FillAttachmentTokenContext ctx) =>
            {
                if (ea.FileName != null)
                    EmailTemplateParser.Parse(ea.FileName, ctx.QueryDescription, ctx.ModelType).FillQueryTokens(ctx.QueryTokens);

                if (ea.Title != null)
                    EmailTemplateParser.Parse(ea.Title, ctx.QueryDescription, ctx.ModelType).FillQueryTokens(ctx.QueryTokens);
            });

            Validator.PropertyValidator((ExcelAttachmentEntity e) => e.FileName).StaticPropertyValidation = ExcelAttachmentFileName_StaticPropertyValidation;
            Validator.PropertyValidator((ExcelAttachmentEntity e) => e.Title).StaticPropertyValidation = ExcelAttachmentTitle_StaticPropertyValidation;

            EmailTemplateLogic.GenerateAttachment.Register((ExcelAttachmentEntity ea, EmailTemplateLogic.GenerateAttachmentContext ctx) =>
            {
                var finalEntity = ea.Related?.Retrieve() ?? (Entity)ctx.Entity ?? ctx.SystemEmail.UntypedEntity as Entity;

                using (finalEntity == null ? null : CurrentEntityConverter.SetCurrentEntity(finalEntity))
                using (CultureInfoUtils.ChangeBothCultures(ctx.Culture))
                {
                    QueryRequest request = UserQueryLogic.ToQueryRequest(ea.UserQuery.Retrieve());

                    var title = GetTemplateString(ea.Title, ref ea.TitleNode, ctx);
                    var fileName = GetTemplateString(ea.FileName, ref ea.FileNameNode, ctx);

                    var bytes = ExcelLogic.ExecutePlainExcel(request, title);

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

        static string ExcelAttachmentFileName_StaticPropertyValidation(ExcelAttachmentEntity excelAttachment, PropertyInfo pi)
        {
            var template = (EmailTemplateEntity)excelAttachment.GetParentEntity();
            if (template != null && excelAttachment.FileNameNode as EmailTemplateParser.BlockNode == null)
            {
                try
                {
                    excelAttachment.FileNameNode = EmailTemplateLogic.ParseTemplate(template, excelAttachment.FileName, out string errorMessage);
                    return errorMessage.DefaultText(null);
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }

            return null;
        }

        static string ExcelAttachmentTitle_StaticPropertyValidation(ExcelAttachmentEntity excelAttachment, PropertyInfo pi)
        {
            var template = (EmailTemplateEntity)excelAttachment.GetParentEntity();
            if (template != null && excelAttachment.TitleNode as EmailTemplateParser.BlockNode == null)
            {
                try
                {
                    excelAttachment.FileNameNode = EmailTemplateLogic.ParseTemplate(template, excelAttachment.Title, out string errorMessage);
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
