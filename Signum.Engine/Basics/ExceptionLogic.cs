using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using System.Reflection;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Basics;
using System.Threading;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Basics
{
    public static class ExceptionLogic
    {
        public static Func<string> GetCurrentVersion;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ExceptionDN>();

                dqm.RegisterQuery(typeof(ExceptionDN),()=>
                    from r in Database.Query<ExceptionDN>()
                    select new
                    {
                        Entity = r,
                        r.Id,
                        r.CreationDate,
                        r.ExceptionType,
                        ExcepcionMessage = r.ExceptionMessage,
                        r.StackTraceHash,
                    });

                dqm.RegisterQuery(typeof(ExceptionDN), ()=>
                     from r in Database.Query<ExceptionDN>()
                     select new
                     {
                         Entity = r,
                         r.Id,
                         r.CreationDate,
                         r.ExceptionType,
                         ExcepcionMessage = r.ExceptionMessage,
                         r.StackTraceHash,
                     });

                DefaultEnvironment = "Default"; 
            }
        }

        public static ExceptionDN LogException(this Exception ex, Action<ExceptionDN> completeContext)
        {
            var entity = GetEntity(ex);
            
            completeContext(entity);

            return entity.SaveForceNew();
        }

        public static ExceptionDN LogException(this Exception ex)
        {
            var entity = GetEntity(ex);

            return entity.SaveForceNew();
        }

        static ExceptionDN PreviousExceptionDN(this Exception ex)
        {
            var exEntity = ex.Data[ExceptionDN.ExceptionDataKey] as ExceptionDN;

            if (exEntity != null)
                return exEntity;

            return null;
        }

        static ExceptionDN GetEntity(Exception ex)
        {
            ExceptionDN entity = ex.PreviousExceptionDN() ?? new ExceptionDN(ex);

            entity.ExceptionType = ex.GetType().Name;

            var exceptions= ex.Follow(e => e.InnerException);
            string messages = exceptions.ToString(e => e.Message, "\r\n\r\n");
            string stacktraces = exceptions.ToString(e => e.StackTrace, "\r\n\r\n");
           
            entity.ExceptionMessage = messages.DefaultText("- No message - ");
            entity.StackTrace = stacktraces.DefaultText("- No stacktrace -");
            entity.ThreadId = Thread.CurrentThread.ManagedThreadId;
            entity.ApplicationName = Schema.Current.ApplicationName;

            entity.Environment = CurrentEnvironment;
            try
            {
                entity.User = UserHolder.Current.ToLite(); //Session special situations
            }
            catch { }

            entity.Data = ex.Data.Dump();
            entity.Version = Schema.Current.Version.ToString();

            return entity;
        }

        static ExceptionDN SaveForceNew(this ExceptionDN entity)
        {
            if (entity.Modified == ModifiedState.Clean)
                return entity;

            using (ExecutionMode.Global())
            using (Transaction tr = Transaction.ForceNew())
            {
                entity.Save();

                return tr.Commit(entity);
            }
        }

        public static string DefaultEnvironment { get; set; }

        public static string CurrentEnvironment { get { return overridenEnvironment.Value ?? DefaultEnvironment; } }

        static readonly Variable<string> overridenEnvironment = Statics.ThreadVariable<string>("exceptionEnviroment");

        public static IDisposable OverrideEnviroment(string newEnviroment)
        {
            string oldEnviroment = overridenEnvironment.Value;
            overridenEnvironment.Value = newEnviroment;
            return new Disposable(() => overridenEnvironment.Value = oldEnviroment);
        }


        public static event Action<DateTime> DeleteLogs;

        public static int DeleteLogsTimeOut = 10 * 60 * 1000; 

        public static void DeleteLogsAndExceptions(DateTime limitDate)
        {
            using(Connector.CommandTimeoutScope(DeleteLogsTimeOut))
            {
                if(DeleteLogs != null)
                {
                    foreach (var action in DeleteLogs.GetInvocationList().Cast<Action<DateTime>>())
	                {
                        action(limitDate);
	                }
                }

                int exceptions = Database.Query<ExceptionDN>().UnsafeUpdate().Set(a => a.Referenced, a => false).Execute();

                var ex = Schema.Current.Table<ExceptionDN>();
                var referenced = (FieldValue)ex.GetField(ReflectionTools.GetPropertyInfo((ExceptionDN e)=>e.Referenced));

                var commands = (from t in Schema.Current.GetDatabaseTables()
                               from c in t.Columns.Values
                               where c.ReferenceTable == ex
                                select new SqlPreCommandSimple("UPDATE ex SET {1} = 1 FROM {0} ex JOIN {2} log ON ex.Id = log.{3}"
                                   .Formato(ex.Name, referenced.Name, t.Name, c.Name))).ToList();

                foreach (var c in commands) 
                {
                    c.ExecuteNonQuery();
                }

                int deletedExceptions = Database.Query<ExceptionDN>()
                    .Where(a => !a.Referenced && a.CreationDate < limitDate)
                    .UnsafeDeleteChunks(); 
            }
        }
    }
}
