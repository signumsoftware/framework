using Signum.Entities.Workflow;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using Signum.Entities.Dynamic;
using System.Text.RegularExpressions;
using Signum.Entities.Reflection;
using Signum.Engine.UserAssets;
using Signum.Entities.Basics;
using Signum.Engine.Translation;
using Microsoft.Graph.SecurityNamespace;

namespace Signum.Engine.Workflow;


public static class WorkflowLogic
{
    public static Action<ICaseMainEntity, WorkflowTransitionContext>? OnTransition;

    public static ResetLazy<Dictionary<Lite<WorkflowEntity>, WorkflowEntity>> Workflows = null!;

    [AutoExpressionField]
    public static bool HasExpired(this WorkflowEntity w) =>
    As.Expression(() => w.ExpirationDate.HasValue && w.ExpirationDate.Value < Clock.Now);

    [AutoExpressionField]
    public static IQueryable<WorkflowPoolEntity> WorkflowPools(this WorkflowEntity e) =>
        As.Expression(() => Database.Query<WorkflowPoolEntity>().Where(a => a.Workflow.Is(e)));

    [AutoExpressionField]
    public static IQueryable<WorkflowActivityEntity> WorkflowActivities(this WorkflowEntity e) =>
        As.Expression(() => Database.Query<WorkflowActivityEntity>().Where(a => a.Lane.Pool.Workflow.Is(e)));

    public static IEnumerable<WorkflowActivityEntity> WorkflowActivitiesFromCache(this WorkflowEntity e)
    {
        return GetWorkflowNodeGraph(e.ToLite()).NextGraph.OfType<WorkflowActivityEntity>();
    }

    [AutoExpressionField]
    public static IQueryable<WorkflowEventEntity> WorkflowEvents(this WorkflowEntity e) =>
        As.Expression(() => Database.Query<WorkflowEventEntity>().Where(a => a.Lane.Pool.Workflow.Is(e)));

    [AutoExpressionField]
    public static WorkflowEventEntity? WorkflowStartEvent(this WorkflowEntity e) =>
        As.Expression(() => e.WorkflowEvents().Where(we => we.Type == WorkflowEventType.Start).SingleOrDefault());

    public static IEnumerable<WorkflowEventEntity> WorkflowEventsFromCache(this WorkflowEntity e)
    {
        return GetWorkflowNodeGraph(e.ToLite()).NextGraph.OfType<WorkflowEventEntity>();
    }

    [AutoExpressionField]
    public static IQueryable<WorkflowGatewayEntity> WorkflowGateways(this WorkflowEntity e) =>
        As.Expression(() => Database.Query<WorkflowGatewayEntity>().Where(a => a.Lane.Pool.Workflow.Is(e)));

    public static IEnumerable<WorkflowGatewayEntity> WorkflowGatewaysFromCache(this WorkflowEntity e)
    {
        return GetWorkflowNodeGraph(e.ToLite()).NextGraph.OfType<WorkflowGatewayEntity>();
    }

    [AutoExpressionField]
    public static IQueryable<WorkflowConnectionEntity> WorkflowConnections(this WorkflowEntity e) =>
        As.Expression(() => Database.Query<WorkflowConnectionEntity>().Where(a => a.From.Lane.Pool.Workflow.Is(e) && a.To.Lane.Pool.Workflow.Is(e)));

    public static IEnumerable<WorkflowConnectionEntity> WorkflowConnectionsFromCache(this WorkflowEntity e)
    {
        return GetWorkflowNodeGraph(e.ToLite()).NextGraph.EdgesWithValue.SelectMany(edge => edge.Value);
    }

    [AutoExpressionField]
    public static IQueryable<WorkflowConnectionEntity> WorkflowMessageConnections(this WorkflowEntity e) =>
        As.Expression(() => e.WorkflowConnections().Where(a => !a.From.Lane.Pool.Is(a.To.Lane.Pool)));

    [AutoExpressionField]
    public static IQueryable<WorkflowLaneEntity> WorkflowLanes(this WorkflowPoolEntity e) =>
        As.Expression(() => Database.Query<WorkflowLaneEntity>().Where(a => a.Pool.Is(e)));

    [AutoExpressionField]
    public static IQueryable<WorkflowConnectionEntity> WorkflowConnections(this WorkflowPoolEntity e) =>
        As.Expression(() => Database.Query<WorkflowConnectionEntity>().Where(a => a.From.Lane.Pool.Is(e) && a.To.Lane.Pool.Is(e)));

    [AutoExpressionField]
    public static IQueryable<WorkflowGatewayEntity> WorkflowGateways(this WorkflowLaneEntity e) =>
        As.Expression(() => Database.Query<WorkflowGatewayEntity>().Where(a => a.Lane.Is(e)));

    [AutoExpressionField]
    public static IQueryable<WorkflowEventEntity> WorkflowEvents(this WorkflowLaneEntity e) =>
        As.Expression(() => Database.Query<WorkflowEventEntity>().Where(a => a.Lane.Is(e)));

    [AutoExpressionField]
    public static IQueryable<WorkflowActivityEntity> WorkflowActivities(this WorkflowLaneEntity e) =>
        As.Expression(() => Database.Query<WorkflowActivityEntity>().Where(a => a.Lane.Is(e)));

    [AutoExpressionField]
    public static IQueryable<WorkflowConnectionEntity> NextConnections(this IWorkflowNodeEntity e) =>
        As.Expression(() => Database.Query<WorkflowConnectionEntity>().Where(a => a.From == e));

    [AutoExpressionField]
    public static WorkflowEntity Workflow(this CaseActivityEntity ca) =>
        As.Expression(() => ca.Case.Workflow);

    public static IEnumerable<WorkflowConnectionEntity> NextConnectionsFromCache(this IWorkflowNodeEntity e, ConnectionType? type)
    {
        var result = GetWorkflowNodeGraph(e.Lane.Pool.Workflow.ToLite()).NextConnections(e);

        if (type == null)
            return result;

        return result.Where(a => a.Type == type);
    }

    [AutoExpressionField]
    public static IQueryable<WorkflowConnectionEntity> PreviousConnections(this IWorkflowNodeEntity e) =>
        As.Expression(() => Database.Query<WorkflowConnectionEntity>().Where(a => a.To == e));

    public static IEnumerable<WorkflowConnectionEntity> PreviousConnectionsFromCache(this IWorkflowNodeEntity e)
    {
        return GetWorkflowNodeGraph(e.Lane.Pool.Workflow.ToLite()).PreviousConnections(e);
    }


    public static ResetLazy<Dictionary<Lite<WorkflowEntity>, WorkflowNodeGraph>> WorkflowGraphLazy = null!;

    public static List<Lite<IWorkflowNodeEntity>> AutocompleteNodes(Lite<WorkflowEntity> workflow, string subString, int count, List<Lite<IWorkflowNodeEntity>> excludes)
    {
        return WorkflowGraphLazy.Value.GetOrThrow(workflow).Autocomplete(subString, count, excludes);
    }



    public static WorkflowNodeGraph GetWorkflowNodeGraph(Lite<WorkflowEntity> workflow)
    {
        var graph = WorkflowGraphLazy.Value.GetOrThrow(workflow);
        if (graph.TrackId != null)
            return graph;

        lock (graph)
        {
            if (graph.TrackId != null)
                return graph;

            var issues = new List<WorkflowIssue>();
            graph.Validate(issues, (g, newDirection) =>
            {
                throw new InvalidOperationException($"Unexpected direction of gateway '{g}' (Should be '{newDirection.NiceToString()}'). Consider saving Workflow '{workflow}'.");
            });

            var errors = issues.Where(a => a.Type == WorkflowIssueType.Error);
            if (errors.HasItems())
                throw new ApplicationException("Errors in Workflow '" + workflow + "':\r\n" + errors.ToString("\r\n").Indent(4));

            return graph;
        }
    }

    static Func<WorkflowConfigurationEmbedded> getConfiguration = null!;
    public static WorkflowConfigurationEmbedded Configuration
    {
        get { return getConfiguration(); }
    }

    static Regex CurrentIsRegex = new Regex($@"{nameof(WorkflowActivityInfo)}\s*\.\s*{nameof(WorkflowActivityInfo.Current)}\s*\.\s*{nameof(WorkflowActivityInfo.Is)}\s*\(\s*""(?<workflowName>[^""]*)""\s*,\s*""(?<activityName>[^""]*)""\s*\)");
    internal static List<CustomCompilerError> GetCustomErrors(string code)
    {
        var matches = CurrentIsRegex.Matches(code).Cast<Match>().ToList();

        return matches.Select(m =>
        {
            var workflowName = m.Groups["workflowName"].Value;
            var wa = WorkflowLogic.WorkflowGraphLazy.Value.Values.SingleOrDefault(w => w.Workflow.Name == workflowName);

            if (wa == null)
                return CreateCompilerError(code, m, $"No workflow with Name '{workflowName}' found.");

            var activityName = m.Groups["activityName"].Value;
            if (!wa.Activities.Values.Any(a => a.Name == activityName))
                return CreateCompilerError(code, m, $"No activity with Name '{activityName}' found in workflow '{workflowName}'.");

            return null;

        }).NotNull().ToList();
    }

    private static CustomCompilerError CreateCompilerError(string code, Match m, string errorText)
    {
        int index = 0;
        int line = 1;
        while (true)
        {
            var newIndex = code.IndexOf('\n', index + 1);
            if (newIndex >= m.Index || newIndex == -1)
                return new CustomCompilerError { ErrorText = errorText, Line = line };

            index = newIndex;
            line++;
        }
    }

    public static void Start(SchemaBuilder sb, Func<WorkflowConfigurationEmbedded> getConfiguration)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            PermissionAuthLogic.RegisterPermissions(WorkflowPermission.ViewWorkflowPanel);
            PermissionAuthLogic.RegisterPermissions(WorkflowPermission.ViewCaseFlow);
            PermissionAuthLogic.RegisterPermissions(WorkflowPermission.WorkflowToolbarMenu);

            WorkflowLogic.getConfiguration = getConfiguration;

            UserAssetsImporter.Register<WorkflowEntity>("Workflow", WorkflowOperation.Save);
            UserAssetsImporter.Register<WorkflowScriptEntity>("WorkflowScript", WorkflowScriptOperation.Save);
            UserAssetsImporter.Register<WorkflowTimerConditionEntity>("WorkflowTimerCondition", WorkflowTimerConditionOperation.Save);
            UserAssetsImporter.Register<WorkflowConditionEntity>("WorkflowCondition", WorkflowConditionOperation.Save);
            UserAssetsImporter.Register<WorkflowActionEntity>("WorkflowAction", WorkflowActionOperation.Save);
            UserAssetsImporter.Register<WorkflowScriptRetryStrategyEntity>("WorkflowScriptRetryStrategy", WorkflowScriptRetryStrategyOperation.Save);

            sb.Include<WorkflowEntity>()
                .WithConstruct(WorkflowOperation.Create)
                .WithQuery(() => DynamicQueryCore.Auto(
                from e in Database.Query<WorkflowEntity>()
                select new
                {
                    Entity = e,
                    e.Id,
                    e.Name,
                    e.MainEntityType,
                    HasExpired = e.HasExpired(),
                    e.ExpirationDate,
                })
                .ColumnDisplayName(a => a.HasExpired, () => WorkflowMessage.HasExpired.NiceToString()))
                .WithExpressionFrom((CaseActivityEntity ca) => ca.Workflow());

            WorkflowGraph.Register();
            QueryLogic.Expressions.Register((WorkflowEntity wf) => wf.WorkflowStartEvent());
            QueryLogic.Expressions.Register((WorkflowEntity wf) => wf.HasExpired(), WorkflowMessage.HasExpired);
            sb.AddIndex((WorkflowEntity wf) => wf.ExpirationDate);

            DynamicCode.GetCustomErrors += GetCustomErrors;

            Workflows = sb.GlobalLazy(() => Database.Query<WorkflowEntity>().ToDictionary(a => a.ToLite()),
                new InvalidateWith(typeof(WorkflowEntity)));


            sb.Include<WorkflowPoolEntity>()
                .WithUniqueIndex(wp => new { wp.Workflow, wp.Name })
                .WithSave(WorkflowPoolOperation.Save)
                .WithDelete(WorkflowPoolOperation.Delete)
                .WithExpressionFrom((WorkflowEntity p) => p.WorkflowPools())
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Name,
                    e.BpmnElementId,
                    e.Workflow,
                });

            sb.Include<WorkflowLaneEntity>()
                .WithUniqueIndex(wp => new { wp.Pool, wp.Name })
                .WithSave(WorkflowLaneOperation.Save)
                .WithDelete(WorkflowLaneOperation.Delete)
                .WithExpressionFrom((WorkflowPoolEntity p) => p.WorkflowLanes())
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Name,
                    e.BpmnElementId,
                    e.Pool,
                    e.Pool.Workflow,
                });

            sb.Include<WorkflowActivityEntity>()
                .WithUniqueIndex(w => new { w.Lane, w.Name })
                .WithSave(WorkflowActivityOperation.Save)
                .WithDelete(WorkflowActivityOperation.Delete)
                .WithExpressionFrom((WorkflowEntity p) => p.WorkflowActivities())
                .WithExpressionFrom((WorkflowLaneEntity p) => p.WorkflowActivities())
                .WithVirtualMList(wa => wa.BoundaryTimers, e => e.BoundaryOf, WorkflowEventOperation.Save, WorkflowEventOperation.Delete)
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Name,
                    e.BpmnElementId,
                    e.Comments,
                    e.Lane,
                    e.Lane.Pool.Workflow,
                });

            sb.Include<WorkflowEventEntity>()
                .WithExpressionFrom((WorkflowEntity p) => p.WorkflowEvents())
                .WithExpressionFrom((WorkflowLaneEntity p) => p.WorkflowEvents())
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Type,
                    e.Name,
                    e.BpmnElementId,
                    e.Lane,
                    e.Lane.Pool.Workflow,
                    e.RunRepeatedly,
                    e.DecisionOptionName,
                });

            AuthLogic.HasRuleOverridesEvent += role => Database.Query<WorkflowLaneEntity>().Any(a => a.Actors.Contains(role));


            new Graph<WorkflowEventEntity>.Execute(WorkflowEventOperation.Save)
            {
                CanBeNew = true,
                CanBeModified = true,
                Execute = (e, _) =>
                {
                    if (e.Timer == null && e.Type.IsTimer())
                        throw new InvalidOperationException(ValidationMessage._0IsMandatoryWhen1IsSetTo2.NiceToString(Entity.NicePropertyName(() => e.Timer), Entity.NicePropertyName(() => e.Type), e.Type.NiceToString()));

                    if (e.Timer != null && !e.Type.IsTimer())
                        throw new InvalidOperationException(ValidationMessage._0ShouldBeNullWhen1IsSetTo2.NiceToString(Entity.NicePropertyName(() => e.Timer), Entity.NicePropertyName(() => e.Type), e.Type.NiceToString()));

                    if (e.BoundaryOf == null && e.Type.IsBoundaryTimer())
                        throw new InvalidOperationException(ValidationMessage._0IsMandatoryWhen1IsSetTo2.NiceToString(Entity.NicePropertyName(() => e.BoundaryOf), Entity.NicePropertyName(() => e.Type), e.Type.NiceToString()));

                    if (e.BoundaryOf != null && !e.Type.IsBoundaryTimer())
                        throw new InvalidOperationException(ValidationMessage._0ShouldBeNullWhen1IsSetTo2.NiceToString(Entity.NicePropertyName(() => e.BoundaryOf), Entity.NicePropertyName(() => e.Type), e.Type.NiceToString()));

                    e.Save();
                },
            }.Register();

            new Graph<WorkflowEventEntity>.Delete(WorkflowEventOperation.Delete)
            {
                Delete = (e, _) =>
                {

                    if (e.Type.IsScheduledStart())
                    {
                        var scheduled = e.ScheduledTask();
                        if (scheduled != null)
                            WorkflowEventTaskLogic.DeleteWorkflowEventScheduledTask(scheduled);
                    }

                    e.Delete();
                },
            }.Register();

            sb.Include<WorkflowGatewayEntity>()
                .WithSave(WorkflowGatewayOperation.Save)
                .WithDelete(WorkflowGatewayOperation.Delete)
                .WithExpressionFrom((WorkflowEntity p) => p.WorkflowGateways())
                .WithExpressionFrom((WorkflowLaneEntity p) => p.WorkflowGateways())
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Type,
                    e.Name,
                    e.BpmnElementId,
                    e.Lane,
                    e.Lane.Pool.Workflow,
                });

            sb.Include<WorkflowConnectionEntity>()
                .WithSave(WorkflowConnectionOperation.Save)
                .WithDelete(WorkflowConnectionOperation.Delete)
                .WithExpressionFrom((WorkflowEntity p) => p.WorkflowConnections())
                .WithExpressionFrom((WorkflowEntity p) => p.WorkflowMessageConnections(), null!)
                .WithExpressionFrom((WorkflowPoolEntity p) => p.WorkflowConnections())
                .WithExpressionFrom((IWorkflowNodeEntity p) => p.NextConnections(), null!)
                .WithExpressionFrom((IWorkflowNodeEntity p) => p.PreviousConnections(), null!)
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Name,
                    e.BpmnElementId,
                    e.From,
                    e.To,
                });

            WorkflowEventTaskEntity.GetWorkflowEntity = lite => WorkflowGraphLazy.Value.GetOrThrow(lite).Workflow;

            WorkflowGraphLazy = sb.GlobalLazy(() =>
            {
                using (new EntityCache())
                {
                    var events = Database.RetrieveAll<WorkflowEventEntity>().GroupToDictionary(a => a.Lane.Pool.Workflow.ToLite());
                    var gateways = Database.RetrieveAll<WorkflowGatewayEntity>().GroupToDictionary(a => a.Lane.Pool.Workflow.ToLite());
                    var activities = Database.RetrieveAll<WorkflowActivityEntity>().GroupToDictionary(a => a.Lane.Pool.Workflow.ToLite());
                    var connections = Database.RetrieveAll<WorkflowConnectionEntity>().GroupToDictionary(a => a.From.Lane.Pool.Workflow.ToLite());

                    var result = Database.RetrieveAll<WorkflowEntity>().ToDictionary(workflow => workflow.ToLite(), workflow =>
                    {
                        var w = workflow.ToLite();
                        var nodeGraph = new WorkflowNodeGraph
                        {
                            Workflow = workflow,
                            Events = events.TryGetC(w).EmptyIfNull().ToDictionary(e => e.ToLite()),
                            Gateways = gateways.TryGetC(w).EmptyIfNull().ToDictionary(g => g.ToLite()),
                            Activities = activities.TryGetC(w).EmptyIfNull().ToDictionary(a => a.ToLite()),
                            Connections = connections.TryGetC(w).EmptyIfNull().ToDictionary(c => c.ToLite()),
                        };

                        nodeGraph.FillGraphs();
                        return nodeGraph;
                    });

                    return result;
                }
            }, new InvalidateWith(typeof(WorkflowConnectionEntity)));
            WorkflowGraphLazy.OnReset += (e, args) => DynamicCode.OnInvalidated?.Invoke();

            Validator.PropertyValidator((WorkflowConnectionEntity c) => c.Condition).StaticPropertyValidation = (e, pi) =>
            {
                if (e.Condition != null && e.From != null)
                {
                    var conditionType = (e.Condition.EntityOrNull ?? Conditions.Value.GetOrThrow(e.Condition)).MainEntityType;
                    var workflowType = e.From.Lane.Pool.Workflow.MainEntityType;

                    if (!conditionType.Is(workflowType))
                        return WorkflowMessage.Condition0IsDefinedFor1Not2.NiceToString(conditionType, workflowType);
                }

                return null;
            };

            StartWorkflowConditions(sb);

            StartWorkflowTimerConditions(sb);

            StartWorkflowActions(sb);

            StartWorkflowScript(sb);
        }
    }

    public static void RegisterTranslatableRoutes()
    {
        TranslatedInstanceLogic.AddRoute((WorkflowEntity tb) => tb.Name);
        TranslatedInstanceLogic.AddRoute((WorkflowActivityEntity tb) => tb.Name);
        TranslatedInstanceLogic.AddRoute((WorkflowActivityEntity tb) => tb.UserHelp, Entities.Translation.TranslateableRouteType.Html);
    }


    public static ResetLazy<Dictionary<Lite<WorkflowTimerConditionEntity>, WorkflowTimerConditionEntity>> TimerConditions = null!;
    public static bool Evaluate(this Lite<WorkflowTimerConditionEntity> wc, CaseActivityEntity ca, DateTime now)
    {
        var tc = TimerConditions.Value.GetOrThrow(wc);
        using (HeavyProfiler.Log("WorkflowTimerCondition", ()=> tc.Name))
        {
            return tc.Eval.Algorithm.EvaluateUntyped(ca, now);
        }
    }

    private static void StartWorkflowTimerConditions(SchemaBuilder sb)
    {
        sb.Include<WorkflowTimerConditionEntity>()
           .WithQuery(() => e => new
           {
               Entity = e,
               e.Id,
               e.Name,
               e.MainEntityType,
               e.Eval.Script
           });

        new Graph<WorkflowTimerConditionEntity>.Execute(WorkflowTimerConditionOperation.Save)
        {
            CanBeNew = true,
            CanBeModified = true,
            Execute = (e, _) =>
            {
                if (!e.IsNew)
                {

                    var oldMainEntityType = e.InDB(a => a.MainEntityType);
                    if (!oldMainEntityType.Is(e.MainEntityType))
                        ThrowConnectionError(Database.Query<WorkflowEventEntity>().Where(a => a.Timer!.Condition.Is(e.ToLite())), e, WorkflowTimerConditionOperation.Save);
                }

                e.Save();
            },
        }.Register();

        new Graph<WorkflowTimerConditionEntity>.Delete(WorkflowTimerConditionOperation.Delete)
        {
            Delete = (e, _) =>
            {
                ThrowConnectionError(Database.Query<WorkflowEventEntity>().Where(a => a.Timer!.Condition.Is(e.ToLite())), e, WorkflowTimerConditionOperation.Delete);
                e.Delete();
            },
        }.Register();

        new Graph<WorkflowTimerConditionEntity>.ConstructFrom<WorkflowTimerConditionEntity>(WorkflowTimerConditionOperation.Clone)
        {
            Construct = (e, args) =>
            {
                return new WorkflowTimerConditionEntity
                {
                    MainEntityType = e.MainEntityType,
                    Eval = new WorkflowTimerConditionEval { Script = e.Eval.Script }
                };
            },
        }.Register();

        TimerConditions = sb.GlobalLazy(() => Database.Query<WorkflowTimerConditionEntity>().ToDictionary(a => a.ToLite()),
             new InvalidateWith(typeof(WorkflowTimerConditionEntity)));
    }

    public static ResetLazy<Dictionary<Lite<WorkflowActionEntity>, WorkflowActionEntity>> Actions = null!;
    public static void Execute(this Lite<WorkflowActionEntity> wa, ICaseMainEntity mainEntity, WorkflowTransitionContext ctx)
    {
        var waEntity = Actions.Value.GetOrThrow(wa);

        using(HeavyProfiler.Log("WorkflowAction", ()=> waEntity.Name))
        {
            waEntity.Eval.Algorithm.ExecuteUntyped(mainEntity, ctx);
        }
    }

    private static void StartWorkflowActions(SchemaBuilder sb)
    {
        sb.Include<WorkflowActionEntity>()
           .WithQuery(() => e => new
           {
               Entity = e,
               e.Id,
               e.Name,
               e.MainEntityType,
               e.Eval.Script
           });

        new Graph<WorkflowActionEntity>.Execute(WorkflowActionOperation.Save)
        {
            CanBeNew = true,
            CanBeModified = true,
            Execute = (e, _) =>
            {
                if (!e.IsNew)
                {

                    var oldMainEntityType = e.InDB(a => a.MainEntityType);
                    if (!oldMainEntityType.Is(e.MainEntityType))
                        ThrowConnectionError(Database.Query<WorkflowConnectionEntity>().Where(a => a.Action.Is(e.ToLite())), e, WorkflowActionOperation.Save);
                }

                e.Save();
            },
        }.Register();

        new Graph<WorkflowActionEntity>.Delete(WorkflowActionOperation.Delete)
        {
            Delete = (e, _) =>
            {
                ThrowConnectionError(Database.Query<WorkflowConnectionEntity>().Where(a => a.Action.Is(e.ToLite())), e, WorkflowActionOperation.Delete);
                e.Delete();
            },
        }.Register();

        new Graph<WorkflowActionEntity>.ConstructFrom<WorkflowActionEntity>(WorkflowActionOperation.Clone)
        {
            Construct = (e, args) =>
            {
                return new WorkflowActionEntity
                {
                    MainEntityType = e.MainEntityType,
                    Eval = new WorkflowActionEval { Script = e.Eval.Script }
                };
            },
        }.Register();

        Actions = sb.GlobalLazy(() => Database.Query<WorkflowActionEntity>().ToDictionary(a => a.ToLite()),
            new InvalidateWith(typeof(WorkflowActionEntity)));
    }

    public static ResetLazy<Dictionary<Lite<WorkflowConditionEntity>, WorkflowConditionEntity>> Conditions = null!;
    public static bool Evaluate(this Lite<WorkflowConditionEntity> wc, ICaseMainEntity mainEntity, WorkflowTransitionContext ctx)
    {
        var wcEntity = Conditions.Value.GetOrThrow(wc);

        using (HeavyProfiler.Log("WorkflowCondition", () => wcEntity.Name))
        {
            return wcEntity.Eval.Algorithm.EvaluateUntyped(mainEntity, ctx);
        }
    }

    private static void StartWorkflowConditions(SchemaBuilder sb)
    {
        sb.Include<WorkflowConditionEntity>()
           .WithQuery(() => e => new
           {
               Entity = e,
               e.Id,
               e.Name,
               e.MainEntityType,
               e.Eval.Script
           });

        new Graph<WorkflowConditionEntity>.Execute(WorkflowConditionOperation.Save)
        {
            CanBeNew = true,
            CanBeModified = true,
            Execute = (e, _) =>
            {
                if (!e.IsNew)
                {

                    var oldMainEntityType = e.InDB(a => a.MainEntityType);
                    if (!oldMainEntityType.Is(e.MainEntityType))
                        ThrowConnectionError(Database.Query<WorkflowConnectionEntity>().Where(a => a.Condition.Is(e.ToLite())), e, WorkflowConditionOperation.Save);
                }

                e.Save();
            },
        }.Register();

        new Graph<WorkflowConditionEntity>.Delete(WorkflowConditionOperation.Delete)
        {
            Delete = (e, _) =>
            {
                ThrowConnectionError(Database.Query<WorkflowConnectionEntity>().Where(a => a.Condition.Is(e.ToLite())), e, WorkflowConditionOperation.Delete);
                e.Delete();
            },
        }.Register();

        new Graph<WorkflowConditionEntity>.ConstructFrom<WorkflowConditionEntity>(WorkflowConditionOperation.Clone)
        {
            Construct = (e, args) =>
            {
                return new WorkflowConditionEntity
                {
                    MainEntityType = e.MainEntityType,
                    Eval = new WorkflowConditionEval { Script = e.Eval.Script }
                };
            },
        }.Register();


        Conditions = sb.GlobalLazy(() => Database.Query<WorkflowConditionEntity>().ToDictionary(a => a.ToLite()),
            new InvalidateWith(typeof(WorkflowConditionEntity)));
    }

    public static ResetLazy<Dictionary<Lite<WorkflowScriptEntity>, WorkflowScriptEntity>> Scripts = null!;
    public static WorkflowScriptEntity RetrieveFromCache(this Lite<WorkflowScriptEntity> ws) => Scripts.Value.GetOrThrow(ws);
    private static void StartWorkflowScript(SchemaBuilder sb)
    {
        sb.Include<WorkflowScriptEntity>()
          .WithQuery(() => s => new
          {
              Entity = s,
              s.Id,
              s.Name,
              s.MainEntityType,
          });

        new Graph<WorkflowScriptEntity>.Execute(WorkflowScriptOperation.Save)
        {
            CanBeNew = true,
            CanBeModified = true,
            Execute = (e, _) =>
            {
                if (!e.IsNew)
                {

                    var oldMainEntityType = e.InDB(a => a.MainEntityType);
                    if (!oldMainEntityType.Is(e.MainEntityType))
                        ThrowConnectionError(Database.Query<WorkflowActivityEntity>().Where(a => a.Script!.Script.Is(e.ToLite())), e, WorkflowScriptOperation.Save);
                }

                e.Save();
            },
        }.Register();

        new Graph<WorkflowScriptEntity>.ConstructFrom<WorkflowScriptEntity>(WorkflowScriptOperation.Clone)
        {
            Construct = (s, _) => new WorkflowScriptEntity()
            {
                MainEntityType = s.MainEntityType,
                Eval = new WorkflowScriptEval() { Script = s.Eval.Script }
            }
        }.Register();

        new Graph<WorkflowScriptEntity>.Delete(WorkflowScriptOperation.Delete)
        {
            Delete = (s, _) =>
            {
                ThrowConnectionError(Database.Query<WorkflowActivityEntity>().Where(a => a.Script!.Script.Is(s.ToLite())), s, WorkflowScriptOperation.Delete);
                s.Delete();
            },
        }.Register();

        Scripts = sb.GlobalLazy(() => Database.Query<WorkflowScriptEntity>().ToDictionary(a => a.ToLite()),
            new InvalidateWith(typeof(WorkflowScriptEntity)));

        sb.Include<WorkflowScriptRetryStrategyEntity>()
            .WithSave(WorkflowScriptRetryStrategyOperation.Save)
            .WithDelete(WorkflowScriptRetryStrategyOperation.Delete)
            .WithQuery(() => e => new
            {
                Entity = e,
                e.Id,
                e.Rule
            });
    }

    private static void ThrowConnectionError(IQueryable<WorkflowConnectionEntity> queryable, Entity entity, IOperationSymbolContainer operation)
    {
        if (queryable.Count() == 0)
            return;

        var errors = queryable.Select(a => new { Connection = a.ToLite(), From = a.From.ToLite(), To = a.To.ToLite(), Workflow = a.From.Lane.Pool.Workflow.ToLite() }).ToList();

        var formattedErrors = errors.GroupBy(a => a.Workflow).ToString(gr => $"Workflow '{gr.Key}':" +
              gr.ToString(a => $"Connection {a.Connection!.Id} ({a.Connection}): {a.From} -> {a.To}", "\r\n").Indent(4),
            "\r\n\r\n").Indent(4);

        throw new ApplicationException($"Impossible to {operation.Symbol.Key.After('.')} '{entity}' because is used in some connections: \r\n" + formattedErrors);
    }

    private static void ThrowConnectionError<T>(IQueryable<T> queryable, Entity entity, IOperationSymbolContainer operation)
        where T : Entity, IWorkflowNodeEntity
    {
        if (queryable.Count() == 0)
            return;

        var errors = queryable.Select(a => new { Entity = a.ToLite(), Workflow = a.Lane.Pool.Workflow.ToLite() }).ToList();

        var formattedErrors = errors.GroupBy(a => a.Workflow).ToString(gr => $"Workflow '{gr.Key}':" +
              gr.ToString(a => $"{typeof(T).NiceName()} {a.Entity}", "\r\n").Indent(4),
            "\r\n\r\n").Indent(4);

        throw new ApplicationException($"Impossible to {operation.Symbol.Key.After('.')} '{entity}' because is used in some {typeof(T).NicePluralName()}: \r\n" + formattedErrors);
    }

    public class WorkflowGraph : Graph<WorkflowEntity>
    {
        public static void Register()
        {
            new Execute(WorkflowOperation.Save)
            {
                CanBeNew = true,
                CanBeModified = true,
                Execute = (e, args) =>
                {
                    if (e.MainEntityStrategies.Contains(WorkflowMainEntityStrategy.CreateNew))
                    {
                        var type = e.MainEntityType.ToType();
                        if (CaseActivityLogic.Options.TryGetC(type)?.Constructor == null)
                            throw new ApplicationException(WorkflowMessage._0NotAllowedFor1NoConstructorHasBeenDefinedInWithWorkflow.NiceToString(WorkflowMainEntityStrategy.CreateNew.NiceToString(), type.NiceName()));
                    }

                    WorkflowLogic.ApplyDocument(e, args.TryGetArgC<WorkflowModel>(), args.TryGetArgC<WorkflowReplacementModel>(), args.TryGetArgC<List<WorkflowIssue>>() ?? new List<WorkflowIssue>());
                    DynamicCode.OnInvalidated?.Invoke();
                }
            }.Register();

            new ConstructFrom<WorkflowEntity>(WorkflowOperation.Clone)
            {
                Construct = (w, args) =>
                {
                    WorkflowBuilder wb = new WorkflowBuilder(w);

                    var result = wb.Clone();

                    return result;
                }
            }.Register();

            new Delete(WorkflowOperation.Delete)
            {
                CanDelete = w =>
                {
                    var usedWorkflows = Database.Query<CaseEntity>()
                                            .Where(c => c.Workflow.Is(w) && c.ParentCase != null)
                                            .Select(c => c.ParentCase!.Entity.Workflow.ToLite())
                                            .Distinct()
                                            .ToList();

                    if (usedWorkflows.Any())
                        return WorkflowMessage.WorkflowUsedIn0ForDecompositionOrCallWorkflow.NiceToString(usedWorkflows.ToString(", "));

                    return null;
                },

                Delete = (w, _) =>
                {
                    var wb = new WorkflowBuilder(w);
                    wb.Delete();
                    DynamicCode.OnInvalidated?.Invoke();
                }
            }.Register();

            new Execute(WorkflowOperation.Activate)
            {
                CanExecute = w => w.HasExpired() ? null : WorkflowMessage.Workflow0AlreadyActivated.NiceToString(w),
                Execute = (w, _) =>
                {
                    w.ExpirationDate = null;
                    w.Save();
                    w.SuspendWorkflowScheduledTasks(suspended: false);
                }
            }.Register();

            new Execute(WorkflowOperation.Deactivate)
            {
                CanExecute = w => w.HasExpired() ? WorkflowMessage.Workflow0HasExpiredOn1.NiceToString(w, w.ExpirationDate!.Value.ToString()) :
                    w.Cases().SelectMany(c => c.CaseActivities()).Any(ca => ca.DoneDate == null) ? CaseActivityMessage.ThereAreInprogressActivities.NiceToString() : null,
                Execute = (w, args) =>
                {
                    w.ExpirationDate = args.GetArg<DateTime>();
                    w.Save();
                    w.SuspendWorkflowScheduledTasks(suspended: true);
                }
            }.Register();
        }
    }

    public static void SuspendWorkflowScheduledTasks(this WorkflowEntity workflow, bool suspended)
    {
        workflow.WorkflowEvents()
            .Where(a => a.Type == WorkflowEventType.ScheduledStart)
            .Select(a => a.ScheduledTask()!)
            .UnsafeUpdate()
            .Set(a => a.Suspended, a => suspended)
            .Execute();
    }

    public static Func<Lite<Entity>, bool> IsCurrentUserActor = (actor) =>
        actor.Is(UserEntity.Current) ||
        (actor is Lite<RoleEntity> && AuthLogic.IndirectlyRelated(RoleEntity.Current).Contains((Lite<RoleEntity>)actor));

    public static Expression<Func<UserEntity, Lite<Entity>, bool>> IsUserActorForNotifications = (user, actorConstant) =>
        actorConstant.Is(user) ||
       (actorConstant is Lite<RoleEntity> && AuthLogic.InverseIndirectlyRelated((Lite<RoleEntity>)actorConstant).Contains(user.Role));

    public static List<WorkflowEntity> GetAllowedStarts()
    {
        return WorkflowGraphLazy.Value.Values.Where(wg => wg.IsStartCurrentUser()).Select(wg => wg.Workflow).ToList();
    }

    public static WorkflowModel GetWorkflowModel(WorkflowEntity workflow)
    {
        var wb = new WorkflowBuilder(workflow);
        return wb.GetWorkflowModel();
    }

    public static WorkflowReplacementModel PreviewChanges(WorkflowEntity workflow, WorkflowModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        var document = WorkflowBuilder.ParseDocument(model.DiagramXml);
        var wb = new WorkflowBuilder(workflow);
        return wb.PreviewChanges(document, model);
    }

    public static void ApplyDocument(WorkflowEntity workflow, WorkflowModel? model, WorkflowReplacementModel? replacements, List<WorkflowIssue> issuesContainer)
    {
        if (issuesContainer.Any())
            throw new InvalidOperationException("issuesContainer should be empty");

        var wb = new WorkflowBuilder(workflow);
        if (workflow.IsNew)
            workflow.Save();

        if (model != null)
        {
            wb.ApplyChanges(model, replacements);
        }
        wb.ValidateGraph(issuesContainer);

        if (issuesContainer.Any(a => a.Type == WorkflowIssueType.Error))
            throw new IntegrityCheckException(new Dictionary<Guid, IntegrityCheck>());

        workflow.FullDiagramXml = new WorkflowXmlEmbedded { DiagramXml = wb.GetXDocument().ToString() };
        workflow.Save();
    }
}
