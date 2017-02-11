using Signum.Entities.Workflow;
using Signum.Engine;
using Signum.Engine.Dynamic;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Signum.Engine.Maps.SchemaBuilder;
using Signum.Entities.Dynamic;
using Signum.Engine.Basics;
using Signum.Entities.DynamicQuery;

namespace Signum.Engine.Workflow
{
    public static class CaseActivityLogic
    {

        static Expression<Func<WorkflowEntity, IQueryable<CaseEntity>>> CasesExpression =
            w => Database.Query<CaseEntity>().Where(a => a.Workflow == w);
        [ExpressionField]
        public static IQueryable<CaseEntity> Cases(this WorkflowEntity e)
        {
            return CasesExpression.Evaluate(e);
        }


        static Expression<Func<CaseActivityEntity, IQueryable<CaseActivityEntity>>> NextActivitiesExpression =
            ca => Database.Query<CaseActivityEntity>().Where(a => a.Previous.RefersTo(ca));
        [ExpressionField]
        public static IQueryable<CaseActivityEntity> NextActivities(this CaseActivityEntity e)
        {
            return NextActivitiesExpression.Evaluate(e);
        }



        static Expression<Func<CaseEntity, CaseActivityEntity>> DecompositionSurrogateActivityExpression =
          childCase => Database.Query<CaseActivityEntity>()
          .Where(ca => ca.Case.ToLite() == childCase.ParentCase)
          .SingleOrDefaultEx(ca => ca.WorkflowActivity.Decomposition.Workflow == childCase.Workflow);
        [ExpressionField]
        public static CaseActivityEntity DecompositionSurrogateActivity(this CaseEntity childCase)
        {
            return DecompositionSurrogateActivityExpression.Evaluate(childCase);
        }




        static Expression<Func<CaseActivityEntity, bool>> IsFreshNewExpression =
        ca => (ca.State == CaseActivityState.PendingNext || ca.State == CaseActivityState.PendingDecision) && 
                ca.Notifications().All(n => n.State == CaseNotificationState.New);
        [ExpressionField]
        public static bool IsFreshNew(this CaseActivityEntity entity)
        {
            return IsFreshNewExpression.Evaluate(entity);
        }
  

        static Expression<Func<WorkflowActivityEntity, IQueryable<CaseActivityEntity>>> CaseActivitiesFromWorkflowActivityExpression =
            e => Database.Query<CaseActivityEntity>().Where(a => a.WorkflowActivity == e);
        [ExpressionField]
        public static IQueryable<CaseActivityEntity> CaseActivities(this WorkflowActivityEntity e)
        {
            return CaseActivitiesFromWorkflowActivityExpression.Evaluate(e);
        }

        static Expression<Func<CaseEntity, IQueryable<CaseActivityEntity>>> CaseActivitiesFromCaseExpression =
            e => Database.Query<CaseActivityEntity>().Where(a => a.Case == e);
        [ExpressionField]
        public static IQueryable<CaseActivityEntity> CaseActivities(this CaseEntity e)
        {
            return CaseActivitiesFromCaseExpression.Evaluate(e);
        }

        static Expression<Func<CaseActivityEntity, IQueryable<CaseNotificationEntity>>> NotificationsExpression =
            e => Database.Query<CaseNotificationEntity>().Where(a => a.CaseActivity.RefersTo(e));
        [ExpressionField]
        public static IQueryable<CaseNotificationEntity> Notifications(this CaseActivityEntity e)
        {
            return NotificationsExpression.Evaluate(e);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<CaseEntity>()
                    .WithExpressionFrom(dqm, (WorkflowEntity w) => w.Cases())
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Description,
                    });

                sb.Include<CaseActivityEntity>()
                    .WithExpressionFrom(dqm, (WorkflowActivityEntity c) => c.CaseActivities())
                    .WithExpressionFrom(dqm, (CaseEntity c) => c.CaseActivities())
                    .WithExpressionFrom(dqm, (CaseActivityEntity c) => c.NextActivities())
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.StartDate,
                        e.DoneDate,
                        e.DoneBy,
                        e.Previous,
                        e.Case,
                        e.WorkflowActivity,
                    });

                dqm.RegisterExpression((CaseEntity c) => c.DecompositionSurrogateActivity());

                sb.Include<CaseNotificationEntity>()
                    .WithExpressionFrom(dqm, (CaseActivityEntity c) => c.Notifications())
                    .WithQuery(dqm, e => new
                    {
                        Entity = e,
                        e.Id,
                        e.CaseActivity.Entity.StartDate,
                        e.State,
                        e.CaseActivity,
                        e.User,
                    });


                new Graph<CaseNotificationEntity>.Execute(CaseNotificationOperation.SetRemarks)
                {
                    Execute = (e, args) =>
                    {
                        e.Remarks = args.GetArg<string>();
                    },
                }.Register();


                dqm.RegisterQuery(CaseActivityQuery.Inbox, () =>
                        from cn in Database.Query<CaseNotificationEntity>()
                        where cn.User == UserEntity.Current.ToLite()
                        let ca = cn.CaseActivity.Entity
                        let previous = ca.Previous.Entity
                        select new
                        {
                            Entity = cn.CaseActivity,
                            ca.StartDate,
                            Activity = new ActivityWithRemarks
                            {
                                activity = ca.WorkflowActivity.ToLite(),
                                notification = cn.ToLite(),
                                remarks = cn.Remarks
                            },
                            Case = ca.Case.Description,
                            Sender = previous.DoneBy,
                            SenderNote = previous.Note,
                            cn.State,
                            cn.Actor,
                        });

                sb.Schema.WhenIncluded<DynamicTypeEntity>(() =>
                {
                    new Graph<DynamicTypeEntity>.Execute(CaseActivityOperation.FixCaseDescriptions)
                    {
                        Execute = (e, _) =>
                        {
                            var type = TypeLogic.GetType(e.TypeName);
                            giFixCaseDescriptions.GetInvoker(type)();
                        },
                    }.Register();
                });

                CaseActivityGraph.Register();
            }
        }

        public class ActivityWithRemarks : IQueryTokenBag
        {
            public Lite<WorkflowActivityEntity> activity { get; set; }
            public Lite<CaseNotificationEntity> notification { get; set; }
            public string remarks { get; set; }
        }

        static readonly GenericInvoker<Action> giFixCaseDescriptions = new GenericInvoker<Action>(() => FixCaseDescriptions<Entity>());
        public static void FixCaseDescriptions<T>() where T : Entity
        {
            Database.Query<CaseEntity>()
                          .Where(a => a.MainEntity.GetType() == typeof(T))
                          .UnsafeUpdate()
                          .Set(a => a.Description, a => ((T)a.MainEntity).ToString())
                          .Execute();
        }

        public static Dictionary<Type, WorkflowOptions> Options = new Dictionary<Type, WorkflowOptions>(); 

        public class WorkflowOptions
        {
            public Func<ICaseMainEntity> Constructor;
            public Action<ICaseMainEntity> SaveEntity;
        }
        
        public static FluentInclude<T> WithWorkflow<T>(this FluentInclude<T> fi, Func<T> constructor, Action<T> save)
            where T: Entity, ICaseMainEntity
        {
            fi.SchemaBuilder.Schema.EntityEvents<T>().Saved += (e, args)=>
            {
                if (AvoidNotifyInProgressVariable.Value == true)
                    return;

                e.NotifyInProgress();
            };

            Options[typeof(T)] = new WorkflowOptions
            {
                Constructor = constructor,
                SaveEntity = e => save((T)e)
            };

            return fi; 
        }

        static IDisposable AvoidNotifyInProgress()
        {
            var old = AvoidNotifyInProgressVariable.Value;
            AvoidNotifyInProgressVariable.Value = true;
            return new Disposable(() => AvoidNotifyInProgressVariable.Value = old);
        }
        
        static ThreadVariable<bool> AvoidNotifyInProgressVariable = Statics.ThreadVariable<bool>("avoidNotifyInProgress");

        public static int NotifyInProgress(this ICaseMainEntity mainEntity)
        {
            return Database.Query<CaseNotificationEntity>()
                .Where(n => n.CaseActivity.Entity.Case.MainEntity == mainEntity && n.CaseActivity.Entity.DoneDate == null)
                .Where(n => n.User == UserEntity.Current.ToLite() && (n.State == CaseNotificationState.New || n.State == CaseNotificationState.Opened))
                .UnsafeUpdate()
                .Set(n => n.State, n => CaseNotificationState.InProgress)
                .Execute();
        }

        public class WorkflowExecuteStepContext
        {
            public DecisionResult? DecisionResult;
            public CaseActivityEntity CaseActivity;
            public List<WorkflowActivityEntity> ToActivities = new List<WorkflowActivityEntity>();
            public bool IsFinished { get; set; }
            public List<IWorkflowConnectionOrJump> Connections = new List<IWorkflowConnectionOrJump>();
        }

        static bool Applicable(this WorkflowConnectionEntity wc, WorkflowExecuteStepContext ctx)
        {
            if (wc.DecisonResult != null && wc.DecisonResult != ctx.DecisionResult)
                return false;

            if (wc.Condition != null)
            {
                var alg = wc.Condition.RetrieveFromCache().Eval.Algorithm;
                var result = alg.EvaluateUntyped(ctx.CaseActivity.Case.MainEntity, new WorkflowEvaluationContext(ctx.CaseActivity, wc, ctx.DecisionResult));


                return result;
            }
            
            return true;
        }

        static void WorkflowAction(ICaseMainEntity me, WorkflowEvaluationContext ctx)
        {
            WorkflowLogic.OnTransition?.Invoke(me, ctx);

            if (ctx.Connection.Action != null)
            {
                var alg = ctx.Connection.Action.RetrieveFromCache().Eval.Algorithm;
                alg.ExecuteUntyped(me, ctx);
            };
        }

        static void SaveEntity(ICaseMainEntity mainEntity)
        {
            var options = CaseActivityLogic.Options.GetOrThrow(mainEntity.GetType());
            using (AvoidNotifyInProgress())
                options.SaveEntity(mainEntity);
        }

        public static CaseActivityEntity RetrieveForViewing(Lite<CaseActivityEntity> lite)
        {
            var ca = lite.Retrieve();

            if (ca.DoneBy == null)
                ca.Notifications()
                  .Where(n => n.User == UserEntity.Current.ToLite() && n.State == CaseNotificationState.New)
                  .UnsafeUpdate()
                  .Set(a => a.State, a => CaseNotificationState.Opened)
                  .Execute();

            return ca;
        }

        static void InsertCaseActivityNotifications(CaseActivityEntity caseActivity)
        {
            var lane = caseActivity.WorkflowActivity.Lane;
            var actors = lane.Actors.ToList();
            if (lane.ActorsEval != null)
                actors.AddRange(lane.ActorsEval.Algorithm.GetActors(caseActivity.Case.MainEntity, new WorkflowEvaluationContext(caseActivity, null, null)).EmptyIfNull().NotNull());

            var notifications = actors.Distinct().SelectMany(a =>
            Database.Query<UserEntity>()
            .Where(u => WorkflowLogic.IsCurrentUserActor.Evaluate(a, u))
            .Select(u => new CaseNotificationEntity
            {
                CaseActivity = caseActivity.ToLite(),
                Actor = a,
                State = CaseNotificationState.New,
                User = u.ToLite()
            })).ToList();

            notifications.BulkInsert();
        }

        static CaseActivityEntity InsertNewCaseActivity(CaseActivityEntity currentCaseActivity, WorkflowActivityEntity workflowActivity)
        {
            return new CaseActivityEntity
            {
                StartDate = currentCaseActivity.DoneDate.Value,
                Previous = currentCaseActivity.ToLite(),
                WorkflowActivity = workflowActivity,
                OriginalWorkflowActivityName = workflowActivity.Name,
                Case = currentCaseActivity.Case
            }.Save();
        }

        class CaseActivityGraph : Graph<CaseActivityEntity, CaseActivityState>
        {
            public static void Register()
            {
                GetState = ca => ca.State;
                new ConstructFrom<WorkflowEntity>(CaseActivityOperation.CreateCaseFromWorkflow)
                {
                    ToStates = { CaseActivityState.New},
                    Construct = (w, args) =>
                    {
                        var mainEntity = args.TryGetArgC<ICaseMainEntity>() ?? CaseActivityLogic.Options.GetOrThrow(w.MainEntityType.ToType()).Constructor();

                        var @case = new CaseEntity
                        {
                            ParentCase = args.TryGetArgC<Lite<CaseEntity>>(),
                            Workflow = w,
                            Description = w.Name,
                            MainEntity = mainEntity,
                        };

                        var start = w.WorkflowEvents().Single(a => a.Type == WorkflowEventType.Start);
                        var connection = start.NextConnectionsFromCache().SingleEx();
                        var next = (WorkflowActivityEntity)connection.To;
                        var ca = new CaseActivityEntity
                        {
                            WorkflowActivity = next,
                            OriginalWorkflowActivityName = next.Name,
                            Case = @case,
                        };

                        WorkflowAction(@case.MainEntity, new WorkflowEvaluationContext(ca, connection, null));
                       
                        return ca;
                    }
                }.Register();

                new Execute(CaseActivityOperation.Register)
                {
                    FromStates = {  CaseActivityState.New },
                    ToStates = {  CaseActivityState.PendingNext, CaseActivityState.PendingDecision},
                    AllowsNew = true,
                    Lite = false,
                    Execute = (ca, _) =>
                    {
                        SaveEntity(ca.Case.MainEntity);
                        var now = TimeZoneManager.Now;
                        var c = ca.Case;
                        c.StartDate = now;
                        c.Description = ca.Case.MainEntity.ToString().Trim();
                        c.Save();

                        var prev = ca.WorkflowActivity.PreviousConnectionsFromCache().SingleEx(a => a.From is WorkflowEventEntity && ((WorkflowEventEntity)a.From).Type == WorkflowEventType.Start);
                        WorkflowAction(ca.Case.MainEntity, new WorkflowEvaluationContext(ca, prev, null));

                        ca.StartDate = now;
                        ca.Save();

                        InsertCaseActivityNotifications(ca);
                    }
                }.Register();

                new Delete(CaseActivityOperation.Delete)
                {
                    FromStates = { CaseActivityState.PendingDecision, CaseActivityState.PendingNext },
                    CanDelete = ca => ca.Case.ParentCase != null ? CaseActivityMessage.CaseIsADecompositionOf0.NiceToString(ca.Case.ParentCase) :
                    ca.Case.CaseActivities().Any(a => a != ca) ? CaseActivityMessage.CaseContainsOtherActivities.NiceToString() : 
                    null,
                    Delete = (ca, _) =>
                    {
                        var c = ca.Case;
                        ca.Notifications().UnsafeDelete();
                        ca.Delete();
                        c.Delete();
                        c.MainEntity.Delete();
                    },
                }.Register();

                new Execute(CaseActivityOperation.Approve)
                {
                    FromStates = {  CaseActivityState.PendingDecision },
                    ToStates = {  CaseActivityState.Done },
                    Lite = false,
                    Execute = (ca, _) =>
                    {
                        CheckRequiresOpen(ca);
                        ExecuteStep(ca, DecisionResult.Approve, null);
                    },
                }.Register();

                new Execute(CaseActivityOperation.Decline)
                {
                    FromStates = { CaseActivityState.PendingDecision },
                    ToStates = { CaseActivityState.Done },
                    Lite = false,
                    Execute = (ca, _) =>
                    {
                        CheckRequiresOpen(ca);
                        ExecuteStep(ca, DecisionResult.Decline, null);
                    },
                }.Register();

                new Execute(CaseActivityOperation.Next)
                {
                    FromStates = { CaseActivityState.PendingNext },
                    ToStates = { CaseActivityState.Done },
                    Lite = false,
                    Execute = (ca, args) =>
                    {
                        CheckRequiresOpen(ca);
                        ExecuteStep(ca, null, null);
                    },
                }.Register();

                new Execute(CaseActivityOperation.Jump)
                {
                    FromStates = { CaseActivityState.PendingNext, CaseActivityState.PendingDecision },
                    ToStates = { CaseActivityState.Done },
                    CanExecute = a => a.WorkflowActivity.Jumps.Any() ? null : CaseActivityMessage.Activity0HasNoJumps.NiceToString(a.WorkflowActivity),
                    Lite = false,
                    Execute = (ca, args) =>
                    {
                        CheckRequiresOpen(ca);
                        var to = args.GetArg<Lite<IWorkflowNodeEntity>>();
                        WorkflowJumpEntity jump = ca.WorkflowActivity.Jumps.SingleEx(j => j.To.Is(to));
                        ExecuteStep(ca, null, jump);
                    },
                }.Register();

                new Execute(CaseActivityOperation.Reject)
                {
                    FromStates = { CaseActivityState.PendingNext, CaseActivityState.PendingDecision },
                    ToStates = { CaseActivityState.Done },
                    CanExecute = ca => 
                        !ca.WorkflowActivity.CanReject ? 
                        CaseActivityMessage.RejectOperationIsNotAllowed.NiceToString() : 
                            ca.Previous == null ? 
                            CaseActivityMessage.ThereIsNoPreviousForRejectOperation.NiceToString() : null,
                    Lite = false,
                    Execute = (ca, _) =>
                    {
                        var pwa = ca.Previous.Retrieve().WorkflowActivity;
                        ExecuteStep(ca, null, new WorkflowJumpEntity { To = pwa.ToLite() });
                    },
                }.Register();

                new Execute(CaseActivityOperation.MarkAsUnread)
                {
                    FromStates = { CaseActivityState.PendingNext, CaseActivityState.PendingDecision },
                    ToStates = { CaseActivityState.PendingNext, CaseActivityState.PendingDecision },
                    CanExecute = c=> c.Notifications().Any(a=>a.User.RefersTo(UserEntity.Current) && (a.State == CaseNotificationState.InProgress || a.State == CaseNotificationState.Opened)) ? null :
                        CaseActivityMessage.NoOpenedOrInProgressNotificationsFound.NiceToString(),
                    Execute = (ca, args) =>
                    {
                        ca.Notifications()
                        .Where(cn => cn.User.RefersTo(UserEntity.Current) && (cn.State == CaseNotificationState.InProgress || cn.State == CaseNotificationState.Opened))
                        .UnsafeUpdate()
                        .Set(cn => cn.State, cn => CaseNotificationState.New)
                        .Execute();
                    },
                }.Register();

                new Execute(CaseActivityOperation.Undo)
                {
                    FromStates = { CaseActivityState.Done },
                    ToStates = { CaseActivityState.PendingNext, CaseActivityState.PendingDecision },
                    CanExecute = ca =>
                    {
                        if (!ca.DoneBy.Is(UserEntity.Current.ToLite()))
                            return CaseActivityMessage.Only0CanUndoThisOperation.NiceToString(ca.DoneBy);

                        if (!ca.NextActivities().All(na => na.IsFreshNew()))
                            return CaseActivityMessage.NextActivityAlreadyInProgress.NiceToString();

                        if (ca.Case.ParentCase != null && !ca.Case.InDB().SelectMany(c=>c.DecompositionSurrogateActivity().NextActivities()).All(na =>na.IsFreshNew()))
                            return CaseActivityMessage.NextActivityOfDecompositionSurrogateAlreadyInProgress.NiceToString();

                        return null;
                    },
                    Execute = (ca, args) =>
                    {
                        ca.NextActivities().SelectMany(a => a.Notifications()).UnsafeDelete();
                        var cases = ca.NextActivities().Select(a => a.Case).ToList();
                        cases.Remove(ca.Case);
                        ca.NextActivities().UnsafeDelete();
                        //Decomposition
                        if (cases.Any())
                            Database.Query<CaseEntity>().Where(c => cases.Contains(c) && !c.CaseActivities().Any()).UnsafeDelete();

                        //Recomposition
                        if(ca.Case.ParentCase != null && ca.Case.FinishDate.HasValue)
                        {
                            var surrogate = ca.Case.DecompositionSurrogateActivity();
                            surrogate.NextActivities().SelectMany(a => a.Notifications()).UnsafeDelete();
                            surrogate.NextActivities().UnsafeDelete();

                            surrogate.DoneBy = null;
                            surrogate.DoneDate = null;
                            surrogate.Case.FinishDate = null;
                            surrogate.Save();
                        }

                        ca.DoneBy = null;
                        ca.DoneDate = null;
                        ca.Case.FinishDate = null;
                        ca.Notifications()
                           .UnsafeUpdate()
                           .Set(a => a.State, a => CaseNotificationState.New)
                           .Execute();
                    },
                }.Register();
            }

            private static void CheckRequiresOpen(CaseActivityEntity ca)
            {
                if (ca.WorkflowActivity.RequiresOpen)
                {
                    if (!ca.Notifications().Any(cn => cn.User == UserEntity.Current.ToLite() && cn.State != CaseNotificationState.New))
                        throw new ApplicationException(CaseActivityMessage.TheActivity0RequiresToBeOpened.NiceToString(ca.WorkflowActivity));
                }
            }

            private static void ExecuteStep(CaseActivityEntity ca, DecisionResult? decisionResult, WorkflowJumpEntity jump)
            {
                using (DynamicValidationLogic.EnabledRulesExplicitely(ca.WorkflowActivity.ValidationRules
                            .Where(a => decisionResult == null || (decisionResult == DecisionResult.Approve ? a.OnAccept : a.OnDecline))
                            .Select(a => a.Rule)
                            .ToHashSet()))
                {
                    SaveEntity(ca.Case.MainEntity);

                    ca.DoneBy = UserEntity.Current.ToLite();
                    ca.DoneDate = TimeZoneManager.Now;
                    ca.Case.Description = ca.Case.MainEntity.ToString().Trim();
                    ca.Save();

                    ca.Notifications()
                       .UnsafeUpdate()
                       .Set(a => a.State, a => a.User == UserEntity.Current.ToLite() ? CaseNotificationState.Done : CaseNotificationState.DoneByOther)
                       .Execute();

                    var ctx = new WorkflowExecuteStepContext
                    {
                        CaseActivity = ca,
                        DecisionResult = decisionResult,
                    };

                    if (jump != null)
                    {
                        if (jump.Condition != null)
                        {
                            var jumpCtx = new WorkflowEvaluationContext(ctx.CaseActivity, jump, null);
                            var alg = jump.Condition.RetrieveFromCache().Eval.Algorithm;
                            var result = alg.EvaluateUntyped(ctx.CaseActivity.Case.MainEntity, jumpCtx);
                            if (!result)
                                throw new ApplicationException(WorkflowMessage.JumpTo0FailedBecause1.NiceToString(jump.To, jump.Condition));
                        }

                        ctx.Connections.Add(jump);
                        if (!FindNext(jump.To.Retrieve(), ctx))
                            return;
                    }
                    else
                    {
                        var connection = ca.WorkflowActivity.NextConnectionsFromCache().SingleEx();
                        if (!FindNext(connection, ctx))
                            return;
                    }

                    ctx.Connections.ForEach(wc => WorkflowAction(ca.Case.MainEntity, new WorkflowEvaluationContext(ca, wc, ctx.DecisionResult)));

                    ca.Case.Description = ca.Case.MainEntity.ToString().Trim();

                    if (ctx.IsFinished)
                    {
                        if (ctx.ToActivities.Any())
                            throw new InvalidOperationException("ToActivities should be empty when finishing");

                        ca.Case.FinishDate = ca.DoneDate.Value;
                        ca.Case.Save();

                        if (ca.Case.ParentCase != null)
                            TryToRecompose(ca.Case);
                    }
                    else
                    {
                        ca.Case.Save();

                        foreach (var t2 in ctx.ToActivities)
                        {
                            if (t2.Type == WorkflowActivityType.DecompositionTask)
                            {
                                var lastConn =
                                    (IWorkflowConnectionOrJump)ctx.Connections.OfType<WorkflowJumpEntity>().SingleOrDefaultEx() ??
                                    (IWorkflowConnectionOrJump)ctx.Connections.OfType<WorkflowConnectionEntity>().Single(a => a.To.Is(t2));

                                Decompose(ca, t2, lastConn);
                            }
                            else
                            {
                                var nca = InsertNewCaseActivity(ca, t2);
                                InsertCaseActivityNotifications(nca);
                            }
                        }
                    }
                }
            }

            private static void TryToRecompose(CaseEntity childCase)
            {
                if(Database.Query<CaseEntity>().Where(cc => cc.ParentCase.Is(childCase.ParentCase) && cc.Workflow == childCase.Workflow).All(a => a.FinishDate.HasValue))
                {
                    var decompositionCaseActivity = childCase.DecompositionSurrogateActivity();
                    if (decompositionCaseActivity.DoneDate != null)
                        throw new InvalidOperationException("The DecompositionCaseActivity is already finished");
                    
                    var lastActivities = Database.Query<CaseEntity>().Where(c => c.ParentCase.Is(childCase.ParentCase)).Select(c => c.CaseActivities().OrderByDescending(ca => ca.DoneDate).FirstOrDefault()).ToList();
                    decompositionCaseActivity.Note = lastActivities.NotNull().Where(ca => ca.Note.HasText()).ToString(a => $"{a.DoneBy}: {a.Note}", "\r\n");
                    ExecuteStep(decompositionCaseActivity, null, null);
                }
            }

            private static void Decompose(CaseActivityEntity ca, WorkflowActivityEntity decActivity, IWorkflowConnectionOrJump conn)
            {
                var surrogate = InsertNewCaseActivity(ca, decActivity);
                var subEntities = decActivity.Decomposition.SubEntitiesEval.Algorithm.GetSubEntities(ca.Case.MainEntity, new WorkflowEvaluationContext(ca, conn, null));

                if (subEntities.IsEmpty())
                    ExecuteStep(surrogate, null, null);
                else
                {
                    var subWorkflow = decActivity.Decomposition.Workflow;
                    foreach (var se in subEntities)
                    {
                        var caseActivity = subWorkflow.ConstructFrom(CaseActivityOperation.CreateCaseFromWorkflow, se, ca.Case.ToLite());
                        caseActivity.Previous = ca.ToLite();
                        caseActivity.Execute(CaseActivityOperation.Register);
                    }
                }
            }

            private static bool FindNext(WorkflowConnectionEntity connection, WorkflowExecuteStepContext ctx)
            {
                ctx.Connections.Add(connection);
                return FindNext(connection.To, ctx);
            }

            private static bool FindNext(IWorkflowNodeEntity next, WorkflowExecuteStepContext ctx)
            {
                if (next is WorkflowEventEntity)
                {
                    var ne = ((WorkflowEventEntity)next);

                    if (ne.Type == WorkflowEventType.Finish)
                    {
                        ctx.IsFinished = true;
                        return true;
                    }

                    throw new NotImplementedException($"Unexpected {nameof(WorkflowEventType)} {ne.Type}");
                }
                else if (next is WorkflowActivityEntity)
                {
                    ctx.ToActivities.Add((WorkflowActivityEntity)next);
                    return true; 
                }
                else
                {
                    var gateway = (WorkflowGatewayEntity)next;

                    switch (gateway.Type)
                    {
                        case WorkflowGatewayType.Exclusive:
                            if (gateway.Direction == WorkflowGatewayDirection.Split)
                            {
                                var firstConnection = gateway.NextConnectionsFromCache()
                                    .GroupBy(c => c.Order)
                                    .OrderBy(gr => gr.Key)
                                    .Select(gr => gr.SingleOrDefaultEx(c => c.Applicable(ctx)))
                                    .NotNull()
                                    .FirstEx();
                                return FindNext(firstConnection, ctx);
                            }
                            else //if (gateway.Direction == WorkflowGatewayDirection.Join)
                            {
                                var singleConnection = gateway.NextConnectionsFromCache().SingleEx();
                                return FindNext(singleConnection, ctx);
                            }
         
                        case WorkflowGatewayType.Parallel:
                        case WorkflowGatewayType.Inclusive:
                            if (gateway.Direction == WorkflowGatewayDirection.Split)
                            {
                                var applicable = gateway.NextConnections().ToList().Where(c =>
                                {
                                    var app = c.Applicable(ctx);
                                    if (!app && gateway.Type == WorkflowGatewayType.Parallel)
                                        throw new InvalidOperationException($"Conditions not allowed in {WorkflowGatewayType.Parallel} {WorkflowGatewayDirection.Split}!");
                                    return app;
                                }).ToList();

                                if (applicable.IsEmpty())
                                    throw new InvalidOperationException("No condition applied");

                                foreach (var con in applicable)
                                {
                                    FindNext(con, ctx);
                                }

                                return true; 
                            }
                            else //if (gateway.Direction == WorkflowGatewayDirection.Join)
                            {
                                if (!FindPrevious(0, gateway, ctx))
                                    return false;

                                var singleConnection = gateway.NextConnectionsFromCache().SingleEx();
                                return FindNext(singleConnection, ctx);
                            }
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }
            
            private static bool FindPrevious(int depth, IWorkflowNodeEntity node, WorkflowExecuteStepContext ctx)
            {
                if (node is WorkflowEventEntity)
                {
                    throw new InvalidOperationException($"Unexpected {nameof(WorkflowEventEntity)} in {nameof(FindPrevious)}");
                }
                else if (node is WorkflowActivityEntity)
                {
                    var wa = (WorkflowActivityEntity)node;
                    if (wa.Is(ctx.CaseActivity.WorkflowActivity))
                        return true;

                    var last = ctx.CaseActivity.Case.CaseActivities().Where(a => a.WorkflowActivity == wa).OrderBy(a => a.StartDate).LastOrDefault();
                    if (last != null)
                        return (last.DoneDate.HasValue);

                    var prevsConnections = node.PreviousConnectionsFromCache().Select(a => a.From).ToList();
                    return prevsConnections.All(wn => FindPrevious(depth, wn, ctx));
                }
                else if (node is WorkflowGatewayEntity)
                {
                    var g = (WorkflowGatewayEntity)node;
                    depth += (g.Direction == WorkflowGatewayDirection.Split ? -1 : 1);

                    if (depth == 0)
                        return true;

                    switch (g.Type)
                    {
                        case WorkflowGatewayType.Exclusive:
                            {
                                var prevsExclusive = g.PreviousConnectionsFromCache().Select(a => a.From).ToList();
                                return prevsExclusive.Any(wn => FindPrevious(depth, wn, ctx));
                            }
                        case WorkflowGatewayType.Parallel:
                        case WorkflowGatewayType.Inclusive:
                            {
                                var prevsParallelAndInclusive = g.PreviousConnectionsFromCache().Select(a => a.From).ToList();
                                return prevsParallelAndInclusive.All(wn => FindPrevious(depth, wn, ctx));
                            }
                        default:
                            throw new InvalidOperationException();
                    }
                }
                throw new InvalidOperationException();
            }
        }
    }
}
