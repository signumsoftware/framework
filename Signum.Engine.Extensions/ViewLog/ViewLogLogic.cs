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
using Signum.Utilities.ExpressionTrees;
using System.Data.Common;
using Signum.Engine;
using Signum.Entities.Authorization;
using System.Threading;

namespace Signum.Engine.ViewLog
{
    public static class ViewLogLogic
    {
        public static Func<DynamicQueryManager.ExecuteType, object, BaseQueryRequest, IDisposable> QueryExecutedLog;

        static Expression<Func<Entity, IQueryable<ViewLogEntity>>> ViewLogsExpression =
            a => Database.Query<ViewLogEntity>().Where(log => log.Target.RefersTo(a));
        [ExpressionField]
        public static IQueryable<ViewLogEntity> ViewLogs(this Entity a)
        {
            return ViewLogsExpression.Evaluate(a);
        }
        
        static Expression<Func<Entity, ViewLogEntity>> ViewLogMyLastExpression =
            e => Database.Query<ViewLogEntity>()
            .Where(a => a.User.RefersTo(UserEntity.Current) && a.Target.RefersTo(e))
            .OrderBy(a => a.StartDate).FirstOrDefault();     
        [ExpressionField]
        public static ViewLogEntity ViewLogMyLast(this Entity e)
        {
            return ViewLogMyLastExpression.Evaluate(e);
        }

        public static Func<Type, bool> LogType = type => true;
        public static Func<BaseQueryRequest, DynamicQueryManager.ExecuteType, bool> LogQuery = (request, type) => true;
        public static Func<BaseQueryRequest, StringWriter, string> GetData = (request, sw) => request.QueryUrl + "\r\n\r\n" + sw.ToString();
      

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, HashSet<Type> registerExpression)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ViewLogEntity>()
                    .WithQuery(dqm, () => e => new
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
                var expLast = Signum.Utilities.ExpressionTrees.Linq.Expr((Entity entity) => entity.ViewLogMyLast());

                foreach (var t in registerExpression)
                {
                    dqm.RegisterExpression(new ExtensionInfo(t, exp, exp.Body.Type, "ViewLogs", () => typeof(ViewLogEntity).NicePluralName()));
                    dqm.RegisterExpression(new ExtensionInfo(t, expLast, expLast.Body.Type, "LastViewLog", () => ViewLogMessage.ViewLogMyLast.NiceToString()));
                }

                DynamicQueryManager.Current.QueryExecuted += Current_QueryExecuted;
                sb.Schema.Table<TypeEntity>().PreDeleteSqlSync += Type_PreDeleteSqlSync;
            }
        }


        static SqlPreCommand Type_PreDeleteSqlSync(Entity arg)
        {
            var t = Schema.Current.Table<ViewLogEntity>();
            var f = ((FieldImplementedByAll)Schema.Current.Field((ViewLogEntity vl) => vl.Target)).ColumnType;
            return Administrator.DeleteWhereScript(t, f, arg.Id);
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
                User = UserHolder.Current?.ToLite(),
                ViewAction = type.ToString(),
            };

            return new Disposable(() =>
            {
                try
                {
                    using (Transaction tr = Transaction.ForceNew())
                    {

                        viewLog.EndDate = TimeZoneManager.Now;
                         viewLog.Data = GetData(request, sw);
                        using (ExecutionMode.Global())
                            viewLog.Save();
                        tr.Commit();
                    }

                }
                finally
                {
                    Connector.CurrentLogger = old;
                }
            });
        }

        static void ExceptionLogic_DeleteLogs(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
        {
            var dateLimit = parameters.GetDateLimitDelete(typeof(ViewLogEntity).ToTypeEntity());

            if (dateLimit == null)
                return;

            Database.Query<ViewLogEntity>().Where(view => view.StartDate < dateLimit.Value).UnsafeDeleteChunksLog(parameters, sb, token);
        }

        public static IDisposable LogView(Lite<IEntity> entity, string viewAction)
        {
            var viewLog = new ViewLogEntity
            {
                Target = (Lite<Entity>)entity.Clone(),
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
