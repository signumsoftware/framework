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

            var exceptions= ex.FollowC(e => e.InnerException);
            string innerMessages = exceptions.ToString(e => e.Message, "\r\n\r\n");
            string innerStackTrace = exceptions.ToString(e => e.StackTrace, "\r\n\r\n");

            if (innerMessages.HasText())
                innerMessages = "\r\n\r\n {0}".Formato(innerMessages);


            if (innerStackTrace.HasText())
                innerStackTrace = "\r\n\r\n {0}".Formato(innerStackTrace);

            entity.ExceptionMessage = (ex.Message + innerMessages).DefaultText("- No message - ");
            entity.StackTrace = (ex.StackTrace + innerStackTrace).DefaultText("- No stacktrace -");
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
    }
}
