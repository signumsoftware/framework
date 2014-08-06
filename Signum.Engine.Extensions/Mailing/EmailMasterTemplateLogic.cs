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
        public static EmailMasterTemplateMessageDN GetCultureMessage(this EmailMasterTemplateDN template, CultureInfo ci)
        {
            return template.Messages.SingleOrDefault(tm => tm.CultureInfo.ToCultureInfo() == ci);
        }

        public static Func<EmailMasterTemplateDN> CreateDefaultMasterTemplate;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailMasterTemplateDN>();

                dqm.RegisterQuery(typeof(EmailMasterTemplateDN), () =>
                    from t in Database.Query<EmailMasterTemplateDN>()
                    select new
                    {
                        Entity = t,
                        t.Id,
                        t.Name,
                    });

                EmailMasterTemplateGraph.Register();

                Validator.PropertyValidator<EmailMasterTemplateDN>(et => et.Messages).StaticPropertyValidation += (et, pi) =>
                {
                    if (!et.Messages.Any(m => m.CultureInfo.Is(EmailLogic.Configuration.DefaultCulture)))
                        return EmailTemplateMessage.ThereMustBeAMessageFor0.NiceToString().Formato(EmailLogic.Configuration.DefaultCulture.EnglishName);

                    return null;
                }; 
            }
        }

        class EmailMasterTemplateGraph : Graph<EmailMasterTemplateDN>
        {
            public static void Register()
            {
                new Construct(EmailMasterTemplateOperation.Create)
                {
                    Construct = _ => CreateDefaultMasterTemplate == null ?
                        new EmailMasterTemplateDN { } :
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

        public static Lite<EmailMasterTemplateDN> GetDefaultMasterTemplate()
        {
            var result = Database.Query<EmailMasterTemplateDN>().Select(emt => emt.ToLite()).FirstOrDefault();

            if (result != null)
                return result;

            if (CreateDefaultMasterTemplate == null)
                return null;

            var newTemplate = CreateDefaultMasterTemplate();

            using (OperationLogic.AllowSave<EmailMasterTemplateDN>())
                return newTemplate.Save().ToLite();
        }
    }
}
