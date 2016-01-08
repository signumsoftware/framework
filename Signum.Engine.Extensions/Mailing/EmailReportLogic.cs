using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Engine.Scheduler;
using Signum.Entities;
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

    public static class EmailReportLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<EmailReportEntity>();

                dqm.RegisterQuery(typeof(EmailReportEntity), () =>
                    from e in Database.Query<EmailReportEntity>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.EmailTemplate,
                        e.Target,
                    });

                Validator.PropertyValidator((EmailReportEntity er) => er.Target).StaticPropertyValidation += (er, pi) =>
                {
                    Implementations? implementations = er.EmailTemplate == null ? null : GetImplementations(er.EmailTemplate);
                    if (implementations != null && er.Target == null)
                        return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

                    if (!implementations.Value.Types.Contains(er.Target.EntityType))
                        return ValidationMessage._0ShouldBeOfType1.NiceToString(pi.NiceName(), implementations.Value.Types.CommaOr(t => t.NiceName()));

                    return null;
                };

                new Graph<EmailReportEntity>.Execute(EmailReportOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { }
                }.Register();

                SchedulerLogic.ExecuteTask.Register((EmailReportEntity er) =>
                {
                    var email = EmailTemplateLogic.CreateEmailMessage(er.EmailTemplate, er.Target?.Retrieve()).SingleEx();

                    EmailLogic.SendMail(email);

                    return email.ToLite();
                });
            }
        }

        public static Implementations? GetImplementations(Lite<EmailTemplateEntity> template)
        {
            var queryName = template.InDB(a=>a.Query)?.ToQueryName();

            if (queryName == null)
                return null;

            var entityColumn = DynamicQueryManager.Current.QueryDescription(queryName).Columns.Single(a => a.IsEntity);
            var implementations = entityColumn.Implementations.Value;
            return implementations;
        }
    }
}
