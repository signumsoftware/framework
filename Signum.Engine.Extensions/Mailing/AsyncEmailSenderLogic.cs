using Signum.Utilities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.Mailing;
using Signum.Engine.Authorization;
using Signum.Engine.Cache;
using System.Data.SqlClient;
using Signum.Engine.Scheduler;
using Signum.Entities.Basics;

namespace Signum.Engine.Mailing
{
    public static class AsyncEmailSenderLogic
    {
        static Timer timer = null!;
        internal static DateTime? nextPlannedExecution;
        static bool running = false;
        static CancellationTokenSource CancelProcess = null!;
        static long queuedItems;
        static Guid processIdentifier;
        static AutoResetEvent autoResetEvent = new AutoResetEvent(false);

        public static AsyncEmailSenderState ExecutionState()
        {
            return new AsyncEmailSenderState
            {
                Running = running,
                CurrentProcessIdentifier = processIdentifier,
                AsyncSenderPeriod = EmailLogic.Configuration.AsyncSenderPeriod,
                NextPlannedExecution = nextPlannedExecution,
                IsCancelationRequested = CancelProcess != null && CancelProcess.IsCancellationRequested,
                QueuedItems = queuedItems,
            };
        }

        public static void StartRunningEmailSenderAsync(int initialDelayMilliseconds)
        {
            using (ExecutionContext.SuppressFlow())
                Task.Run(() =>
                {
                    Thread.Sleep(initialDelayMilliseconds);
                    ExecuteProcess();
                });
        }

        private static void ExecuteProcess()
        {
            if (running)
                throw new InvalidOperationException("EmailAsyncSender process is already running");


            using (ExecutionContext.SuppressFlow())
            {
                Task.Factory.StartNew(() =>
                {
                    SystemEventLogLogic.Log("Start AsyncEmailSender");
                    ExceptionEntity? exception = null;
                    try
                    {
                        running = true;
                        CancelProcess = new CancellationTokenSource();
                        autoResetEvent.Set();

                        timer = new Timer(ob => WakeUp("TimerNextExecution", null),
                             null,
                             Timeout.Infinite,
                             Timeout.Infinite);

                        GC.KeepAlive(timer);

                        using (AuthLogic.Disable())
                        {
                            if (EmailLogic.Configuration.AvoidSendingEmailsOlderThan.HasValue)
                            {
                                DateTime firstDate = TimeZoneManager.Now.AddHours(-EmailLogic.Configuration.AvoidSendingEmailsOlderThan.Value);
                                Database.Query<EmailMessageEntity>().Where(m =>
                                    m.State == EmailMessageState.ReadyToSend &&
                                    m.CreationDate < firstDate).UnsafeUpdate()
                                    .Set(m => m.State, m => EmailMessageState.Outdated)
                                    .Execute();
                            }

                            if (CacheLogic.WithSqlDependency)
                            {
                                SetSqlDepndency();
                            }

                            while (autoResetEvent.WaitOne())
                            {
                                if (!EmailLogic.Configuration.SendEmails)
                                    throw new ApplicationException("Email configuration does not allow email sending");

                                if (CancelProcess.IsCancellationRequested)
                                    return;

                                timer.Change(Timeout.Infinite, Timeout.Infinite);
                                nextPlannedExecution = null;

                                using (HeavyProfiler.Log("EmailAsyncSender", () => "Execute process"))
                                {
                                    processIdentifier = Guid.NewGuid();
                                    if (RecruitQueuedItems())
                                    {
                                        while (queuedItems > 0 || RecruitQueuedItems())
                                        {
                                            var items = Database.Query<EmailMessageEntity>().Where(m =>
                                                m.ProcessIdentifier == processIdentifier &&
                                                m.State == EmailMessageState.RecruitedForSending)
                                                .Take(EmailLogic.Configuration.ChunkSizeSendingEmails).ToList();
                                            queuedItems = items.Count;
                                            foreach (var email in items)
                                            {
                                                CancelProcess.Token.ThrowIfCancellationRequested();

                                                try
                                                {
                                                    using (Transaction tr = Transaction.ForceNew())
                                                    {
                                                        EmailLogic.SenderManager.Send(email);
                                                        tr.Commit();
                                                    }
                                                }
                                                catch
                                                {
                                                    try
                                                    {
                                                        if (email.SendRetries < EmailLogic.Configuration.MaxEmailSendRetries)
                                                        {
                                                            using (Transaction tr = Transaction.ForceNew())
                                                            {
                                                                var nm = email.ToLite().RetrieveAndRemember();
                                                                nm.SendRetries += 1;
                                                                nm.State = EmailMessageState.ReadyToSend;
                                                                nm.Save();
                                                                tr.Commit();
                                                            }
                                                        }
                                                    }
                                                    catch { }
                                                }
                                                queuedItems--;
                                            }
                                            queuedItems = Database.Query<EmailMessageEntity>().Where(m =>
                                                m.ProcessIdentifier == processIdentifier &&
                                                m.State == EmailMessageState.RecruitedForSending).Count();
                                        }
                                    }
                                    SetTimer();
                                    SetSqlDepndency();
                                }
                            }
                        }
                    }
                    catch (ThreadAbortException)
                    {

                    }
                    catch (Exception e)
                    {
                        try
                        {
                            exception = e.LogException(edn =>
                            {
                                edn.ControllerName = "EmailAsyncSender";
                                edn.ActionName = "ExecuteProcess";
                            });
                        }
                        catch { }
                    }
                    finally
                    {
                        SystemEventLogLogic.Log("Stop AsyncEmailSender", exception);
                        running = false;
                    }
                }, TaskCreationOptions.LongRunning);
            }
        }

        static bool sqlDependencyRegistered = false;
        private static void SetSqlDepndency()
        {
			if(!sqlDependencyRegistered)
				return;
			
            var query = Database.Query<EmailMessageEntity>().Where(m => m.State == EmailMessageState.ReadyToSend).Select(m => m.Id);
            sqlDependencyRegistered = true;
            query.ToListWithInvalidation(typeof(EmailMessageEntity), "EmailAsyncSender ReadyToSend dependency", a => {
                sqlDependencyRegistered = false;
                WakeUp("EmailAsyncSender ReadyToSend dependency", a);
            });
        }

        private static bool RecruitQueuedItems()
        {
            DateTime? firstDate = EmailLogic.Configuration.AvoidSendingEmailsOlderThan == null ?
                null : (DateTime?)TimeZoneManager.Now.AddHours(-EmailLogic.Configuration.AvoidSendingEmailsOlderThan.Value);

            queuedItems = Database.Query<EmailMessageEntity>().Where(m =>
                m.State == EmailMessageState.ReadyToSend &&
                m.CreationDate  < TimeZoneManager.Now &&
                (firstDate == null ? true : m.CreationDate >= firstDate)).UnsafeUpdate()
                    .Set(m => m.ProcessIdentifier, m => processIdentifier)
                    .Set(m => m.State, m => EmailMessageState.RecruitedForSending)
                    .Execute();
            return queuedItems > 0;
        }

        internal static bool WakeUp(string reason, SqlNotificationEventArgs? args)
        {
            using (HeavyProfiler.Log("EmailAsyncSender WakeUp", () => "WakeUp! " + reason + ToString(args)))
            {
                return autoResetEvent.Set();
            }
        }

        private static string? ToString(SqlNotificationEventArgs? args)
        {
            if (args == null)
                return null;

            return " ({0} {1} {2})".FormatWith(args.Type, args.Source, args.Info);
        }

        private static void SetTimer()
        {
            nextPlannedExecution = TimeZoneManager.Now.AddMilliseconds(EmailLogic.Configuration.AsyncSenderPeriod * 1000);
            timer!.Change(EmailLogic.Configuration.AsyncSenderPeriod * 1000, Timeout.Infinite);
        }

        public static void Stop()
        {
            if (!running)
                throw new InvalidOperationException("EmailAsyncSender is not running");

            using (HeavyProfiler.Log("EmailAsyncSender", () => "Stopping process"))
            {
                timer!.Dispose();
                CancelProcess!.Cancel();
                WakeUp("Stop", null);
                nextPlannedExecution = null;
            }
        }
    }


    public class AsyncEmailSenderState
    {
        public int AsyncSenderPeriod;
        public bool Running;
        public bool IsCancelationRequested;
        public DateTime? NextPlannedExecution;
        public long QueuedItems;
        public Guid CurrentProcessIdentifier;
    }
}
