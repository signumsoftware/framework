using Signum.Engine.Basics;
using Signum.Engine.UserAssets;
using System.Globalization;

namespace Signum.Mailing.Templates;

public static class EmailMasterTemplateLogic
{
    public static EmailMasterTemplateMessageEmbedded? GetCultureMessage(this EmailMasterTemplateEntity template, CultureInfo ci)
    {
        
        return template.Messages.SingleOrDefault(tm => tm.CultureInfo.ToCultureInfo().Equals(ci));
    }

    public static Func<EmailMasterTemplateEntity>? CreateDefaultMasterTemplate;

    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodBase.GetCurrentMethod()))
        {
            sb.Include<EmailMasterTemplateEntity>()
                .WithQuery(() => t => new
                {
                    Entity = t,
                    t.Id,
                    t.Name,
                });

            EmailMasterTemplateGraph.Register();
            Validator.PropertyValidator<EmailMasterTemplateEntity>(et => et.Messages).StaticPropertyValidation += (et, pi) =>
            {
                var dc = EmailLogic.Configuration.DefaultCulture;
                
                if (!et.Messages.Any(m => m.CultureInfo != null && dc.Name.StartsWith(m.CultureInfo.Name)))
                    return EmailTemplateMessage.ThereMustBeAMessageFor0.NiceToString().FormatWith(CultureInfoLogic.EntityToCultureInfo.Value.Keys.Where(c => dc.Name.StartsWith(c.Name)).CommaOr(a => a.EnglishName));

                return null;
            };

            UserAssetsImporter.Register<EmailMasterTemplateEntity>("EmailMasterTemplate", EmailMasterTemplateOperation.Save);

        }
    }

    class EmailMasterTemplateGraph : Graph<EmailMasterTemplateEntity>
    {
        public static void Register()
        {
            new Construct(EmailMasterTemplateOperation.Create)
            {
                Construct = _ => CreateDefaultMasterTemplate == null ? new EmailMasterTemplateEntity { } : CreateDefaultMasterTemplate()
            }.Register();

            new Execute(EmailMasterTemplateOperation.Save)
            {
                CanBeNew = true,
                CanBeModified = true,
                Execute = (t, _) => { }
            }.Register();
        }
    }

    public static Lite<EmailMasterTemplateEntity>? GetDefaultMasterTemplate()
    {


        var result = Database.Query<EmailMasterTemplateEntity>().Where(a => a.IsDefault).Select(emt => emt.ToLite()).FirstOrDefault();

        if (result != null)
            return result;

        if (CreateDefaultMasterTemplate == null)
            return null;

        var newTemplate = CreateDefaultMasterTemplate();

        newTemplate.IsDefault = true;

        using (OperationLogic.AllowSave<EmailMasterTemplateEntity>())
            return newTemplate.Save().ToLite();
    }
}
