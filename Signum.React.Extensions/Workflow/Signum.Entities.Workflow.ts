//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum.React/Scripts/Reflection'
import * as Entities from '../../Signum.React/Scripts/Signum.Entities'
import * as Basics from '../../Signum.React/Scripts/Signum.Entities.Basics'
import * as Authorization from '../Authorization/Signum.Entities.Authorization'
import * as UserAssets from '../UserAssets/Signum.Entities.UserAssets'
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
  workflowActivity: Entities.Lite<WorkflowActivityEntity> | null;
  case: Entities.Lite<CaseEntity>;
  caseActivity: Entities.Lite<CaseActivityEntity> | null;
  notification: Entities.Lite<CaseNotificationEntity> | null;
  remarks: string | null;
  alerts: number;
  tags: Array<CaseTagTypeEntity>;
}

export const BpmnEntityPairEmbedded = new Type<BpmnEntityPairEmbedded>("BpmnEntityPairEmbedded");
export interface BpmnEntityPairEmbedded extends Entities.EmbeddedEntity {
  Type: "BpmnEntityPairEmbedded";
  model: Entities.ModelEntity;
  bpmnElementId: string;
}

export const ButtonOptionEmbedded = new Type<ButtonOptionEmbedded>("ButtonOptionEmbedded");
export interface ButtonOptionEmbedded extends Entities.EmbeddedEntity {
  Type: "ButtonOptionEmbedded";
  name: string;
  style: Signum.BootstrapStyle;
}

export const CaseActivityEntity = new Type<CaseActivityEntity>("CaseActivity");
export interface CaseActivityEntity extends Entities.Entity {
  Type: "CaseActivity";
  case: CaseEntity;
  workflowActivity: IWorkflowNodeEntity;
  originalWorkflowActivityName: string;
  startDate: string /*DateTime*/;
  previous: Entities.Lite<CaseActivityEntity> | null;
  note: string | null;
  doneDate: string /*DateTime*/ | null;
  duration: number | null;
  doneBy: Entities.Lite<Authorization.UserEntity> | null;
  doneType: DoneType | null;
  doneDecision: string | null;
  scriptExecution: ScriptExecutionEmbedded | null;
}

export const CaseActivityExecutedTimerEntity = new Type<CaseActivityExecutedTimerEntity>("CaseActivityExecutedTimer");
export interface CaseActivityExecutedTimerEntity extends Entities.Entity {
  Type: "CaseActivityExecutedTimer";
  creationDate: string /*DateTime*/;
  caseActivity: Entities.Lite<CaseActivityEntity>;
  boundaryEvent: Entities.Lite<WorkflowEventEntity>;
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
  export const NoActorsFoundToInsertCaseActivityNotifications = new MessageKey("CaseActivityMessage", "NoActorsFoundToInsertCaseActivityNotifications");
  export const ThereAreInprogressActivities = new MessageKey("CaseActivityMessage", "ThereAreInprogressActivities");
  export const ShowHelp = new MessageKey("CaseActivityMessage", "ShowHelp");
  export const HideHelp = new MessageKey("CaseActivityMessage", "HideHelp");
  export const CanceledCase = new MessageKey("CaseActivityMessage", "CanceledCase");
  export const AlreadyFinished = new MessageKey("CaseActivityMessage", "AlreadyFinished");
  export const NotCanceled = new MessageKey("CaseActivityMessage", "NotCanceled");
}

export const CaseActivityMixin = new Type<CaseActivityMixin>("CaseActivityMixin");
export interface CaseActivityMixin extends Entities.MixinEntity {
  Type: "CaseActivityMixin";
  caseActivity: Entities.Lite<CaseActivityEntity> | null;
}

export module CaseActivityOperation {
  export const CreateCaseActivityFromWorkflow : Entities.ConstructSymbol_From<CaseActivityEntity, WorkflowEntity> = registerSymbol("Operation", "CaseActivityOperation.CreateCaseActivityFromWorkflow");
  export const CreateCaseFromWorkflowEventTask : Entities.ConstructSymbol_From<CaseEntity, WorkflowEventTaskEntity> = registerSymbol("Operation", "CaseActivityOperation.CreateCaseFromWorkflowEventTask");
  export const Register : Entities.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Register");
  export const Delete : Entities.DeleteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Delete");
  export const Next : Entities.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Next");
  export const Jump : Entities.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Jump");
  export const FreeJump : Entities.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.FreeJump");
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
  parentCase: Entities.Lite<CaseEntity> | null;
  description: string;
  mainEntity: ICaseMainEntity;
  startDate: string /*DateTime*/;
  finishDate: string /*DateTime*/ | null;
}

export const CaseFlowColor = new EnumType<CaseFlowColor>("CaseFlowColor");
export type CaseFlowColor =
  "CaseMaxDuration" |
  "AverageDuration" |
  "EstimatedDuration";

export const CaseJunctionEntity = new Type<CaseJunctionEntity>("CaseJunction");
export interface CaseJunctionEntity extends Entities.Entity {
  Type: "CaseJunction";
  direction: WorkflowGatewayDirection;
  from: Entities.Lite<CaseActivityEntity>;
  to: Entities.Lite<CaseActivityEntity>;
}

export module CaseMessage {
  export const DeleteMainEntity = new MessageKey("CaseMessage", "DeleteMainEntity");
  export const DoYouWAntToAlsoDeleteTheMainEntity0 = new MessageKey("CaseMessage", "DoYouWAntToAlsoDeleteTheMainEntity0");
  export const DoYouWAntToAlsoDeleteTheMainEntities = new MessageKey("CaseMessage", "DoYouWAntToAlsoDeleteTheMainEntities");
}

export const CaseNotificationEntity = new Type<CaseNotificationEntity>("CaseNotification");
export interface CaseNotificationEntity extends Entities.Entity {
  Type: "CaseNotification";
  caseActivity: Entities.Lite<CaseActivityEntity>;
  user: Entities.Lite<Authorization.UserEntity>;
  actor: Entities.Lite<Entities.Entity>;
  remarks: string | null;
  state: CaseNotificationState;
}

export module CaseNotificationOperation {
  export const SetRemarks : Entities.ExecuteSymbol<CaseNotificationEntity> = registerSymbol("Operation", "CaseNotificationOperation.SetRemarks");
  export const Delete : Entities.DeleteSymbol<CaseNotificationEntity> = registerSymbol("Operation", "CaseNotificationOperation.Delete");
  export const CreateCaseNotificationFromCaseActivity : Entities.ConstructSymbol_From<CaseNotificationEntity, CaseActivityEntity> = registerSymbol("Operation", "CaseNotificationOperation.CreateCaseNotificationFromCaseActivity");
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
  export const Cancel : Entities.ExecuteSymbol<CaseEntity> = registerSymbol("Operation", "CaseOperation.Cancel");
  export const Reactivate : Entities.ExecuteSymbol<CaseEntity> = registerSymbol("Operation", "CaseOperation.Reactivate");
  export const Delete : Entities.DeleteSymbol<CaseEntity> = registerSymbol("Operation", "CaseOperation.Delete");
}

export const CaseTagEntity = new Type<CaseTagEntity>("CaseTag");
export interface CaseTagEntity extends Entities.Entity {
  Type: "CaseTag";
  creationDate: string /*DateTime*/;
  case: Entities.Lite<CaseEntity>;
  tagType: CaseTagTypeEntity;
  createdBy: Entities.Lite<Basics.IUserEntity>;
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
  name: string;
  color: string;
}

export module CaseTagTypeOperation {
  export const Save : Entities.ExecuteSymbol<CaseTagTypeEntity> = registerSymbol("Operation", "CaseTagTypeOperation.Save");
}

export const ConnectionType = new EnumType<ConnectionType>("ConnectionType");
export type ConnectionType =
  "Normal" |
  "Decision" |
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
  range: DateFilterRange;
  states: Entities.MList<CaseNotificationState>;
  fromDate: string /*DateTime*/ | null;
  toDate: string /*DateTime*/ | null;
}

export module InboxMessage {
  export const Clear = new MessageKey("InboxMessage", "Clear");
  export const Activity = new MessageKey("InboxMessage", "Activity");
  export const SenderNote = new MessageKey("InboxMessage", "SenderNote");
  export const Sender = new MessageKey("InboxMessage", "Sender");
  export const Filters = new MessageKey("InboxMessage", "Filters");
}

export interface IWorkflowNodeEntity extends IWorkflowObjectEntity, Entities.Entity {
  lane: WorkflowLaneEntity;
}

export interface IWorkflowObjectEntity extends Entities.Entity {
  xml: WorkflowXmlEmbedded;
  bpmnElementId: string;
}

export const NewTasksEmbedded = new Type<NewTasksEmbedded>("NewTasksEmbedded");
export interface NewTasksEmbedded extends Entities.EmbeddedEntity {
  Type: "NewTasksEmbedded";
  bpmnId: string;
  name: string | null;
  subWorkflow: Entities.Lite<WorkflowEntity> | null;
}

export const ScriptExecutionEmbedded = new Type<ScriptExecutionEmbedded>("ScriptExecutionEmbedded");
export interface ScriptExecutionEmbedded extends Entities.EmbeddedEntity {
  Type: "ScriptExecutionEmbedded";
  nextExecution: string /*DateTime*/;
  retryCount: number;
  processIdentifier: string /*Guid*/ | null;
}

export const SubEntitiesEval = new Type<SubEntitiesEval>("SubEntitiesEval");
export interface SubEntitiesEval extends Dynamic.EvalEmbedded<ISubEntitiesEvaluator> {
  Type: "SubEntitiesEval";
}

export const SubWorkflowEmbedded = new Type<SubWorkflowEmbedded>("SubWorkflowEmbedded");
export interface SubWorkflowEmbedded extends Entities.EmbeddedEntity {
  Type: "SubWorkflowEmbedded";
  workflow: WorkflowEntity;
  subEntitiesEval: SubEntitiesEval;
}

export const TriggeredOn = new EnumType<TriggeredOn>("TriggeredOn");
export type TriggeredOn =
  "Always" |
  "ConditionIsTrue" |
  "ConditionChangesToTrue";

export const ViewNamePropEmbedded = new Type<ViewNamePropEmbedded>("ViewNamePropEmbedded");
export interface ViewNamePropEmbedded extends Entities.EmbeddedEntity {
  Type: "ViewNamePropEmbedded";
  name: string;
  expression: string | null;
}

export const WorkflowActionEntity = new Type<WorkflowActionEntity>("WorkflowAction");
export interface WorkflowActionEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "WorkflowAction";
  name: string;
  guid: string /*Guid*/;
  mainEntityType: Basics.TypeEntity;
  eval: WorkflowActionEval;
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
  lane: WorkflowLaneEntity;
  name: string;
  bpmnElementId: string;
  type: WorkflowActivityType;
  comments: string | null;
  requiresOpen: boolean;
  decisionOptions: Entities.MList<ButtonOptionEmbedded>;
  customNextButton: ButtonOptionEmbedded | null;
  boundaryTimers: Entities.MList<WorkflowEventEntity>;
  estimatedDuration: number | null;
  viewName: string | null;
  viewNameProps: Entities.MList<ViewNamePropEmbedded>;
  script: WorkflowScriptPartEmbedded | null;
  xml: WorkflowXmlEmbedded;
  subWorkflow: SubWorkflowEmbedded | null;
  userHelp: string | null;
}

export module WorkflowActivityMessage {
  export const DuplicateViewNameFound0 = new MessageKey("WorkflowActivityMessage", "DuplicateViewNameFound0");
  export const ChooseADestinationForWorkflowJumping = new MessageKey("WorkflowActivityMessage", "ChooseADestinationForWorkflowJumping");
  export const CaseFlow = new MessageKey("WorkflowActivityMessage", "CaseFlow");
  export const AverageDuration = new MessageKey("WorkflowActivityMessage", "AverageDuration");
  export const ActivityIs = new MessageKey("WorkflowActivityMessage", "ActivityIs");
  export const NoActiveTimerFound = new MessageKey("WorkflowActivityMessage", "NoActiveTimerFound");
  export const InprogressCaseActivities = new MessageKey("WorkflowActivityMessage", "InprogressCaseActivities");
  export const OpenCaseActivityStats = new MessageKey("WorkflowActivityMessage", "OpenCaseActivityStats");
  export const LocateWorkflowActivityInDiagram = new MessageKey("WorkflowActivityMessage", "LocateWorkflowActivityInDiagram");
  export const Approve = new MessageKey("WorkflowActivityMessage", "Approve");
  export const Decline = new MessageKey("WorkflowActivityMessage", "Decline");
}

export const WorkflowActivityModel = new Type<WorkflowActivityModel>("WorkflowActivityModel");
export interface WorkflowActivityModel extends Entities.ModelEntity {
  Type: "WorkflowActivityModel";
  workflowActivity: Entities.Lite<WorkflowActivityEntity> | null;
  workflow: WorkflowEntity | null;
  mainEntityType: Basics.TypeEntity;
  name: string;
  type: WorkflowActivityType;
  requiresOpen: boolean;
  decisionOptions: Entities.MList<ButtonOptionEmbedded>;
  customNextButton: ButtonOptionEmbedded | null;
  boundaryTimers: Entities.MList<WorkflowEventModel>;
  estimatedDuration: number | null;
  script: WorkflowScriptPartEmbedded | null;
  viewName: string | null;
  viewNameProps: Entities.MList<ViewNamePropEmbedded>;
  comments: string | null;
  userHelp: string | null;
  subWorkflow: SubWorkflowEmbedded | null;
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
export interface WorkflowConditionEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "WorkflowCondition";
  name: string;
  guid: string /*Guid*/;
  mainEntityType: Basics.TypeEntity;
  eval: WorkflowConditionEval;
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
  scriptRunnerPeriod: number;
  avoidExecutingScriptsOlderThan: number | null;
  chunkSizeRunningScripts: number;
}

export const WorkflowConnectionEntity = new Type<WorkflowConnectionEntity>("WorkflowConnection");
export interface WorkflowConnectionEntity extends Entities.Entity, IWorkflowObjectEntity {
  Type: "WorkflowConnection";
  from: IWorkflowNodeEntity;
  to: IWorkflowNodeEntity;
  name: string | null;
  decisionOptionName: string | null;
  bpmnElementId: string;
  type: ConnectionType;
  condition: Entities.Lite<WorkflowConditionEntity> | null;
  action: Entities.Lite<WorkflowActionEntity> | null;
  order: number | null;
  xml: WorkflowXmlEmbedded;
}

export const WorkflowConnectionModel = new Type<WorkflowConnectionModel>("WorkflowConnectionModel");
export interface WorkflowConnectionModel extends Entities.ModelEntity {
  Type: "WorkflowConnectionModel";
  mainEntityType: Basics.TypeEntity;
  name: string | null;
  decisionOptionName: string | null;
  needCondition: boolean;
  needOrder: boolean;
  type: ConnectionType;
  condition: Entities.Lite<WorkflowConditionEntity> | null;
  action: Entities.Lite<WorkflowActionEntity> | null;
  order: number | null;
  decisionOptions: Entities.MList<ButtonOptionEmbedded>;
}

export module WorkflowConnectionOperation {
  export const Save : Entities.ExecuteSymbol<WorkflowConnectionEntity> = registerSymbol("Operation", "WorkflowConnectionOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowConnectionEntity> = registerSymbol("Operation", "WorkflowConnectionOperation.Delete");
}

export const WorkflowEntity = new Type<WorkflowEntity>("Workflow");
export interface WorkflowEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "Workflow";
  name: string;
  mainEntityType: Basics.TypeEntity;
  mainEntityStrategies: Entities.MList<WorkflowMainEntityStrategy>;
  expirationDate: string /*DateTime*/ | null;
  guid: string /*Guid*/;
}

export const WorkflowEventEntity = new Type<WorkflowEventEntity>("WorkflowEvent");
export interface WorkflowEventEntity extends Entities.Entity, IWorkflowNodeEntity, IWorkflowObjectEntity {
  Type: "WorkflowEvent";
  name: string | null;
  bpmnElementId: string;
  lane: WorkflowLaneEntity;
  type: WorkflowEventType;
  runRepeatedly: boolean;
  decisionOptionName: string | null;
  timer: WorkflowTimerEmbedded | null;
  boundaryOf: Entities.Lite<WorkflowActivityEntity> | null;
  xml: WorkflowXmlEmbedded;
}

export const WorkflowEventModel = new Type<WorkflowEventModel>("WorkflowEventModel");
export interface WorkflowEventModel extends Entities.ModelEntity {
  Type: "WorkflowEventModel";
  mainEntityType: Basics.TypeEntity;
  name: string | null;
  type: WorkflowEventType;
  runRepeatedly: boolean;
  decisionOptionName: string | null;
  task: WorkflowEventTaskModel | null;
  timer: WorkflowTimerEmbedded | null;
  bpmnElementId: string;
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
  creationDate: string /*DateTime*/;
  workflowEventTask: Entities.Lite<WorkflowEventTaskEntity> | null;
  result: boolean;
}

export const WorkflowEventTaskEntity = new Type<WorkflowEventTaskEntity>("WorkflowEventTask");
export interface WorkflowEventTaskEntity extends Entities.Entity, Scheduler.ITaskEntity {
  Type: "WorkflowEventTask";
  workflow: Entities.Lite<WorkflowEntity>;
  event: Entities.Lite<WorkflowEventEntity>;
  triggeredOn: TriggeredOn;
  condition: WorkflowEventTaskConditionEval | null;
  action: WorkflowEventTaskActionEval | null;
}

export const WorkflowEventTaskModel = new Type<WorkflowEventTaskModel>("WorkflowEventTaskModel");
export interface WorkflowEventTaskModel extends Entities.ModelEntity {
  Type: "WorkflowEventTaskModel";
  suspended: boolean;
  rule: Scheduler.IScheduleRuleEntity | null;
  triggeredOn: TriggeredOn;
  condition: WorkflowEventTaskConditionEval | null;
  action: WorkflowEventTaskActionEval | null;
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
  lane: WorkflowLaneEntity;
  name: string | null;
  bpmnElementId: string;
  type: WorkflowGatewayType;
  direction: WorkflowGatewayDirection;
  xml: WorkflowXmlEmbedded;
}

export const WorkflowGatewayModel = new Type<WorkflowGatewayModel>("WorkflowGatewayModel");
export interface WorkflowGatewayModel extends Entities.ModelEntity {
  Type: "WorkflowGatewayModel";
  name: string | null;
  type: WorkflowGatewayType;
  direction: WorkflowGatewayDirection;
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
  name: string;
  bpmnElementId: string;
  xml: WorkflowXmlEmbedded;
  pool: WorkflowPoolEntity;
  actors: Entities.MList<Entities.Lite<Entities.Entity>>;
  actorsEval: WorkflowLaneActorsEval | null;
  useActorEvalForStart: boolean;
  combineActorAndActorEvalWhenContinuing: boolean;
}

export const WorkflowLaneModel = new Type<WorkflowLaneModel>("WorkflowLaneModel");
export interface WorkflowLaneModel extends Entities.ModelEntity {
  Type: "WorkflowLaneModel";
  mainEntityType: Basics.TypeEntity;
  name: string;
  actors: Entities.MList<Entities.Lite<Entities.Entity>>;
  actorsEval: WorkflowLaneActorsEval | null;
  useActorEvalForStart: boolean;
  combineActorAndActorEvalWhenContinuing: boolean;
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
  export const _0NotAllowedFor1NoConstructorHasBeenDefinedInWithWorkflow = new MessageKey("WorkflowMessage", "_0NotAllowedFor1NoConstructorHasBeenDefinedInWithWorkflow");
  export const YouAreNotMemberOfAnyLaneContainingAnStartEventInWorkflow0 = new MessageKey("WorkflowMessage", "YouAreNotMemberOfAnyLaneContainingAnStartEventInWorkflow0");
  export const EvaluationOrderOfTheConnectionForIfElse = new MessageKey("WorkflowMessage", "EvaluationOrderOfTheConnectionForIfElse");
}

export const WorkflowModel = new Type<WorkflowModel>("WorkflowModel");
export interface WorkflowModel extends Entities.ModelEntity {
  Type: "WorkflowModel";
  diagramXml: string;
  entities: Entities.MList<BpmnEntityPairEmbedded>;
}

export module WorkflowOperation {
  export const Create : Entities.ConstructSymbol_Simple<WorkflowEntity> = registerSymbol("Operation", "WorkflowOperation.Create");
  export const Clone : Entities.ConstructSymbol_From<WorkflowEntity, WorkflowEntity> = registerSymbol("Operation", "WorkflowOperation.Clone");
  export const Save : Entities.ExecuteSymbol<WorkflowEntity> = registerSymbol("Operation", "WorkflowOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowEntity> = registerSymbol("Operation", "WorkflowOperation.Delete");
  export const Activate : Entities.ExecuteSymbol<WorkflowEntity> = registerSymbol("Operation", "WorkflowOperation.Activate");
  export const Deactivate : Entities.ExecuteSymbol<WorkflowEntity> = registerSymbol("Operation", "WorkflowOperation.Deactivate");
}

export module WorkflowPermission {
  export const ViewWorkflowPanel : Authorization.PermissionSymbol = registerSymbol("Permission", "WorkflowPermission.ViewWorkflowPanel");
  export const ViewCaseFlow : Authorization.PermissionSymbol = registerSymbol("Permission", "WorkflowPermission.ViewCaseFlow");
  export const WorkflowToolbarMenu : Authorization.PermissionSymbol = registerSymbol("Permission", "WorkflowPermission.WorkflowToolbarMenu");
}

export const WorkflowPoolEntity = new Type<WorkflowPoolEntity>("WorkflowPool");
export interface WorkflowPoolEntity extends Entities.Entity, IWorkflowObjectEntity {
  Type: "WorkflowPool";
  workflow: WorkflowEntity;
  name: string;
  bpmnElementId: string;
  xml: WorkflowXmlEmbedded;
}

export const WorkflowPoolModel = new Type<WorkflowPoolModel>("WorkflowPoolModel");
export interface WorkflowPoolModel extends Entities.ModelEntity {
  Type: "WorkflowPoolModel";
  name: string;
}

export module WorkflowPoolOperation {
  export const Save : Entities.ExecuteSymbol<WorkflowPoolEntity> = registerSymbol("Operation", "WorkflowPoolOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowPoolEntity> = registerSymbol("Operation", "WorkflowPoolOperation.Delete");
}

export const WorkflowReplacementItemEmbedded = new Type<WorkflowReplacementItemEmbedded>("WorkflowReplacementItemEmbedded");
export interface WorkflowReplacementItemEmbedded extends Entities.EmbeddedEntity {
  Type: "WorkflowReplacementItemEmbedded";
  oldNode: Entities.Lite<IWorkflowNodeEntity>;
  subWorkflow: Entities.Lite<WorkflowEntity> | null;
  newNode: string;
}

export const WorkflowReplacementModel = new Type<WorkflowReplacementModel>("WorkflowReplacementModel");
export interface WorkflowReplacementModel extends Entities.ModelEntity {
  Type: "WorkflowReplacementModel";
  replacements: Entities.MList<WorkflowReplacementItemEmbedded>;
  newTasks: Entities.MList<NewTasksEmbedded>;
}

export const WorkflowScriptEntity = new Type<WorkflowScriptEntity>("WorkflowScript");
export interface WorkflowScriptEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "WorkflowScript";
  guid: string /*Guid*/;
  name: string;
  mainEntityType: Basics.TypeEntity;
  eval: WorkflowScriptEval;
}

export const WorkflowScriptEval = new Type<WorkflowScriptEval>("WorkflowScriptEval");
export interface WorkflowScriptEval extends Dynamic.EvalEmbedded<IWorkflowScriptExecutor> {
  Type: "WorkflowScriptEval";
  customTypes: string | null;
}

export module WorkflowScriptOperation {
  export const Clone : Entities.ConstructSymbol_From<WorkflowScriptEntity, WorkflowScriptEntity> = registerSymbol("Operation", "WorkflowScriptOperation.Clone");
  export const Save : Entities.ExecuteSymbol<WorkflowScriptEntity> = registerSymbol("Operation", "WorkflowScriptOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowScriptEntity> = registerSymbol("Operation", "WorkflowScriptOperation.Delete");
}

export const WorkflowScriptPartEmbedded = new Type<WorkflowScriptPartEmbedded>("WorkflowScriptPartEmbedded");
export interface WorkflowScriptPartEmbedded extends Entities.EmbeddedEntity {
  Type: "WorkflowScriptPartEmbedded";
  script: Entities.Lite<WorkflowScriptEntity>;
  retryStrategy: WorkflowScriptRetryStrategyEntity | null;
}

export const WorkflowScriptRetryStrategyEntity = new Type<WorkflowScriptRetryStrategyEntity>("WorkflowScriptRetryStrategy");
export interface WorkflowScriptRetryStrategyEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "WorkflowScriptRetryStrategy";
  rule: string;
  guid: string /*Guid*/;
}

export module WorkflowScriptRetryStrategyOperation {
  export const Save : Entities.ExecuteSymbol<WorkflowScriptRetryStrategyEntity> = registerSymbol("Operation", "WorkflowScriptRetryStrategyOperation.Save");
  export const Delete : Entities.DeleteSymbol<WorkflowScriptRetryStrategyEntity> = registerSymbol("Operation", "WorkflowScriptRetryStrategyOperation.Delete");
}

export const WorkflowTimerConditionEntity = new Type<WorkflowTimerConditionEntity>("WorkflowTimerCondition");
export interface WorkflowTimerConditionEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "WorkflowTimerCondition";
  name: string;
  guid: string /*Guid*/;
  mainEntityType: Basics.TypeEntity;
  eval: WorkflowTimerConditionEval;
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
  duration: Signum.TimeSpanEmbedded | null;
  condition: Entities.Lite<WorkflowTimerConditionEntity> | null;
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
  export const IntermediateTimer0ShouldHaveName = new MessageKey("WorkflowValidationMessage", "IntermediateTimer0ShouldHaveName");
  export const ParallelSplit0ShouldHaveAtLeastOneConnection = new MessageKey("WorkflowValidationMessage", "ParallelSplit0ShouldHaveAtLeastOneConnection");
  export const ParallelSplit0ShouldHaveOnlyNormalConnectionsWithoutConditions = new MessageKey("WorkflowValidationMessage", "ParallelSplit0ShouldHaveOnlyNormalConnectionsWithoutConditions");
  export const Join0OfType1DoesNotMatchWithItsPairTheSplit2OfType3 = new MessageKey("WorkflowValidationMessage", "Join0OfType1DoesNotMatchWithItsPairTheSplit2OfType3");
  export const DecisionOption0IsDeclaredButNeverUsedInAConnection = new MessageKey("WorkflowValidationMessage", "DecisionOption0IsDeclaredButNeverUsedInAConnection");
  export const DecisionOptionName0IsNotDeclaredInAnyActivity = new MessageKey("WorkflowValidationMessage", "DecisionOptionName0IsNotDeclaredInAnyActivity");
  export const BoundaryTimer0OfActivity1CanNotHave2BecauseActivityIsNot3 = new MessageKey("WorkflowValidationMessage", "BoundaryTimer0OfActivity1CanNotHave2BecauseActivityIsNot3");
  export const BoundaryTimer0OfActivity1ShouldHave2BecauseActivityIs3 = new MessageKey("WorkflowValidationMessage", "BoundaryTimer0OfActivity1ShouldHave2BecauseActivityIs3");
  export const BoundaryTimer0OfActivity1HasInvalid23 = new MessageKey("WorkflowValidationMessage", "BoundaryTimer0OfActivity1HasInvalid23");
}

export const WorkflowXmlEmbedded = new Type<WorkflowXmlEmbedded>("WorkflowXmlEmbedded");
export interface WorkflowXmlEmbedded extends Entities.EmbeddedEntity {
  Type: "WorkflowXmlEmbedded";
  diagramXml: string;
}


