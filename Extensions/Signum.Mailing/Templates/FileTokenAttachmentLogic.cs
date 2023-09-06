using Signum.Files;
using Signum.Templating;

namespace Signum.Mailing.Templates;

public static class FileTokenAttachmentLogic
{
    public static void Start(SchemaBuilder sb)
    {
        sb.Settings.AssertImplementedBy((EmailTemplateEntity e) => e.Attachments.First(), typeof(FileTokenAttachmentEntity));

        sb.Include<FileTokenAttachmentEntity>();

        Validator.PropertyValidator((FileTokenAttachmentEntity e) => e.FileName).StaticPropertyValidation = FileTokenAttachmentFileName_StaticPropertyValidation;

        EmailTemplateLogic.FillAttachmentTokens.Register((FileTokenAttachmentEntity wa, EmailTemplateLogic.FillAttachmentTokenContext ctx) =>
        {
            if (wa.FileName != null)
                TextTemplateParser.Parse(wa.FileName, ctx.QueryDescription, ctx.ModelType).FillQueryTokens(ctx.QueryTokens);

            ctx.QueryTokens.Add(wa.FileToken.Token);
        });

        EmailTemplateLogic.GenerateAttachment.Register((FileTokenAttachmentEntity a, EmailTemplateLogic.GenerateAttachmentContext ctx) =>
        {
            using (CultureInfoUtils.ChangeBothCultures(ctx.Culture))
            {
                var col = ctx.ResultColumns.GetOrThrow(a.FileToken.Token);

                var files = ctx.CurrentRows.Select(r => r[col]).Distinct().NotNull().Select(v => v is Lite<Entity> lite ? (IFile)lite.Retrieve() : (IFile)v!).ToList();

                var overridenFileName = !a.FileName.HasText() ? null : GetTemplateString(a.FileName, ref a.FileNameNode, ctx);

                return files.Select(f => new EmailAttachmentEmbedded
                {
                    Type = a.Type,
                    ContentId = a.ContentId.DefaultToNull() ??  Guid.NewGuid().ToString(),
                    File = f is FilePathEmbedded fpa ? new FilePathEmbedded(fpa.FileType, fpa).Do(clone => clone.FileName = overridenFileName ?? clone.FileName) :
                    f is FilePathEntity fp ? new FilePathEmbedded(fp.FileType, fp).Do(clone => clone.FileName = overridenFileName ?? clone.FileName) :
                    new FilePathEmbedded(EmailFileType.Attachment, overridenFileName ?? f.FileName, f.BinaryFile) 
                }).ToList();
            }
        });
    }

    private static string GetTemplateString(string title, ref object? titleNode, EmailTemplateLogic.GenerateAttachmentContext ctx)
    {
        var block = titleNode != null ? (TextTemplateParser.BlockNode)titleNode :
            (TextTemplateParser.BlockNode)(titleNode = TextTemplateParser.Parse(title, ctx.QueryDescription, ctx.ModelType));

        return block.Print(new TextTemplateParameters(ctx.Entity, ctx.Culture, ctx.ResultColumns, ctx.CurrentRows) { Model = ctx.Model });
    }

    static string? FileTokenAttachmentFileName_StaticPropertyValidation(FileTokenAttachmentEntity WordAttachment, PropertyInfo pi)
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
