//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Entities from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'
import * as Dynamic from '../Dynamic/Signum.Entities.Dynamic'
import * as Signum from '../Basics/Signum.Entities.Basics'
import * as Scheduler from '../Scheduler/Signum.Entities.Scheduler'
import * as Processes from '../Processes/Signum.Entities.Processes'

interface IWorkflowTimerConditionEvaluator {}
interface IWorkflowConditionEvaluator {}
interface IWorkflowActionExecutor {}
interface IWorkflowLaneActorsEvaluator {}
interface ISubEntitiesEvaluator{}
interface IWorkflowScriptExecutor{}
interface IWorkflowEventTaskConditionEvaluator{}
interface IWorkflowEventTaskActionEval{}

export interface WorkflowEntitiesDictionary {
    [bpmnElementId: string]: Entities.ModelEntity
}

export const ActivityWithRemarks = new Type<ActivityWithRemarks>("ActivityWithRemarks");
export interface ActivityWithRemarks extends Entities.ModelEntity {
  Type: "ActivityWithRemarks";
  workflowActivity?: Entities.Lite<WorkflowActivityEntity> | null;
  case?: Entities.Lite<CaseEntity> | null;
  caseActivity?: Entities.Lite<CaseActivityEntity> | null;
  notification?: Entities.Lite<CaseNotificationEntity> | null;
  remarks?: string | null;
  alerts?: number;
  tags: Array<CaseTagTypeEntity>;
}

export const BpmnEntityPairEmbedded = new Type<BpmnEntityPairEmbedded>("BpmnEntityPairEmbedded");
export interface BpmnEntityPairEmbedded extends Entities.EmbeddedEntity {
  Type: "BpmnEntityPairEmbedded";
  model: Entities.ModelEntity;
  bpmnElementId: string;
}

export const CaseActivityEntity = new Type<CaseActivityEntity>("CaseActivity");
export interface CaseActivityEntity extends Entities.Entity {
  Type: "CaseActivity";
  case: CaseEntity;
  workflowActivity: IWorkflowNodeEntity | null;
  originalWorkflowActivityName: string;
  startDate: string;
  previous: Entities.Lite<CaseActivityEntity> | null;
  note: string | null;
  doneDate: string | null;
  duration: number | null;
  doneBy: Entities.Lite<Authorization.UserEntity> | null;
  doneType: DoneType | null;
  scriptExecution: ScriptExecutionEmbedded | null;
}

export const CaseActivityExecutedTimerEntity = new Type<CaseActivityExecutedTimerEntity>("CaseActivityExecutedTimer");
export interface CaseActivityExecutedTimerEntity extends Entities.Entity {
  Type: "CaseActivityExecutedTimer";
  creationDate?: string;
  caseActivity?: Entities.Lite<CaseActivityEntity> | null;
  boundaryEvent?: Entities.Lite<WorkflowEventEntity> | null;
}

export module CaseActivityMessage {
  export const CaseContainsOtherActivities = new MessageKey("CaseActivityMessage", "CaseContainsOtherActivities");
  export const NoNextConnectionThatSatisfiesTheConditionsFound = new MessageKey("CaseActivityMessage", "NoNextConnectionThatSatisfiesTheConditionsFound");
  export const CaseIsADecompositionOf0 = new MessageKey("CaseActivityMessage", "CaseIsADecompositionOf0");
  export const From0On1 = new MessageKey("CaseActivityMessage", "From0On1");
  export const DoneBy0On1 = new MessageKey("CaseActivityMessage", "DoneBy0On1");
  export const PersonalRemarksForThisNotification = new MessageKey("CaseActivityMessage", "PersonalRemarksForThisNotification");
  export const TheActivity0RequiresToBeOpened = new MessageKey("CaseActivityMessage", "TheActivity0RequiresToBeOpened");
  export const NoOpenedOrInProgressNotificationsFound = new MessageKey("CaseActivityMessage", "NoOpenedOrInProgressNotificationsFound");
  export const NextActivityAlreadyInProgress = new MessageKey("CaseActivityMessage", "NextActivityAlreadyInProgress");
  export const NextActivityOfDecompositionSurrogateAlreadyInProgress = new MessageKey("CaseActivityMessage", "NextActivityOfDecompositionSurrogateAlreadyInProgress");
  export const Only0CanUndoThisOperation = new MessageKey("CaseActivityMessage", "Only0CanUndoThisOperation");
  export const Activity0HasNoJumps = new MessageKey("CaseActivityMessage", "Activity0HasNoJumps");
  export const Activity0HasNoTimers = new MessageKey("CaseActivityMessage", "Activity0HasNoTimers");
  export const ThereIsNoPreviousActivity = new MessageKey("CaseActivityMessage", "ThereIsNoPreviousActivity");
  export const OnlyForScriptWorkflowActivities = new MessageKey("CaseActivityMessage", "OnlyForScriptWorkflowActivities");
  export const Pending = new MessageKey("CaseActivityMessage", "Pending");
  export const NoWorkflowActivity = new MessageKey("CaseActivityMessage", "NoWorkflowActivity");
  export const ImpossibleToDeleteCaseActivity0OnWorkflowActivity1BecauseHasNoPreviousActivity = new MessageKey("CaseActivityMessage", "ImpossibleToDeleteCaseActivity0OnWorkflowActivity1BecauseHasNoPreviousActivity");
  export const LastCaseActivity = new MessageKey("CaseActivityMessage", "LastCaseActivity");
  export const CurrentUserHasNotification = new MessageKey("CaseActivityMessage", "CurrentUserHasNotification");
  export const NoNewOrOpenedOrInProgressNotificationsFound = new MessageKey("CaseActivityMessage", "NoNewOrOpenedOrInProgressNotificationsFound");
}

export module CaseActivityOperation {
  export const CreateCaseActivityFromWorkflow : Entities.ConstructSymbol_From<CaseActivityEntity, WorkflowEntity> = registerSymbol("Operation", "CaseActivityOperation.CreateCaseActivityFromWorkflow");
  export const CreateCaseFromWorkflowEventTask : Entities.ConstructSymbol_From<CaseEntity, WorkflowEventTaskEntity> = registerSymbol("Operation", "CaseActivityOperation.CreateCaseFromWorkflowEventTask");
  export const Register : Entities.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Register");
  export const Delete : Entities.DeleteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Delete");
  export const Next : Entities.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Next");
  export const Approve : Entities.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Approve");
  export const Decline : Entities.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Decline");
  export const Jump : Entities.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Jump");
  export const Timer : Entities.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Timer");
  export const MarkAsUnread : Entities.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.MarkAsUnread");
  export const Undo : Entities.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Undo");
  export const ScriptExecute : Entities.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.ScriptExecute");
  export const ScriptScheduleRetry : Entities.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.ScriptScheduleRetry");
  export const ScriptFailureJump : Entities.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.ScriptFailureJump");
  export const FixCaseDescriptions : Entities.ExecuteSymbol<Dynamic.DynamicTypeEntity> = registerSymbol("Operation", "CaseActivityOperation.FixCaseDescriptions");
}

export module CaseActivityProcessAlgorithm {
  export const Timeout : Processes.ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "CaseActivityProcessAlgorithm.Timeout");
}

export module CaseActivityQuery {
  export const Inbox = new QueryKey("CaseActivityQuery", "Inbox");
}

export module CaseActivityTask {
  export const Timeout : Scheduler.SimpleTaskSymbol = registerSymbol("SimpleTask", "CaseActivityTask.Timeout");
}

export const CaseEntity = new Type<CaseEntity>("Case");
export interface CaseEntity extends Entities.Entity {
  Type: "Case";
  workflow: WorkflowEntity;
  parentCase: CaseEntity | null;
  description: string;
  mainEntity: ICaseMainEntity;
  startDate: string;
  finishDate: string | null;
}

export const CaseFlowColor = new EnumType<CaseFlowColor>("CaseFlowColor");
export type CaseFlowColor =
  "CaseMaxDuration" |
  "AverageDuration" |
  "EstimatedDuration";

export const CaseJunctionEntity = new Type<CaseJunctionEntity>("CaseJunction");
export interface CaseJunctionEntity extends Entities.Entity {
  Type: "CaseJunction";
  direction?: WorkflowGatewayDirection;
  from?: Entities.Lite<CaseActivityEntity> | null;
  to?: Entities.Lite<CaseActivityEntity> | null;
}

export const CaseNotificationEntity = new Type<CaseNotificationEntity>("CaseNotification");
export interface CaseNotificationEntity extends Entities.Entity {
  Type: "CaseNotification";
  caseActivity?: Entities.Lite<CaseActivityEntity> | null;
  user?: Entities.Lite<Authorization.UserEntity> | null;
  actor?: Entities.Lite<Entities.Entity> | null;
  remarks?: string | null;
  state?: CaseNotificationState;
}

export module CaseNotificationOperation {
  export const SetRemarks : Entities.ExecuteSymbol<CaseNotificationEntity> = registerSymbol("Operation", "CaseNotificationOperation.SetRemarks");
}

export const CaseNotificationState = new EnumType<CaseNotificationState>("CaseNotificationState");
export type CaseNotificationState =
  "New" |
  "Opened" |
  "InProgress" |
  "Done" |
  "DoneByOther";

export module CaseOperation {
  export const SetTags : Entities.ExecuteSymbol<CaseEntity> = registerSymbol("Operation", "CaseOperation.SetTags");
}

export const CaseTagEntity = new Type<CaseTagEntity>("CaseTag");
export interface CaseTagEntity extends Entities.Entity {
  Type: "CaseTag";
  creationDate?: string;
  case?: Entities.Lite<CaseEntity> | null;
  tagType?: CaseTagTypeEntity | null;
  createdBy?: Entities.Lite<Basics.IUserEntity> | null;
}

export const CaseTagsModel = new Type<CaseTagsModel>("CaseTagsModel");
export interface CaseTagsModel extends Entities.ModelEntity {
  Type: "CaseTagsModel";
  caseTags: Entities.MList<CaseTagTypeEntity>;
  oldCaseTags: Entities.MList<CaseTagTypeEntity>;
}

export const CaseTagTypeEntity = new Type<CaseTagTypeEntity>("CaseTagType");
export interface CaseTagTypeEntity extends Entities.Entity {
  Type: "CaseTagType";
  name?: string | null;
  color?: string | null;
}

export module CaseTagTypeOperation {
  export const Save : Entities.ExecuteSymbol<CaseTagTypeEntity> = registerSymbol("Operation", "CaseTagTypeOperation.Save");
}

export const ConnectionType = new EnumType<ConnectionType>("ConnectionType");
export type ConnectionType =
  "Normal" |
  "Approve" |
  "Decline" |
  "Jump" |
  "ScriptException";

export const DateFilterRange = new EnumType<DateFilterRange>("DateFilterRange");
export type DateFilterRange =
  "All" |
  "LastWeek" |
  "LastMonth" |
  "CurrentYear";

export const DoneType = new EnumType<DoneType>("DoneType");
export type DoneType =
  "Next" |
  "Approve" |
  "Decline" |
  "Jump" |
  "Timeout" |
  "ScriptSuccess" |
  "ScriptFailure" |
  "Recompose";

export interface ICaseMainEntity extends Entities.Entity {
}

export const InboxFilterModel = new Type<InboxFilterModel>("InboxFilterModel");
export interface InboxFilterModel extends Entities.ModelEntity {
  Type: "InboxFilterModel";
  range?: DateFilterRange;
  states: Entities.MList<CaseNotificationState>;
  fromDate?: string | null;
  toDate?: string | null;
}

export module InboxMessage {
  export const Clear = new MessageKey("InboxMessage", "Clear");
  export const Activity = new MessageKey("InboxMessage", "Activity");
  export const SenderNote = new MessageKey("InboxMessage", "SenderNote");
  export const Sender = new MessageKey("InboxMessage", "Sender");
  export const Filters = new MessageKey("InboxMessage", "Filters");
}

export interface IWorkflowNodeEntity extends IWorkflowObjectEntity, Entities.Entity {
  lane?: WorkflowLaneEntity | null;
}

export interface IWorkflowObjectEntity extends Entities.Entity {
  xml?: WorkflowXmlEmbedded | null;
  name?: string | null;
  bpmnElementId?: string | null;
}

export const ScriptExecutionEmbedded = new Type<ScriptExecutionEmbedded>("ScriptExecutionEmbedded");
export interface ScriptExecutionEmbedded extends Entities.EmbeddedEntity {
  Type: "ScriptExecutionEmbedded";
  nextExecution?: string;
  retryCount?: number;
  processIdentifier?: string | null;
}

export const SubEntitiesEval = new Type<SubEntitiesEval>("SubEntitiesEval");
export interface SubEntitiesEval extends Dynamic.EvalEmbedded<ISubEntitiesEvaluator> {
  Type: "SubEntitiesEval";
}

export const SubWorkflowEmbedded = new Type<SubWorkflowEmbedded>("SubWorkflowEmbedded");
export interface SubWorkflowEmbedded extends Entities.EmbeddedEntity {
  Type: "SubWorkflowEmbedded";
  workflow?: WorkflowEntity | null;
  subEntitiesEval?: SubEntitiesEval | null;
}

export const TriggeredOn = new EnumType<TriggeredOn>("TriggeredOn");
export type TriggeredOn =
  "Always" |
  "ConditionIsTrue" |
  "ConditionChangesToTrue";

export const WorkflowActionEntity = new Type<WorkflowActionEntity>("WorkflowAction");
export interface WorkflowActionEntity extends Entities.Entity {
  Type: "WorkflowAction";
  name?: string | null;
  mainEntityType?: Basics.TypeEntity | null;
  eval?: WorkflowActionEval | null;
}

export const WorkflowActionEval = new Type<WorkflowActionEval>("WorkflowActionEval");
export interface WorkflowActionEval extends Dynamic.EvalEmbedded<IWorkflowActionExecutor> {
  Type: "WorkflowActionEval";
}

export module WorkflowActionOperation {
  export const Clone : Entities.ConstructSymbol_From<WorkflowActionEntity, WorkflowActionEntity> = registerSymbol("Operation", "WorkflowActionOperation.Clone");
  export const Save : Entities.ExecuteSymbol<WorkflowActionEntity> = registerSymbol("Operation", "WorkflowActionOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowActionEntity> = registerSymbol("Operation", "WorkflowActionOperation.Delete");
}

export const WorkflowActivityEntity = new Type<WorkflowActivityEntity>("WorkflowActivity");
export interface WorkflowActivityEntity extends Entities.Entity, IWorkflowNodeEntity, IWorkflowObjectEntity {
  Type: "WorkflowActivity";
  lane?: WorkflowLaneEntity | null;
  name?: string | null;
  bpmnElementId?: string | null;
  comments?: string | null;
  type?: WorkflowActivityType;
  requiresOpen?: boolean;
  boundaryTimers: Entities.MList<WorkflowEventEntity>;
  estimatedDuration?: number | null;
  viewName?: string | null;
  script?: WorkflowScriptPartEmbedded | null;
  xml?: WorkflowXmlEmbedded | null;
  subWorkflow?: SubWorkflowEmbedded | null;
  userHelp?: string | null;
}

export module WorkflowActivityMessage {
  export const DuplicateViewNameFound0 = new MessageKey("WorkflowActivityMessage", "DuplicateViewNameFound0");
  export const ChooseADestinationForWorkflowJumping = new MessageKey("WorkflowActivityMessage", "ChooseADestinationForWorkflowJumping");
  export const CaseFlow = new MessageKey("WorkflowActivityMessage", "CaseFlow");
  export const AverageDuration = new MessageKey("WorkflowActivityMessage", "AverageDuration");
  export const ActivityIs = new MessageKey("WorkflowActivityMessage", "ActivityIs");
  export const NoActiveTimerFound = new MessageKey("WorkflowActivityMessage", "NoActiveTimerFound");
  export const InprogressWorkflowActivities = new MessageKey("WorkflowActivityMessage", "InprogressWorkflowActivities");
  export const OpenCaseActivityStats = new MessageKey("WorkflowActivityMessage", "OpenCaseActivityStats");
  export const LocateWorkflowActivityInDiagram = new MessageKey("WorkflowActivityMessage", "LocateWorkflowActivityInDiagram");
}

export const WorkflowActivityModel = new Type<WorkflowActivityModel>("WorkflowActivityModel");
export interface WorkflowActivityModel extends Entities.ModelEntity {
  Type: "WorkflowActivityModel";
  workflowActivity?: Entities.Lite<WorkflowActivityEntity> | null;
  workflow?: WorkflowEntity | null;
  mainEntityType: Basics.TypeEntity;
  name?: string | null;
  type?: WorkflowActivityType;
  requiresOpen?: boolean;
  boundaryTimers: Entities.MList<WorkflowEventModel>;
  estimatedDuration?: number | null;
  script?: WorkflowScriptPartEmbedded | null;
  viewName?: string | null;
  comments?: string | null;
  userHelp?: string | null;
  subWorkflow?: SubWorkflowEmbedded | null;
}

export module WorkflowActivityMonitorMessage {
  export const WorkflowActivityMonitor = new MessageKey("WorkflowActivityMonitorMessage", "WorkflowActivityMonitor");
  export const Draw = new MessageKey("WorkflowActivityMonitorMessage", "Draw");
  export const ResetZoom = new MessageKey("WorkflowActivityMonitorMessage", "ResetZoom");
  export const Find = new MessageKey("WorkflowActivityMonitorMessage", "Find");
  export const Filters = new MessageKey("WorkflowActivityMonitorMessage", "Filters");
  export const Columns = new MessageKey("WorkflowActivityMonitorMessage", "Columns");
}

export module WorkflowActivityOperation {
  export const Save : Entities.ExecuteSymbol<WorkflowActivityEntity> = registerSymbol("Operation", "WorkflowActivityOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowActivityEntity> = registerSymbol("Operation", "WorkflowActivityOperation.Delete");
}

export const WorkflowActivityType = new EnumType<WorkflowActivityType>("WorkflowActivityType");
export type WorkflowActivityType =
  "Task" |
  "Decision" |
  "DecompositionWorkflow" |
  "CallWorkflow" |
  "Script";

export const WorkflowConditionEntity = new Type<WorkflowConditionEntity>("WorkflowCondition");
export interface WorkflowConditionEntity extends Entities.Entity {
  Type: "WorkflowCondition";
  name?: string | null;
  mainEntityType?: Basics.TypeEntity | null;
  eval?: WorkflowConditionEval | null;
}

export const WorkflowConditionEval = new Type<WorkflowConditionEval>("WorkflowConditionEval");
export interface WorkflowConditionEval extends Dynamic.EvalEmbedded<IWorkflowConditionEvaluator> {
  Type: "WorkflowConditionEval";
}

export module WorkflowConditionOperation {
  export const Clone : Entities.ConstructSymbol_From<WorkflowConditionEntity, WorkflowConditionEntity> = registerSymbol("Operation", "WorkflowConditionOperation.Clone");
  export const Save : Entities.ExecuteSymbol<WorkflowConditionEntity> = registerSymbol("Operation", "WorkflowConditionOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowConditionEntity> = registerSymbol("Operation", "WorkflowConditionOperation.Delete");
}

export const WorkflowConfigurationEmbedded = new Type<WorkflowConfigurationEmbedded>("WorkflowConfigurationEmbedded");
export interface WorkflowConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "WorkflowConfigurationEmbedded";
  scriptRunnerPeriod?: number;
  avoidExecutingScriptsOlderThan?: number | null;
  chunkSizeRunningScripts?: number;
}

export const WorkflowConnectionEntity = new Type<WorkflowConnectionEntity>("WorkflowConnection");
export interface WorkflowConnectionEntity extends Entities.Entity, IWorkflowObjectEntity {
  Type: "WorkflowConnection";
  from?: IWorkflowNodeEntity | null;
  to?: IWorkflowNodeEntity | null;
  name?: string | null;
  bpmnElementId?: string | null;
  type?: ConnectionType;
  condition?: Entities.Lite<WorkflowConditionEntity> | null;
  action?: Entities.Lite<WorkflowActionEntity> | null;
  order?: number | null;
  xml?: WorkflowXmlEmbedded | null;
}

export const WorkflowConnectionModel = new Type<WorkflowConnectionModel>("WorkflowConnectionModel");
export interface WorkflowConnectionModel extends Entities.ModelEntity {
  Type: "WorkflowConnectionModel";
  mainEntityType: Basics.TypeEntity;
  name?: string | null;
  needCondition?: boolean;
  needOrder?: boolean;
  type?: ConnectionType;
  condition?: Entities.Lite<WorkflowConditionEntity> | null;
  action?: Entities.Lite<WorkflowActionEntity> | null;
  order?: number | null;
}

export module WorkflowConnectionOperation {
  export const Save : Entities.ExecuteSymbol<WorkflowConnectionEntity> = registerSymbol("Operation", "WorkflowConnectionOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowConnectionEntity> = registerSymbol("Operation", "WorkflowConnectionOperation.Delete");
}

export const WorkflowEntity = new Type<WorkflowEntity>("Workflow");
export interface WorkflowEntity extends Entities.Entity {
  Type: "Workflow";
  name?: string | null;
  mainEntityType?: Basics.TypeEntity | null;
  mainEntityStrategies: Entities.MList<WorkflowMainEntityStrategy>;
  expirationDate?: string | null;
}

export const WorkflowEventEntity = new Type<WorkflowEventEntity>("WorkflowEvent");
export interface WorkflowEventEntity extends Entities.Entity, IWorkflowNodeEntity, IWorkflowObjectEntity {
  Type: "WorkflowEvent";
  name?: string | null;
  bpmnElementId?: string | null;
  lane?: WorkflowLaneEntity | null;
  type?: WorkflowEventType;
  timer?: WorkflowTimerEmbedded | null;
  boundaryOf?: Entities.Lite<WorkflowActivityEntity> | null;
  xml?: WorkflowXmlEmbedded | null;
}

export const WorkflowEventModel = new Type<WorkflowEventModel>("WorkflowEventModel");
export interface WorkflowEventModel extends Entities.ModelEntity {
  Type: "WorkflowEventModel";
  mainEntityType: Basics.TypeEntity;
  name?: string | null;
  type?: WorkflowEventType;
  task?: WorkflowEventTaskModel | null;
  timer?: WorkflowTimerEmbedded | null;
  bpmnElementId?: string | null;
}

export module WorkflowEventOperation {
  export const Save : Entities.ExecuteSymbol<WorkflowEventEntity> = registerSymbol("Operation", "WorkflowEventOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowEventEntity> = registerSymbol("Operation", "WorkflowEventOperation.Delete");
}

export const WorkflowEventTaskActionEval = new Type<WorkflowEventTaskActionEval>("WorkflowEventTaskActionEval");
export interface WorkflowEventTaskActionEval extends Dynamic.EvalEmbedded<IWorkflowEventTaskActionEval> {
  Type: "WorkflowEventTaskActionEval";
}

export const WorkflowEventTaskConditionEval = new Type<WorkflowEventTaskConditionEval>("WorkflowEventTaskConditionEval");
export interface WorkflowEventTaskConditionEval extends Dynamic.EvalEmbedded<IWorkflowEventTaskConditionEvaluator> {
  Type: "WorkflowEventTaskConditionEval";
}

export const WorkflowEventTaskConditionResultEntity = new Type<WorkflowEventTaskConditionResultEntity>("WorkflowEventTaskConditionResult");
export interface WorkflowEventTaskConditionResultEntity extends Entities.Entity {
  Type: "WorkflowEventTaskConditionResult";
  creationDate?: string;
  workflowEventTask?: Entities.Lite<WorkflowEventTaskEntity> | null;
  result?: boolean;
}

export const WorkflowEventTaskEntity = new Type<WorkflowEventTaskEntity>("WorkflowEventTask");
export interface WorkflowEventTaskEntity extends Entities.Entity, Scheduler.ITaskEntity {
  Type: "WorkflowEventTask";
  workflow?: Entities.Lite<WorkflowEntity> | null;
  event?: Entities.Lite<WorkflowEventEntity> | null;
  triggeredOn?: TriggeredOn;
  condition?: WorkflowEventTaskConditionEval | null;
  action?: WorkflowEventTaskActionEval | null;
}

export const WorkflowEventTaskModel = new Type<WorkflowEventTaskModel>("WorkflowEventTaskModel");
export interface WorkflowEventTaskModel extends Entities.ModelEntity {
  Type: "WorkflowEventTaskModel";
  suspended?: boolean;
  rule?: Scheduler.IScheduleRuleEntity | null;
  triggeredOn?: TriggeredOn;
  condition?: WorkflowEventTaskConditionEval | null;
  action?: WorkflowEventTaskActionEval | null;
}

export module WorkflowEventTaskOperation {
  export const Save : Entities.ExecuteSymbol<WorkflowEventTaskEntity> = registerSymbol("Operation", "WorkflowEventTaskOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowEventTaskEntity> = registerSymbol("Operation", "WorkflowEventTaskOperation.Delete");
}

export const WorkflowEventType = new EnumType<WorkflowEventType>("WorkflowEventType");
export type WorkflowEventType =
  "Start" |
  "ScheduledStart" |
  "Finish" |
  "BoundaryForkTimer" |
  "BoundaryInterruptingTimer" |
  "IntermediateTimer";

export const WorkflowGatewayDirection = new EnumType<WorkflowGatewayDirection>("WorkflowGatewayDirection");
export type WorkflowGatewayDirection =
  "Split" |
  "Join";

export const WorkflowGatewayEntity = new Type<WorkflowGatewayEntity>("WorkflowGateway");
export interface WorkflowGatewayEntity extends Entities.Entity, IWorkflowNodeEntity, IWorkflowObjectEntity {
  Type: "WorkflowGateway";
  lane?: WorkflowLaneEntity | null;
  name?: string | null;
  bpmnElementId?: string | null;
  type?: WorkflowGatewayType;
  direction?: WorkflowGatewayDirection;
  xml?: WorkflowXmlEmbedded | null;
}

export const WorkflowGatewayModel = new Type<WorkflowGatewayModel>("WorkflowGatewayModel");
export interface WorkflowGatewayModel extends Entities.ModelEntity {
  Type: "WorkflowGatewayModel";
  name?: string | null;
  type?: WorkflowGatewayType;
  direction?: WorkflowGatewayDirection;
}

export module WorkflowGatewayOperation {
  export const Save : Entities.ExecuteSymbol<WorkflowGatewayEntity> = registerSymbol("Operation", "WorkflowGatewayOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowGatewayEntity> = registerSymbol("Operation", "WorkflowGatewayOperation.Delete");
}

export const WorkflowGatewayType = new EnumType<WorkflowGatewayType>("WorkflowGatewayType");
export type WorkflowGatewayType =
  "Exclusive" |
  "Inclusive" |
  "Parallel";

export const WorkflowIssueType = new EnumType<WorkflowIssueType>("WorkflowIssueType");
export type WorkflowIssueType =
  "Warning" |
  "Error";

export const WorkflowLaneActorsEval = new Type<WorkflowLaneActorsEval>("WorkflowLaneActorsEval");
export interface WorkflowLaneActorsEval extends Dynamic.EvalEmbedded<IWorkflowLaneActorsEvaluator> {
  Type: "WorkflowLaneActorsEval";
}

export const WorkflowLaneEntity = new Type<WorkflowLaneEntity>("WorkflowLane");
export interface WorkflowLaneEntity extends Entities.Entity, IWorkflowObjectEntity {
  Type: "WorkflowLane";
  name?: string | null;
  bpmnElementId?: string | null;
  xml?: WorkflowXmlEmbedded | null;
  pool?: WorkflowPoolEntity | null;
  actors: Entities.MList<Entities.Lite<Entities.Entity>>;
  actorsEval?: WorkflowLaneActorsEval | null;
}

export const WorkflowLaneModel = new Type<WorkflowLaneModel>("WorkflowLaneModel");
export interface WorkflowLaneModel extends Entities.ModelEntity {
  Type: "WorkflowLaneModel";
  mainEntityType: Basics.TypeEntity;
  name?: string | null;
  actors: Entities.MList<Entities.Lite<Entities.Entity>>;
  actorsEval?: WorkflowLaneActorsEval | null;
}

export module WorkflowLaneOperation {
  export const Save : Entities.ExecuteSymbol<WorkflowLaneEntity> = registerSymbol("Operation", "WorkflowLaneOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowLaneEntity> = registerSymbol("Operation", "WorkflowLaneOperation.Delete");
}

export const WorkflowMainEntityStrategy = new EnumType<WorkflowMainEntityStrategy>("WorkflowMainEntityStrategy");
export type WorkflowMainEntityStrategy =
  "CreateNew" |
  "SelectByUser" |
  "Clone";

export module WorkflowMessage {
  export const _0BelongsToADifferentWorkflow = new MessageKey("WorkflowMessage", "_0BelongsToADifferentWorkflow");
  export const Condition0IsDefinedFor1Not2 = new MessageKey("WorkflowMessage", "Condition0IsDefinedFor1Not2");
  export const JumpsToSameActivityNotAllowed = new MessageKey("WorkflowMessage", "JumpsToSameActivityNotAllowed");
  export const JumpTo0FailedBecause1 = new MessageKey("WorkflowMessage", "JumpTo0FailedBecause1");
  export const ToUse0YouSouldSaveWorkflow = new MessageKey("WorkflowMessage", "ToUse0YouSouldSaveWorkflow");
  export const ToUseNewNodesOnJumpsYouSouldSaveWorkflow = new MessageKey("WorkflowMessage", "ToUseNewNodesOnJumpsYouSouldSaveWorkflow");
  export const ToUse0YouSouldSetTheWorkflow1 = new MessageKey("WorkflowMessage", "ToUse0YouSouldSetTheWorkflow1");
  export const ChangeWorkflowMainEntityTypeIsNotAllowedBecauseWeHaveNodesThatUseIt = new MessageKey("WorkflowMessage", "ChangeWorkflowMainEntityTypeIsNotAllowedBecauseWeHaveNodesThatUseIt");
  export const WorkflowUsedIn0ForDecompositionOrCallWorkflow = new MessageKey("WorkflowMessage", "WorkflowUsedIn0ForDecompositionOrCallWorkflow");
  export const Workflow0AlreadyActivated = new MessageKey("WorkflowMessage", "Workflow0AlreadyActivated");
  export const Workflow0HasExpiredOn1 = new MessageKey("WorkflowMessage", "Workflow0HasExpiredOn1");
  export const HasExpired = new MessageKey("WorkflowMessage", "HasExpired");
  export const DeactivateWorkflow = new MessageKey("WorkflowMessage", "DeactivateWorkflow");
  export const PleaseChooseExpirationDate = new MessageKey("WorkflowMessage", "PleaseChooseExpirationDate");
  export const ResetZoom = new MessageKey("WorkflowMessage", "ResetZoom");
  export const Color = new MessageKey("WorkflowMessage", "Color");
  export const WorkflowIssues = new MessageKey("WorkflowMessage", "WorkflowIssues");
  export const WorkflowProperties = new MessageKey("WorkflowMessage", "WorkflowProperties");
}

export const WorkflowModel = new Type<WorkflowModel>("WorkflowModel");
export interface WorkflowModel extends Entities.ModelEntity {
  Type: "WorkflowModel";
  diagramXml: string;
  entities: Entities.MList<BpmnEntityPairEmbedded>;
}

export module WorkflowOperation {
  export const Clone : Entities.ConstructSymbol_From<WorkflowEntity, WorkflowEntity> = registerSymbol("Operation", "WorkflowOperation.Clone");
  export const Save : Entities.ExecuteSymbol<WorkflowEntity> = registerSymbol("Operation", "WorkflowOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowEntity> = registerSymbol("Operation", "WorkflowOperation.Delete");
  export const Activate : Entities.ExecuteSymbol<WorkflowEntity> = registerSymbol("Operation", "WorkflowOperation.Activate");
  export const Deactivate : Entities.ExecuteSymbol<WorkflowEntity> = registerSymbol("Operation", "WorkflowOperation.Deactivate");
}

export module WorkflowPanelPermission {
  export const ViewWorkflowPanel : Authorization.PermissionSymbol = registerSymbol("Permission", "WorkflowPanelPermission.ViewWorkflowPanel");
}

export const WorkflowPoolEntity = new Type<WorkflowPoolEntity>("WorkflowPool");
export interface WorkflowPoolEntity extends Entities.Entity, IWorkflowObjectEntity {
  Type: "WorkflowPool";
  workflow?: WorkflowEntity | null;
  name?: string | null;
  bpmnElementId?: string | null;
  xml?: WorkflowXmlEmbedded | null;
}

export const WorkflowPoolModel = new Type<WorkflowPoolModel>("WorkflowPoolModel");
export interface WorkflowPoolModel extends Entities.ModelEntity {
  Type: "WorkflowPoolModel";
  name?: string | null;
}

export module WorkflowPoolOperation {
  export const Save : Entities.ExecuteSymbol<WorkflowPoolEntity> = registerSymbol("Operation", "WorkflowPoolOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowPoolEntity> = registerSymbol("Operation", "WorkflowPoolOperation.Delete");
}

export const WorkflowReplacementItemEmbedded = new Type<WorkflowReplacementItemEmbedded>("WorkflowReplacementItemEmbedded");
export interface WorkflowReplacementItemEmbedded extends Entities.EmbeddedEntity {
  Type: "WorkflowReplacementItemEmbedded";
  oldNode: Entities.Lite<IWorkflowNodeEntity>;
  subWorkflow?: Entities.Lite<WorkflowEntity> | null;
  newNode?: string | null;
}

export const WorkflowReplacementModel = new Type<WorkflowReplacementModel>("WorkflowReplacementModel");
export interface WorkflowReplacementModel extends Entities.ModelEntity {
  Type: "WorkflowReplacementModel";
  replacements: Entities.MList<WorkflowReplacementItemEmbedded>;
}

export const WorkflowScriptEntity = new Type<WorkflowScriptEntity>("WorkflowScript");
export interface WorkflowScriptEntity extends Entities.Entity {
  Type: "WorkflowScript";
  name?: string | null;
  mainEntityType?: Basics.TypeEntity | null;
  eval?: WorkflowScriptEval | null;
}

export const WorkflowScriptEval = new Type<WorkflowScriptEval>("WorkflowScriptEval");
export interface WorkflowScriptEval extends Dynamic.EvalEmbedded<IWorkflowScriptExecutor> {
  Type: "WorkflowScriptEval";
  customTypes?: string | null;
}

export module WorkflowScriptOperation {
  export const Clone : Entities.ConstructSymbol_From<WorkflowScriptEntity, WorkflowScriptEntity> = registerSymbol("Operation", "WorkflowScriptOperation.Clone");
  export const Save : Entities.ExecuteSymbol<WorkflowScriptEntity> = registerSymbol("Operation", "WorkflowScriptOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowScriptEntity> = registerSymbol("Operation", "WorkflowScriptOperation.Delete");
}

export const WorkflowScriptPartEmbedded = new Type<WorkflowScriptPartEmbedded>("WorkflowScriptPartEmbedded");
export interface WorkflowScriptPartEmbedded extends Entities.EmbeddedEntity {
  Type: "WorkflowScriptPartEmbedded";
  script?: Entities.Lite<WorkflowScriptEntity> | null;
  retryStrategy?: WorkflowScriptRetryStrategyEntity | null;
}

export const WorkflowScriptRetryStrategyEntity = new Type<WorkflowScriptRetryStrategyEntity>("WorkflowScriptRetryStrategy");
export interface WorkflowScriptRetryStrategyEntity extends Entities.Entity {
  Type: "WorkflowScriptRetryStrategy";
  rule?: string | null;
}

export module WorkflowScriptRetryStrategyOperation {
  export const Save : Entities.ExecuteSymbol<WorkflowScriptRetryStrategyEntity> = registerSymbol("Operation", "WorkflowScriptRetryStrategyOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowScriptRetryStrategyEntity> = registerSymbol("Operation", "WorkflowScriptRetryStrategyOperation.Delete");
}

export const WorkflowTimerConditionEntity = new Type<WorkflowTimerConditionEntity>("WorkflowTimerCondition");
export interface WorkflowTimerConditionEntity extends Entities.Entity {
  Type: "WorkflowTimerCondition";
  name?: string | null;
  mainEntityType?: Basics.TypeEntity | null;
  eval?: WorkflowTimerConditionEval | null;
}

export const WorkflowTimerConditionEval = new Type<WorkflowTimerConditionEval>("WorkflowTimerConditionEval");
export interface WorkflowTimerConditionEval extends Dynamic.EvalEmbedded<IWorkflowTimerConditionEvaluator> {
  Type: "WorkflowTimerConditionEval";
}

export module WorkflowTimerConditionOperation {
  export const Clone : Entities.ConstructSymbol_From<WorkflowTimerConditionEntity, WorkflowTimerConditionEntity> = registerSymbol("Operation", "WorkflowTimerConditionOperation.Clone");
  export const Save : Entities.ExecuteSymbol<WorkflowTimerConditionEntity> = registerSymbol("Operation", "WorkflowTimerConditionOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowTimerConditionEntity> = registerSymbol("Operation", "WorkflowTimerConditionOperation.Delete");
}

export const WorkflowTimerEmbedded = new Type<WorkflowTimerEmbedded>("WorkflowTimerEmbedded");
export interface WorkflowTimerEmbedded extends Entities.EmbeddedEntity {
  Type: "WorkflowTimerEmbedded";
  duration?: Signum.TimeSpanEmbedded | null;
  condition?: Entities.Lite<WorkflowTimerConditionEntity> | null;
}

export module WorkflowValidationMessage {
  export const NodeType0WithId1IsInvalid = new MessageKey("WorkflowValidationMessage", "NodeType0WithId1IsInvalid");
  export const ParticipantsAndProcessesAreNotSynchronized = new MessageKey("WorkflowValidationMessage", "ParticipantsAndProcessesAreNotSynchronized");
  export const MultipleStartEventsAreNotAllowed = new MessageKey("WorkflowValidationMessage", "MultipleStartEventsAreNotAllowed");
  export const SomeStartEventIsRequired = new MessageKey("WorkflowValidationMessage", "SomeStartEventIsRequired");
  export const NormalStartEventIsRequiredWhenThe0Are1Or2 = new MessageKey("WorkflowValidationMessage", "NormalStartEventIsRequiredWhenThe0Are1Or2");
  export const TheFollowingTasksAreGoingToBeDeleted = new MessageKey("WorkflowValidationMessage", "TheFollowingTasksAreGoingToBeDeleted");
  export const FinishEventIsRequired = new MessageKey("WorkflowValidationMessage", "FinishEventIsRequired");
  export const _0HasInputs = new MessageKey("WorkflowValidationMessage", "_0HasInputs");
  export const _0HasOutputs = new MessageKey("WorkflowValidationMessage", "_0HasOutputs");
  export const _0HasNoInputs = new MessageKey("WorkflowValidationMessage", "_0HasNoInputs");
  export const _0HasNoOutputs = new MessageKey("WorkflowValidationMessage", "_0HasNoOutputs");
  export const _0HasJustOneInputAndOneOutput = new MessageKey("WorkflowValidationMessage", "_0HasJustOneInputAndOneOutput");
  export const _0HasMultipleOutputs = new MessageKey("WorkflowValidationMessage", "_0HasMultipleOutputs");
  export const IsNotInWorkflow = new MessageKey("WorkflowValidationMessage", "IsNotInWorkflow");
  export const Activity0CanNotJumpTo1Because2 = new MessageKey("WorkflowValidationMessage", "Activity0CanNotJumpTo1Because2");
  export const Activity0CanNotTimeoutTo1Because2 = new MessageKey("WorkflowValidationMessage", "Activity0CanNotTimeoutTo1Because2");
  export const IsStart = new MessageKey("WorkflowValidationMessage", "IsStart");
  export const IsSelfJumping = new MessageKey("WorkflowValidationMessage", "IsSelfJumping");
  export const IsInDifferentParallelTrack = new MessageKey("WorkflowValidationMessage", "IsInDifferentParallelTrack");
  export const _0Track1CanNotBeConnectedTo2Track3InsteadOfTrack4 = new MessageKey("WorkflowValidationMessage", "_0Track1CanNotBeConnectedTo2Track3InsteadOfTrack4");
  export const StartEventNextNodeShouldBeAnActivity = new MessageKey("WorkflowValidationMessage", "StartEventNextNodeShouldBeAnActivity");
  export const ParallelGatewaysShouldPair = new MessageKey("WorkflowValidationMessage", "ParallelGatewaysShouldPair");
  export const TimerOrConditionalStartEventsCanNotGoToJoinGateways = new MessageKey("WorkflowValidationMessage", "TimerOrConditionalStartEventsCanNotGoToJoinGateways");
  export const InclusiveGateway0ShouldHaveOneConnectionWithoutCondition = new MessageKey("WorkflowValidationMessage", "InclusiveGateway0ShouldHaveOneConnectionWithoutCondition");
  export const Gateway0ShouldHasConditionOrDecisionOnEachOutputExceptTheLast = new MessageKey("WorkflowValidationMessage", "Gateway0ShouldHasConditionOrDecisionOnEachOutputExceptTheLast");
  export const _0CanNotBeConnectedToAParallelJoinBecauseHasNoPreviousParallelSplit = new MessageKey("WorkflowValidationMessage", "_0CanNotBeConnectedToAParallelJoinBecauseHasNoPreviousParallelSplit");
  export const Activity0WithDecisionTypeShouldGoToAnExclusiveOrInclusiveGateways = new MessageKey("WorkflowValidationMessage", "Activity0WithDecisionTypeShouldGoToAnExclusiveOrInclusiveGateways");
  export const Activity0ShouldBeDecision = new MessageKey("WorkflowValidationMessage", "Activity0ShouldBeDecision");
  export const _0IsTimerStartAndSchedulerIsMandatory = new MessageKey("WorkflowValidationMessage", "_0IsTimerStartAndSchedulerIsMandatory");
  export const _0IsTimerStartAndTaskIsMandatory = new MessageKey("WorkflowValidationMessage", "_0IsTimerStartAndTaskIsMandatory");
  export const _0IsConditionalStartAndTaskConditionIsMandatory = new MessageKey("WorkflowValidationMessage", "_0IsConditionalStartAndTaskConditionIsMandatory");
  export const DelayActivitiesShouldHaveExactlyOneInterruptingTimer = new MessageKey("WorkflowValidationMessage", "DelayActivitiesShouldHaveExactlyOneInterruptingTimer");
  export const Activity0OfType1ShouldHaveExactlyOneConnectionOfType2 = new MessageKey("WorkflowValidationMessage", "Activity0OfType1ShouldHaveExactlyOneConnectionOfType2");
  export const Activity0OfType1CanNotHaveConnectionsOfType2 = new MessageKey("WorkflowValidationMessage", "Activity0OfType1CanNotHaveConnectionsOfType2");
  export const BoundaryTimer0OfActivity1ShouldHaveExactlyOneConnectionOfType2 = new MessageKey("WorkflowValidationMessage", "BoundaryTimer0OfActivity1ShouldHaveExactlyOneConnectionOfType2");
  export const IntermediateTimer0ShouldHaveOneOutputOfType1 = new MessageKey("WorkflowValidationMessage", "IntermediateTimer0ShouldHaveOneOutputOfType1");
  export const ParallelSplit0ShouldHaveAtLeastOneConnection = new MessageKey("WorkflowValidationMessage", "ParallelSplit0ShouldHaveAtLeastOneConnection");
  export const ParallelSplit0ShouldHaveOnlyNormalConnectionsWithoutConditions = new MessageKey("WorkflowValidationMessage", "ParallelSplit0ShouldHaveOnlyNormalConnectionsWithoutConditions");
  export const Join0OfType1DoesNotMatchWithItsPairTheSplit2OfType3 = new MessageKey("WorkflowValidationMessage", "Join0OfType1DoesNotMatchWithItsPairTheSplit2OfType3");
}

export const WorkflowXmlEmbedded = new Type<WorkflowXmlEmbedded>("WorkflowXmlEmbedded");
export interface WorkflowXmlEmbedded extends Entities.EmbeddedEntity {
  Type: "WorkflowXmlEmbedded";
  diagramXml?: string | null;
}


