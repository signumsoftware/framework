using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Engine.Translation;
using Signum.Entities;
using Signum.Entities.Mailing;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Signum.Engine.Basics;

namespace Signum.Engine.Mailing
{
    public static class EmailMasterTemplateLogic
    {
        public static EmailMasterTemplateMessageEmbedded GetCultureMessage(this EmailMasterTemplateEntity template, CultureInfo ci)
        {
            return template.Messages.SingleOrDefault(tm => tm.CultureInfo.ToCultureInfo() == ci);
        }

        public static Func<EmailMasterTemplateEntity> CreateDefaultMasterTemplate;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailMasterTemplateEntity>()
                    .WithQuery(dqm, () => t => new
                    {
                        Entity = t,
                        t.Id,
                        t.Name,
                    });
                
                EmailMasterTemplateGraph.Register();

                Validator.PropertyValidator<EmailMasterTemplateEntity>(et => et.Messages).StaticPropertyValidation += (et, pi) =>
                {
                    if (!et.Messages.Any(m => m.CultureInfo.Is(EmailLogic.Configuration.DefaultCulture)))
                        return EmailTemplateMessage.ThereMustBeAMessageFor0.NiceToString().FormatWith(EmailLogic.Configuration.DefaultCulture.EnglishName);

                    return null;
                }; 
            }
        }

        class EmailMasterTemplateGraph : Graph<EmailMasterTemplateEntity>
        {
            public static void Register()
            {
                new Construct(EmailMasterTemplateOperation.Create)
                {
                    Construct = _ => CreateDefaultMasterTemplate == null ?
                        new EmailMasterTemplateEntity { } :
                        CreateDefaultMasterTemplate()
                }.Register();

                new Execute(EmailMasterTemplateOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (t, _) => { }
                }.Register();
            }
        }

        public static Lite<EmailMasterTemplateEntity> GetDefaultMasterTemplate()
        {
            var result = Database.Query<EmailMasterTemplateEntity>().Select(emt => emt.ToLite()).FirstOrDefault();

            if (result != null)
                return result;

            if (CreateDefaultMasterTemplate == null)
                return null;

            var newTemplate = CreateDefaultMasterTemplate();

            using (OperationLogic.AllowSave<EmailMasterTemplateEntity>())
                return newTemplate.Save().ToLite();
        }
    }
}
