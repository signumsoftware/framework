using Signum.UserAssets;
using Signum.Files;
using Signum.Mailing;
using Signum.Mailing.Templates;
using Signum.Templating;
using Signum.UserQueries;

namespace Signum.Excel;

public class ExcelAttachmentLogic
{
    public static void Start(SchemaBuilder sb)
    {
        sb.Include<ExcelAttachmentEntity>()
            .WithQuery(() => s => new
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
                TextTemplateParser.Parse(ea.FileName, ctx.QueryDescription, ctx.ModelType).FillQueryTokens(ctx.QueryTokens);

            if (ea.Title != null)
                TextTemplateParser.Parse(ea.Title, ctx.QueryDescription, ctx.ModelType).FillQueryTokens(ctx.QueryTokens);
        });

        Validator.PropertyValidator((ExcelAttachmentEntity e) => e.FileName).StaticPropertyValidation = ExcelAttachmentFileName_StaticPropertyValidation;
        Validator.PropertyValidator((ExcelAttachmentEntity e) => e.Title).StaticPropertyValidation = ExcelAttachmentTitle_StaticPropertyValidation;

        EmailTemplateLogic.GenerateAttachment.Register((ExcelAttachmentEntity ea, EmailTemplateLogic.GenerateAttachmentContext ctx) =>
        {
            var finalEntity = ea.Related?.RetrieveAndRemember() ?? (Entity?)ctx.Entity ?? ctx.Model?.UntypedEntity as Entity;

            using (finalEntity == null ? null : CurrentEntityConverter.SetCurrentEntity(finalEntity))
            using (CultureInfoUtils.ChangeBothCultures(ctx.Culture))
            {
                QueryRequest request = UserQueryLogic.ToQueryRequest(ea.UserQuery.RetrieveAndRemember(), ignoreHidden: true);

                var title = GetTemplateString(ea.Title, ref ea.TitleNode, ctx);
                var fileName = GetTemplateString(ea.FileName, ref ea.FileNameNode, ctx);

                var bytes = ExcelLogic.ExecutePlainExcel(request, title);

                return new List<EmailAttachmentEmbedded>
                {
                    new EmailAttachmentEmbedded
                    {
                        File = new FilePathEmbedded(EmailFileType.Attachment, fileName, bytes),
                        Type = EmailAttachmentType.Attachment,
                    }
                };
            }
        });
    }

    private static string GetTemplateString(string? title, ref object? titleNode, EmailTemplateLogic.GenerateAttachmentContext ctx)
    {
        var block = titleNode != null ? (TextTemplateParser.BlockNode)titleNode :
            (TextTemplateParser.BlockNode)(titleNode = TextTemplateParser.Parse(title, ctx.QueryContext?.QueryDescription, ctx.ModelType));

        return block.Print(new TextTemplateParameters(ctx.Entity, ctx.Culture, ctx.QueryContext) { Model = ctx.Model });
    }

    static string? ExcelAttachmentFileName_StaticPropertyValidation(ExcelAttachmentEntity excelAttachment, PropertyInfo pi)
    {
        var template = excelAttachment.TryGetParentEntity<EmailTemplateEntity>()!;
        if (template != null && excelAttachment.FileNameNode as TextTemplateParser.BlockNode == null)
        {
            try
            {
                excelAttachment.FileNameNode = EmailTemplateLogic.ParseTemplate(template, excelAttachment.FileName, out string errorMessage);
                return errorMessage.DefaultToNull();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        return null;
    }

    static string? ExcelAttachmentTitle_StaticPropertyValidation(ExcelAttachmentEntity excelAttachment, PropertyInfo pi)
    {
        var template = excelAttachment.GetParentEntity<EmailTemplateEntity>()!;
        if (template != null && excelAttachment.TitleNode as TextTemplateParser.BlockNode == null)
        {
            try
            {
                excelAttachment.FileNameNode = EmailTemplateLogic.ParseTemplate(template, excelAttachment.Title, out string errorMessage);
                return errorMessage.DefaultToNull();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        return null;
    }
}
