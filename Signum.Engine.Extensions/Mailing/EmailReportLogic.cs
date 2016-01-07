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
                    if (er.EmailTemplate != null && er.Target == null && er.EmailTemplate.Retrieve().Query != null)
                        return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

                    var queryName = er.EmailTemplate.Retrieve().Query.ToQueryName();
                    var entityColumn = DynamicQueryManager.Current.QueryDescription(queryName).Columns.Single(a => a.IsEntity);
                    var implementations = entityColumn.Implementations.Value;

                    if (!implementations.Types.Contains(er.Target.EntityType))
                        return ValidationMessage._0ShouldBeOfType1.NiceToString(pi.NiceName(), implementations.Types.CommaOr(t => t.NiceName()));

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
    }
}
