using Signum.Files;
using Signum.Templating;

namespace Signum.Mailing.Templates;

public static class ImageAttachmentLogic
{
    public static void Start(SchemaBuilder sb)
    {
        sb.Settings.AssertImplementedBy((EmailTemplateEntity e) => e.Attachments.First(), typeof(ImageAttachmentEntity));

        sb.Include<ImageAttachmentEntity>();

        Validator.PropertyValidator((ImageAttachmentEntity e) => e.FileName).StaticPropertyValidation = ImageAttachmentFileName_StaticPropertyValidation;

        EmailTemplateLogic.FillAttachmentTokens.Register((ImageAttachmentEntity wa, EmailTemplateLogic.FillAttachmentTokenContext ctx) =>
        {
            if (wa.FileName != null)
                TextTemplateParser.Parse(wa.FileName, ctx.QueryDescription, ctx.ModelType).FillQueryTokens(ctx.QueryTokens);
        });

        EmailTemplateLogic.GenerateAttachment.Register((ImageAttachmentEntity a, EmailTemplateLogic.GenerateAttachmentContext ctx) =>
        {
            using (CultureInfoUtils.ChangeBothCultures(ctx.Culture))
            {
                var fileName = !a.FileName.HasText() ? a.File.FileName : GetTemplateString(a.FileName, ref a.FileNameNode, ctx);

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

    private static string GetTemplateString(string title, ref object? titleNode, EmailTemplateLogic.GenerateAttachmentContext ctx)
    {
        var block = titleNode != null ? (TextTemplateParser.BlockNode)titleNode :
            (TextTemplateParser.BlockNode)(titleNode = TextTemplateParser.Parse(title, ctx.QueryContext?.QueryDescription, ctx.ModelType));

        return block.Print(new TextTemplateParameters(ctx.Entity, ctx.Culture, ctx.QueryContext) { Model = ctx.Model });
    }

    static string? ImageAttachmentFileName_StaticPropertyValidation(ImageAttachmentEntity WordAttachment, PropertyInfo pi)
    {
        var template = WordAttachment.TryGetParentEntity<EmailTemplateEntity>()!;
        if (template != null && WordAttachment.FileNameNode as TextTemplateParser.BlockNode == null)
        {
            try
            {
                WordAttachment.FileNameNode = EmailTemplateLogic.ParseTemplate(template, WordAttachment.FileName, out string errorMessage);
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
