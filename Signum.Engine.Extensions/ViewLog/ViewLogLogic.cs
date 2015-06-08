using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Engine.Basics;
using Signum.Entities.ViewLog;
using Signum.Entities.DynamicQuery;
using System.IO;

namespace Signum.Engine.ViewLog
{
    public static class ViewLogLogic
    {
        public static Func<DynamicQueryManager.ExecuteType, object, BaseQueryRequest, IDisposable> QueryExecutedLog;

        static Expression<Func<Entity, IQueryable<ViewLogEntity>>> ViewLogsExpression =
            a => Database.Query<ViewLogEntity>().Where(log => log.Target.RefersTo(a));
        public static IQueryable<ViewLogEntity> ViewLogs(this Entity a)
        {
            return ViewLogsExpression.Evaluate(a);
        }

        public static Func<Type, bool> LogType = type => true;
        public static Func<BaseQueryRequest, DynamicQueryManager.ExecuteType, bool> LogQuery = (request, type) => true;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, HashSet<Type> registerExpression)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ViewLogEntity>();

                dqm.RegisterQuery(typeof(ViewLogEntity), () =>
                    from e in Database.Query<ViewLogEntity>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.Target,
                        e.ViewAction,
                        e.User,
                        e.Duration,
                        e.StartDate,
                        e.EndDate,
                    });

                ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;

                var exp = Signum.Utilities.ExpressionTrees.Linq.Expr((Entity entity) => entity.ViewLogs());

                foreach (var t in registerExpression)
                {
                    dqm.RegisterExpression(new ExtensionInfo(t, exp, exp.Body.Type, "ViewLogs", () => typeof(ViewLogEntity).NicePluralName()));
                }

                DynamicQueryManager.Current.QueryExecuted += Current_QueryExecuted;
            }
        }

        static IDisposable Current_QueryExecuted(DynamicQueryManager.ExecuteType type, object queryName, BaseQueryRequest request)
        {
            if (request == null || !LogQuery(request, type))
                return null;

            var old = Connector.CurrentLogger;

            StringWriter sw = new StringWriter();

            Connector.CurrentLogger = old == null ? (TextWriter)sw : new DuplicateTextWriter(sw, old);

            var viewLog = new ViewLogEntity
            {
                Target = QueryLogic.GetQueryEntity(queryName).ToLite(),
                User = UserHolder.Current.ToLite(),
                ViewAction = type.ToString(),
            };

            return new Disposable(() =>
            {
                try
                {
                    viewLog.EndDate = TimeZoneManager.Now;
                    viewLog.Data = request.QueryUrl + "\r\n\r\n" + sw.ToString();
                    using (ExecutionMode.Global())
                        viewLog.Save();
                }
                finally
                {
                    Connector.CurrentLogger = old;
                }
            });
        }

        static void ExceptionLogic_DeleteLogs(DeleteLogParametersEntity parameters)
        {
            Database.Query<ViewLogEntity>().Where(view => view.StartDate < parameters.DateLimit).UnsafeDeleteChunks(parameters.ChunkSize, parameters.MaxChunks);
        }

        public static IDisposable LogView(Lite<IEntity> entity, string viewAction)
        {
            var viewLog = new ViewLogEntity
            {
                Target = (Lite<Entity>)entity,
                User = UserHolder.Current.ToLite(),
                ViewAction = viewAction,
            };

            return new Disposable(() =>
            {
                viewLog.EndDate = TimeZoneManager.Now;
                using (ExecutionMode.Global())
                    viewLog.Save();
            });
        }
    }

    public class DuplicateTextWriter : TextWriter
    {
        public TextWriter First;
        public TextWriter Second; 

        public DuplicateTextWriter(TextWriter first, TextWriter second)
        {
            this.First = first;
            this.Second = second;
        }

        public override void Write(char[] buffer, int index, int count)
        {
            First.Write(buffer, index, count);
            Second.Write(buffer, index, count);
        }

        public override void Write(string value)
        {
            First.Write(value);
            Second.Write(value);
        }

        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }
    }
}
