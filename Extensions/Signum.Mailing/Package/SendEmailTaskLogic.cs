using Signum.Entities;
using Signum.Mailing;
using Signum.Mailing.Templates;
using Signum.Processes;
using Signum.Scheduler;
using Signum.Templating;
using Signum.UserQueries;
using Signum.Utilities;

namespace Signum.Mailing.Package;

public static class SendEmailTaskLogic
{
    [AutoExpressionField]
    public static IQueryable<EmailMessageEntity> Messages(this EmailPackageEntity p) =>
    As.Expression(() => Database.Query<EmailMessageEntity>().Where(a => a.Mixin<EmailMessagePackageMixin>().Package.Is(p)));


    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

        sb.Include<SendEmailTaskEntity>()
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Name,
                e.EmailTemplate,
                e.UniqueTarget,
            });

        Validator.PropertyValidator((SendEmailTaskEntity er) => er.TargetFrom).StaticPropertyValidation += (er, pi) =>
        {
            if (er.EmailTemplate != null)
            {
                var query = er.EmailTemplate.InDB(a => a.Query);

                if (query != null && er.TargetFrom == EmaiTemplateTargetFrom.NoTarget)
                {
                    return ValidationMessage._0ShouldBe12.NiceToString(pi.NiceName(), ComparisonType.EqualTo, new[] { EmaiTemplateTargetFrom.Unique, EmaiTemplateTargetFrom.UserQuery }.CommaOr(a => a.NiceToString()));
                }
                else if(query == null && er.TargetFrom != EmaiTemplateTargetFrom.NoTarget)
                {
                    return ValidationMessage._0ShouldBe12.NiceToString(pi.NiceName(), ComparisonType.EqualTo, EmaiTemplateTargetFrom.NoTarget.NiceToString());
                }
            }

            return null;
        };

        Validator.PropertyValidator((SendEmailTaskEntity er) => er.UniqueTarget).StaticPropertyValidation += (er, pi) =>
          {
              if (er.EmailTemplate != null && er.UniqueTarget != null)
              {
                  var query = er.EmailTemplate.InDB(a => a.Query);
                  if (query != null)
                  {
                      Implementations? implementations = GetImplementations(query);
                      if (!implementations!.Value.Types.Contains(er.UniqueTarget.EntityType))
                          return ValidationMessage._0ShouldBeOfType1.NiceToString(pi.NiceName(), implementations.Value.Types.CommaOr(t => t.NiceName()));
                  }
              }

              return null;
          };

        Validator.PropertyValidator((SendEmailTaskEntity er) => er.TargetsFromUserQuery).StaticPropertyValidation += (SendEmailTaskEntity er, PropertyInfo pi) =>
        {
            if (er.TargetsFromUserQuery != null && er.EmailTemplate != null)
            {
                var query = er.EmailTemplate.InDB(a => a.Query);
                if (query != null)
                {
                    Implementations? emailImplementations = GetImplementations(query);
                    var uqImplementations = GetImplementations(er.TargetsFromUserQuery.InDB(a => a.Query));
                    if (!emailImplementations!.Value.Types.Intersect(uqImplementations!.Value.Types).Any())
                        return ValidationMessage._0ShouldBeOfType1.NiceToString(pi.NiceName(), emailImplementations.Value.Types.CommaOr(t => t.NiceName()));
                }
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
            if (er.TargetFrom == EmaiTemplateTargetFrom.NoTarget)
            {
                Lite<EmailMessageEntity>? last = null;
                foreach (var email in er.EmailTemplate.CreateEmailMessage())
                {
                    email.SendMailAsync();
                    last = email.ToLite();
                }
                return last;
            }
            else if (er.TargetFrom == EmaiTemplateTargetFrom.Unique)
            {
                ModifiableEntity entity = er.UniqueTarget!.RetrieveAndRemember();

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
            else if (er.TargetFrom == EmaiTemplateTargetFrom.UserQuery)
            {
                var qr = er.TargetsFromUserQuery!.RetrieveAndRemember().ToQueryRequest();

                List<Lite<Entity>> entities;

                if (!qr.GroupResults)
                {
                    qr.Columns.Clear();
                    var result = QueryLogic.Queries.ExecuteQuery(qr);

                    entities = result.Rows.Select(a => a.Entity).Distinct().NotNull().ToList();
                }
                else
                {
                    var result = QueryLogic.Queries.ExecuteQuery(qr);

                    var col = result.Columns.FirstOrDefault();
                    if (col == null || !col.Token.Type.IsLite())
                        throw new InvalidOperationException("Grouping UserQueries should have the target entity as first column");

                    entities = result.Rows.Select(row => (Lite<Entity>?)row[col]).Distinct().NotNull().ToList();
                }

                if (entities.IsEmpty())
                    return null;

                return EmailPackageLogic.SendMultipleEmailsAsync(er.EmailTemplate, entities, er.ModelConverter).Execute(ProcessOperation.Execute).ToLite();
            }
            else 
                throw new UnexpectedValueException(er.TargetFrom);
        });
    }

    public static Implementations? GetImplementations(QueryEntity query)
    {
        if (query == null)
            return null;

        var queryName = query?.ToQueryName();

        if (queryName == null)
            return null;

        var entityColumn = QueryLogic.Queries.QueryDescription(queryName).Columns.Single(a => a.IsEntity);
        var implementations = entityColumn.Implementations!.Value;

        if (implementations.IsByAll)
            throw new InvalidOperationException("ByAll implementations not supported");

        return implementations;
    }
}
