using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Engine.Scheduler;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Mailing;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Linq;
using System.Reflection;
using Signum.Engine.UserQueries;
using Signum.Entities.Processes;
using Signum.Engine.Templating;

namespace Signum.Engine.Mailing
{

    public static class SendEmailTaskLogic
    {
        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<SendEmailTaskEntity>()
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.EmailTemplate,
                        e.UniqueTarget,
                    });
                
                Validator.PropertyValidator((SendEmailTaskEntity er) => er.UniqueTarget).StaticPropertyValidation += (er, pi) =>
                {
                    if (er.UniqueTarget != null && er.TargetsFromUserQuery != null)
                        return ValidationMessage._0And1CanNotBeSetAtTheSameTime.NiceToString(pi.NiceName(), ReflectionTools.GetPropertyInfo(()=> er.TargetsFromUserQuery).NiceName());

                    Implementations? implementations = er.EmailTemplate == null ? null : GetImplementations(er.EmailTemplate.InDB(a => a.Query));
                    if (implementations != null && er.UniqueTarget == null && er.TargetsFromUserQuery == null)
                        return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

                    if (er.UniqueTarget != null)
                    {
                        if (!implementations.Value.Types.Contains(er.UniqueTarget.EntityType))
                            return ValidationMessage._0ShouldBeOfType1.NiceToString(pi.NiceName(), implementations.Value.Types.CommaOr(t => t.NiceName()));
                    }

                    return null;
                };

                Validator.PropertyValidator((SendEmailTaskEntity er) => er.TargetsFromUserQuery).StaticPropertyValidation += (SendEmailTaskEntity er, PropertyInfo pi) =>
                {
                    Implementations? implementations = er.EmailTemplate == null ? null : GetImplementations(er.EmailTemplate.InDB(a => a.Query));
                    if (implementations != null && er.TargetsFromUserQuery == null && er.UniqueTarget == null)
                        return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

                    if (er.TargetsFromUserQuery != null)
                    {
                        var uqImplementations = GetImplementations(er.TargetsFromUserQuery.InDB(a => a.Query));
                        if (!implementations.Value.Types.Intersect(uqImplementations.Value.Types).Any())
                            return ValidationMessage._0ShouldBeOfType1.NiceToString(pi.NiceName(), implementations.Value.Types.CommaOr(t => t.NiceName()));
                    }

                    return null;
                };

                new Graph<SendEmailTaskEntity>.Execute(SendEmailTaskOperation.Save)
                {
                    CanBeNew = true,
                    CanBeModified = true,
                    Execute = (e, _) => { }
                }.Register();

                SchedulerLogic.ExecuteTask.Register((SendEmailTaskEntity er, ScheduledTaskContext ctx) =>
                {
                    if (er.UniqueTarget != null)
                    {
                        ModifiableEntity entity = er.UniqueTarget.RetrieveAndRemember();

                        if (er.ModelConverter != null)
                            entity = er.ModelConverter.Convert(entity);

                        Lite<EmailMessageEntity>? last = null; 
                        foreach (var email in er.EmailTemplate.CreateEmailMessage(entity))
                        {
                            email.SendMailAsync();
                            last = email.ToLite();
                        }
                        return last;
                    }
                    else
                    {
                        var qr = er.TargetsFromUserQuery!.RetrieveAndRemember().ToQueryRequest();
                        qr.Columns.Clear();
                        var result = QueryLogic.Queries.ExecuteQuery(qr);

                        var entities = result.Rows.Select(a => a.Entity).ToList();
                        if (entities.IsEmpty())
                            return null;

                        return EmailPackageLogic.SendMultipleEmailsAsync(er.EmailTemplate, entities, er.ModelConverter).Execute(ProcessOperation.Execute).ToLite();
                    }
                });
            }
        }

        public static Implementations? GetImplementations(QueryEntity query)
        {
            if (query == null)
                return null;

            var queryName = query?.ToQueryName();

            if (queryName == null)
                return null;

            var entityColumn = QueryLogic.Queries.QueryDescription(queryName).Columns.Single(a => a.IsEntity);
            var implementations = entityColumn.Implementations.Value;

            if (implementations.IsByAll)
                throw new InvalidOperationException("ByAll implementations not supported");

            return implementations;
        }
    }
}
