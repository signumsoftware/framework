using Signum.Engine.DynamicQuery;
using Signum.Engine.Mailing;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Word;
using Signum.Entities.Mailing;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using Signum.Engine.Templating;

namespace Signum.Engine.Word
{
    public class WordAttachmentLogic
    {
        public static void Start(SchemaBuilder sb)
        {
            sb.Include<WordAttachmentEntity>()
                .WithQuery(() => s => new
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
                    TextTemplateParser.Parse(wa.FileName, ctx.QueryDescription, ctx.ModelType).FillQueryTokens(ctx.QueryTokens);
            });

            Validator.PropertyValidator((WordAttachmentEntity e) => e.FileName).StaticPropertyValidation = WordAttachmentFileName_StaticPropertyValidation;

            EmailTemplateLogic.GenerateAttachment.Register((WordAttachmentEntity wa, EmailTemplateLogic.GenerateAttachmentContext ctx) =>
            {
                var entity = wa.OverrideModel?.RetrieveAndRemember() ??  (Entity?)ctx.Entity ?? ctx.Model!.UntypedEntity;

                if (wa.ModelConverter != null)
                    entity = wa.ModelConverter.Convert(entity);
                
                using (CultureInfoUtils.ChangeBothCultures(ctx.Culture))
                {
                    WordTemplateEntity template = WordTemplateLogic.GetFromCache(wa.WordTemplate);

                    var fileName = GetTemplateString(wa.FileName, ref wa.FileNameNode, ctx);

                    var model = template.Model != null && !WordModelLogic.RequiresExtraParameters(template.Model) ?
                    WordModelLogic.CreateDefaultWordModel(template.Model, entity) : null;

                    var bytes = WordTemplateLogic.CreateReport(template, entity, model);

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

        private static string GetTemplateString(string title, ref object? titleNode, EmailTemplateLogic.GenerateAttachmentContext ctx)
        {
            var block = titleNode != null ? (TextTemplateParser.BlockNode)titleNode :
                (TextTemplateParser.BlockNode)(titleNode = TextTemplateParser.Parse(title, ctx.QueryDescription, ctx.ModelType));

            return block.Print(new TextTemplateParameters(ctx.Entity, ctx.Culture, ctx.ResultColumns, ctx.CurrentRows) { Model = ctx.Model });
        }

        static string? WordAttachmentFileName_StaticPropertyValidation(WordAttachmentEntity wordAttachment, PropertyInfo pi)
        {
            var template = wordAttachment.TryGetParentEntity<EmailTemplateEntity>();
            if (template != null && wordAttachment.FileNameNode as TextTemplateParser.BlockNode == null)
            {
                try
                {
                    wordAttachment.FileNameNode = EmailTemplateLogic.ParseTemplate(template, wordAttachment.FileName, out string errorMessage);
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
}
