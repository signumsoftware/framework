//////////////////////////////////
//Auto-generated. Do NOT modify!//
//////////////////////////////////

import { MessageKey, QueryKey, Type, EnumType, registerSymbol } from '../../Signum/React/Reflection'
import * as Entities from '../../Signum/React/Signum.Entities'
import * as Security from '../../Signum/React/Signum.Security'
import * as Basics from '../../Signum/React/Signum.Basics'
import * as Operations from '../../Signum/React/Signum.Operations'
import * as Authorization from '../Signum.Authorization/Signum.Authorization'
import * as UserAssets from '../Signum.UserAssets/Signum.UserAssets'
import * as Eval from '../Signum.Eval/Signum.Eval'
import * as Scheduler from '../Signum.Scheduler/Signum.Scheduler'
import * as Processes from '../Signum.Processes/Signum.Processes'

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

export const ActivityWithRemarks: Type<ActivityWithRemarks> = new Type<ActivityWithRemarks>("ActivityWithRemarks");
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

export const BpmnEntityPairEmbedded: Type<BpmnEntityPairEmbedded> = new Type<BpmnEntityPairEmbedded>("BpmnEntityPairEmbedded");
export interface BpmnEntityPairEmbedded extends Entities.EmbeddedEntity {
  Type: "BpmnEntityPairEmbedded";
  model: Entities.ModelEntity;
  bpmnElementId: string;
}

export const ButtonOptionEmbedded: Type<ButtonOptionEmbedded> = new Type<ButtonOptionEmbedded>("ButtonOptionEmbedded");
export interface ButtonOptionEmbedded extends Entities.EmbeddedEntity {
  Type: "ButtonOptionEmbedded";
  name: string;
  style: Basics.BootstrapStyle;
  withConfirmation: boolean;
}

export const CaseActivityEntity: Type<CaseActivityEntity> = new Type<CaseActivityEntity>("CaseActivity");
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

export const CaseActivityExecutedTimerEntity: Type<CaseActivityExecutedTimerEntity> = new Type<CaseActivityExecutedTimerEntity>("CaseActivityExecutedTimer");
export interface CaseActivityExecutedTimerEntity extends Entities.Entity {
  Type: "CaseActivityExecutedTimer";
  creationDate: string /*DateTime*/;
  caseActivity: Entities.Lite<CaseActivityEntity>;
  boundaryEvent: Entities.Lite<WorkflowEventEntity>;
}

export namespace CaseActivityMessage {
  export const CaseContainsOtherActivities: MessageKey = new MessageKey("CaseActivityMessage", "CaseContainsOtherActivities");
  export const NoNextConnectionThatSatisfiesTheConditionsFound: MessageKey = new MessageKey("CaseActivityMessage", "NoNextConnectionThatSatisfiesTheConditionsFound");
  export const CaseIsADecompositionOf0: MessageKey = new MessageKey("CaseActivityMessage", "CaseIsADecompositionOf0");
  export const From0On1: MessageKey = new MessageKey("CaseActivityMessage", "From0On1");
  export const DoneBy0On1: MessageKey = new MessageKey("CaseActivityMessage", "DoneBy0On1");
  export const PersonalRemarksForThisNotification: MessageKey = new MessageKey("CaseActivityMessage", "PersonalRemarksForThisNotification");
  export const TheActivity0RequiresToBeOpened: MessageKey = new MessageKey("CaseActivityMessage", "TheActivity0RequiresToBeOpened");
  export const NoOpenedOrInProgressNotificationsFound: MessageKey = new MessageKey("CaseActivityMessage", "NoOpenedOrInProgressNotificationsFound");
  export const NextActivityAlreadyInProgress: MessageKey = new MessageKey("CaseActivityMessage", "NextActivityAlreadyInProgress");
  export const NextActivityOfDecompositionSurrogateAlreadyInProgress: MessageKey = new MessageKey("CaseActivityMessage", "NextActivityOfDecompositionSurrogateAlreadyInProgress");
  export const Only0CanUndoThisOperation: MessageKey = new MessageKey("CaseActivityMessage", "Only0CanUndoThisOperation");
  export const Activity0HasNoJumps: MessageKey = new MessageKey("CaseActivityMessage", "Activity0HasNoJumps");
  export const Activity0HasNoTimers: MessageKey = new MessageKey("CaseActivityMessage", "Activity0HasNoTimers");
  export const ThereIsNoPreviousActivity: MessageKey = new MessageKey("CaseActivityMessage", "ThereIsNoPreviousActivity");
  export const OnlyForScriptWorkflowActivities: MessageKey = new MessageKey("CaseActivityMessage", "OnlyForScriptWorkflowActivities");
  export const Pending: MessageKey = new MessageKey("CaseActivityMessage", "Pending");
  export const NoWorkflowActivity: MessageKey = new MessageKey("CaseActivityMessage", "NoWorkflowActivity");
  export const ImpossibleToDeleteCaseActivity0OnWorkflowActivity1BecauseHasNoPreviousActivity: MessageKey = new MessageKey("CaseActivityMessage", "ImpossibleToDeleteCaseActivity0OnWorkflowActivity1BecauseHasNoPreviousActivity");
  export const LastCaseActivity: MessageKey = new MessageKey("CaseActivityMessage", "LastCaseActivity");
  export const CurrentUserHasNotification: MessageKey = new MessageKey("CaseActivityMessage", "CurrentUserHasNotification");
  export const NoNewOrOpenedOrInProgressNotificationsFound: MessageKey = new MessageKey("CaseActivityMessage", "NoNewOrOpenedOrInProgressNotificationsFound");
  export const NoActorsFoundToInsertCaseActivityNotifications: MessageKey = new MessageKey("CaseActivityMessage", "NoActorsFoundToInsertCaseActivityNotifications");
  export const ThereAreInprogressActivities: MessageKey = new MessageKey("CaseActivityMessage", "ThereAreInprogressActivities");
  export const ShowHelp: MessageKey = new MessageKey("CaseActivityMessage", "ShowHelp");
  export const HideHelp: MessageKey = new MessageKey("CaseActivityMessage", "HideHelp");
  export const CanceledCase: MessageKey = new MessageKey("CaseActivityMessage", "CanceledCase");
  export const AlreadyFinished: MessageKey = new MessageKey("CaseActivityMessage", "AlreadyFinished");
  export const NotCanceled: MessageKey = new MessageKey("CaseActivityMessage", "NotCanceled");
}

export const CaseActivityMixin: Type<CaseActivityMixin> = new Type<CaseActivityMixin>("CaseActivityMixin");
export interface CaseActivityMixin extends Entities.MixinEntity {
  Type: "CaseActivityMixin";
  caseActivity: Entities.Lite<CaseActivityEntity> | null;
}

export namespace CaseActivityOperation {
  export const CreateCaseActivityFromWorkflow : Operations.ConstructSymbol_From<CaseActivityEntity, WorkflowEntity> = registerSymbol("Operation", "CaseActivityOperation.CreateCaseActivityFromWorkflow");
  export const CreateCaseFromWorkflowEventTask : Operations.ConstructSymbol_From<CaseEntity, WorkflowEventTaskEntity> = registerSymbol("Operation", "CaseActivityOperation.CreateCaseFromWorkflowEventTask");
  export const Register : Operations.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Register");
  export const Delete : Operations.DeleteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Delete");
  export const Next : Operations.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Next");
  export const Jump : Operations.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Jump");
  export const FreeJump : Operations.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.FreeJump");
  export const Timer : Operations.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Timer");
  export const MarkAsUnread : Operations.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.MarkAsUnread");
  export const Undo : Operations.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.Undo");
  export const ScriptExecute : Operations.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.ScriptExecute");
  export const ScriptScheduleRetry : Operations.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.ScriptScheduleRetry");
  export const ScriptFailureJump : Operations.ExecuteSymbol<CaseActivityEntity> = registerSymbol("Operation", "CaseActivityOperation.ScriptFailureJump");
}

export namespace CaseActivityProcessAlgorithm {
  export const Timeout : Processes.ProcessAlgorithmSymbol = registerSymbol("ProcessAlgorithm", "CaseActivityProcessAlgorithm.Timeout");
}

export namespace CaseActivityQuery {
  export const Inbox: QueryKey = new QueryKey("CaseActivityQuery", "Inbox");
}

export namespace CaseActivityTask {
  export const Timeout : Scheduler.SimpleTaskSymbol = registerSymbol("SimpleTask", "CaseActivityTask.Timeout");
}

export const CaseEntity: Type<CaseEntity> = new Type<CaseEntity>("Case");
export interface CaseEntity extends Entities.Entity {
  Type: "Case";
  workflow: WorkflowEntity;
  parentCase: Entities.Lite<CaseEntity> | null;
  description: string;
  mainEntity: ICaseMainEntity;
  startDate: string /*DateTime*/;
  finishDate: string /*DateTime*/ | null;
}

export const CaseFlowColor: EnumType<CaseFlowColor> = new EnumType<CaseFlowColor>("CaseFlowColor");
export type CaseFlowColor =
  "CaseMaxDuration" |
  "AverageDuration" |
  "EstimatedDuration";

export const CaseJunctionEntity: Type<CaseJunctionEntity> = new Type<CaseJunctionEntity>("CaseJunction");
export interface CaseJunctionEntity extends Entities.Entity {
  Type: "CaseJunction";
  direction: WorkflowGatewayDirection;
  from: Entities.Lite<CaseActivityEntity>;
  to: Entities.Lite<CaseActivityEntity>;
}

export namespace CaseMessage {
  export const DeleteMainEntity: MessageKey = new MessageKey("CaseMessage", "DeleteMainEntity");
  export const DoYouWAntToAlsoDeleteTheMainEntity0: MessageKey = new MessageKey("CaseMessage", "DoYouWAntToAlsoDeleteTheMainEntity0");
  export const DoYouWAntToAlsoDeleteTheMainEntities: MessageKey = new MessageKey("CaseMessage", "DoYouWAntToAlsoDeleteTheMainEntities");
  export const SetTags: MessageKey = new MessageKey("CaseMessage", "SetTags");
}

export const CaseNotificationEntity: Type<CaseNotificationEntity> = new Type<CaseNotificationEntity>("CaseNotification");
export interface CaseNotificationEntity extends Entities.Entity {
  Type: "CaseNotification";
  caseActivity: Entities.Lite<CaseActivityEntity>;
  user: Entities.Lite<Authorization.UserEntity>;
  actor: Entities.Lite<Entities.Entity>;
  remarks: string | null;
  state: CaseNotificationState;
}

export namespace CaseNotificationOperation {
  export const SetRemarks : Operations.ExecuteSymbol<CaseNotificationEntity> = registerSymbol("Operation", "CaseNotificationOperation.SetRemarks");
  export const Delete : Operations.DeleteSymbol<CaseNotificationEntity> = registerSymbol("Operation", "CaseNotificationOperation.Delete");
  export const CreateCaseNotificationFromCaseActivity : Operations.ConstructSymbol_From<CaseNotificationEntity, CaseActivityEntity> = registerSymbol("Operation", "CaseNotificationOperation.CreateCaseNotificationFromCaseActivity");
}

export const CaseNotificationState: EnumType<CaseNotificationState> = new EnumType<CaseNotificationState>("CaseNotificationState");
export type CaseNotificationState =
  "New" |
  "Opened" |
  "InProgress" |
  "Done" |
  "DoneByOther";

export namespace CaseOperation {
  export const SetTags : Operations.ExecuteSymbol<CaseEntity> = registerSymbol("Operation", "CaseOperation.SetTags");
  export const Cancel : Operations.ExecuteSymbol<CaseEntity> = registerSymbol("Operation", "CaseOperation.Cancel");
  export const Reactivate : Operations.ExecuteSymbol<CaseEntity> = registerSymbol("Operation", "CaseOperation.Reactivate");
  export const Delete : Operations.DeleteSymbol<CaseEntity> = registerSymbol("Operation", "CaseOperation.Delete");
}

export const CaseTagEntity: Type<CaseTagEntity> = new Type<CaseTagEntity>("CaseTag");
export interface CaseTagEntity extends Entities.Entity {
  Type: "CaseTag";
  creationDate: string /*DateTime*/;
  case: Entities.Lite<CaseEntity>;
  tagType: CaseTagTypeEntity;
  createdBy: Entities.Lite<Security.IUserEntity>;
}

export const CaseTagsModel: Type<CaseTagsModel> = new Type<CaseTagsModel>("CaseTagsModel");
export interface CaseTagsModel extends Entities.ModelEntity {
  Type: "CaseTagsModel";
  caseTags: Entities.MList<CaseTagTypeEntity>;
  oldCaseTags: Entities.MList<CaseTagTypeEntity>;
}

export const CaseTagTypeEntity: Type<CaseTagTypeEntity> = new Type<CaseTagTypeEntity>("CaseTagType");
export interface CaseTagTypeEntity extends Entities.Entity {
  Type: "CaseTagType";
  name: string;
  color: string;
}

export namespace CaseTagTypeOperation {
  export const Save : Operations.ExecuteSymbol<CaseTagTypeEntity> = registerSymbol("Operation", "CaseTagTypeOperation.Save");
}

export const ConnectionType: EnumType<ConnectionType> = new EnumType<ConnectionType>("ConnectionType");
export type ConnectionType =
  "Normal" |
  "Decision" |
  "Jump" |
  "ScriptException";

export const DateFilterRange: EnumType<DateFilterRange> = new EnumType<DateFilterRange>("DateFilterRange");
export type DateFilterRange =
  "All" |
  "LastWeek" |
  "LastMonth" |
  "CurrentYear";

export const DoneType: EnumType<DoneType> = new EnumType<DoneType>("DoneType");
export type DoneType =
  "Next" |
  "Jump" |
  "Timeout" |
  "ScriptSuccess" |
  "ScriptFailure" |
  "Recompose";

export interface ICaseMainEntity extends Entities.Entity {
}

export const InboxFilterModel: Type<InboxFilterModel> = new Type<InboxFilterModel>("InboxFilterModel");
export interface InboxFilterModel extends Entities.ModelEntity {
  Type: "InboxFilterModel";
  range: DateFilterRange;
  states: Entities.MList<CaseNotificationState>;
  fromDate: string /*DateTime*/ | null;
  toDate: string /*DateTime*/ | null;
}

export namespace InboxMessage {
  export const Clear: MessageKey = new MessageKey("InboxMessage", "Clear");
  export const Activity: MessageKey = new MessageKey("InboxMessage", "Activity");
  export const SenderNote: MessageKey = new MessageKey("InboxMessage", "SenderNote");
  export const Sender: MessageKey = new MessageKey("InboxMessage", "Sender");
  export const Filters: MessageKey = new MessageKey("InboxMessage", "Filters");
}

export interface IWorkflowNodeEntity extends IWorkflowObjectEntity, Entities.Entity {
  lane: WorkflowLaneEntity;
}

export interface IWorkflowObjectEntity extends Entities.Entity {
  xml: WorkflowXmlEmbedded;
  bpmnElementId: string;
}

export const NewTasksEmbedded: Type<NewTasksEmbedded> = new Type<NewTasksEmbedded>("NewTasksEmbedded");
export interface NewTasksEmbedded extends Entities.EmbeddedEntity {
  Type: "NewTasksEmbedded";
  bpmnId: string;
  name: string | null;
  subWorkflow: Entities.Lite<WorkflowEntity> | null;
}

export const ScriptExecutionEmbedded: Type<ScriptExecutionEmbedded> = new Type<ScriptExecutionEmbedded>("ScriptExecutionEmbedded");
export interface ScriptExecutionEmbedded extends Entities.EmbeddedEntity {
  Type: "ScriptExecutionEmbedded";
  nextExecution: string /*DateTime*/;
  retryCount: number;
  processIdentifier: string /*Guid*/ | null;
}

export const SubEntitiesEval: Type<SubEntitiesEval> = new Type<SubEntitiesEval>("SubEntitiesEval");
export interface SubEntitiesEval extends Eval.EvalEmbedded<ISubEntitiesEvaluator> {
  Type: "SubEntitiesEval";
}

export const SubWorkflowEmbedded: Type<SubWorkflowEmbedded> = new Type<SubWorkflowEmbedded>("SubWorkflowEmbedded");
export interface SubWorkflowEmbedded extends Entities.EmbeddedEntity {
  Type: "SubWorkflowEmbedded";
  workflow: WorkflowEntity;
  subEntitiesEval: SubEntitiesEval;
}

export const TimeSpanEmbedded: Type<TimeSpanEmbedded> = new Type<TimeSpanEmbedded>("TimeSpanEmbedded");
export interface TimeSpanEmbedded extends Entities.EmbeddedEntity {
  Type: "TimeSpanEmbedded";
  days: number;
  hours: number;
  minutes: number;
  seconds: number;
}

export const TriggeredOn: EnumType<TriggeredOn> = new EnumType<TriggeredOn>("TriggeredOn");
export type TriggeredOn =
  "Always" |
  "ConditionIsTrue" |
  "ConditionChangesToTrue";

export const ViewNamePropEmbedded: Type<ViewNamePropEmbedded> = new Type<ViewNamePropEmbedded>("ViewNamePropEmbedded");
export interface ViewNamePropEmbedded extends Entities.EmbeddedEntity {
  Type: "ViewNamePropEmbedded";
  name: string;
  expression: string | null;
}

export const WorkflowActionEntity: Type<WorkflowActionEntity> = new Type<WorkflowActionEntity>("WorkflowAction");
export interface WorkflowActionEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "WorkflowAction";
  name: string;
  guid: string /*Guid*/;
  mainEntityType: Basics.TypeEntity;
  eval: WorkflowActionEval;
}

export const WorkflowActionEval: Type<WorkflowActionEval> = new Type<WorkflowActionEval>("WorkflowActionEval");
export interface WorkflowActionEval extends Eval.EvalEmbedded<IWorkflowActionExecutor> {
  Type: "WorkflowActionEval";
}

export namespace WorkflowActionOperation {
  export const Clone : Operations.ConstructSymbol_From<WorkflowActionEntity, WorkflowActionEntity> = registerSymbol("Operation", "WorkflowActionOperation.Clone");
  export const Save : Operations.ExecuteSymbol<WorkflowActionEntity> = registerSymbol("Operation", "WorkflowActionOperation.Save");
  export const Delete : Operations.DeleteSymbol<WorkflowActionEntity> = registerSymbol("Operation", "WorkflowActionOperation.Delete");
}

export const WorkflowActivityEntity: Type<WorkflowActivityEntity> = new Type<WorkflowActivityEntity>("WorkflowActivity");
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

export namespace WorkflowActivityMessage {
  export const DuplicateViewNameFound0: MessageKey = new MessageKey("WorkflowActivityMessage", "DuplicateViewNameFound0");
  export const ChooseADestinationForWorkflowJumping: MessageKey = new MessageKey("WorkflowActivityMessage", "ChooseADestinationForWorkflowJumping");
  export const CaseFlow: MessageKey = new MessageKey("WorkflowActivityMessage", "CaseFlow");
  export const AverageDuration: MessageKey = new MessageKey("WorkflowActivityMessage", "AverageDuration");
  export const ActivityIs: MessageKey = new MessageKey("WorkflowActivityMessage", "ActivityIs");
  export const NoActiveTimerFound: MessageKey = new MessageKey("WorkflowActivityMessage", "NoActiveTimerFound");
  export const InprogressCaseActivities: MessageKey = new MessageKey("WorkflowActivityMessage", "InprogressCaseActivities");
  export const OpenCaseActivityStats: MessageKey = new MessageKey("WorkflowActivityMessage", "OpenCaseActivityStats");
  export const LocateWorkflowActivityInDiagram: MessageKey = new MessageKey("WorkflowActivityMessage", "LocateWorkflowActivityInDiagram");
  export const Approve: MessageKey = new MessageKey("WorkflowActivityMessage", "Approve");
  export const Decline: MessageKey = new MessageKey("WorkflowActivityMessage", "Decline");
  export const Confirmation: MessageKey = new MessageKey("WorkflowActivityMessage", "Confirmation");
  export const AreYouSureYouWantToExecute0: MessageKey = new MessageKey("WorkflowActivityMessage", "AreYouSureYouWantToExecute0");
}

export const WorkflowActivityModel: Type<WorkflowActivityModel> = new Type<WorkflowActivityModel>("WorkflowActivityModel");
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

export namespace WorkflowActivityMonitorMessage {
  export const WorkflowActivityMonitor: MessageKey = new MessageKey("WorkflowActivityMonitorMessage", "WorkflowActivityMonitor");
  export const Draw: MessageKey = new MessageKey("WorkflowActivityMonitorMessage", "Draw");
  export const ResetZoom: MessageKey = new MessageKey("WorkflowActivityMonitorMessage", "ResetZoom");
  export const Find: MessageKey = new MessageKey("WorkflowActivityMonitorMessage", "Find");
  export const Filters: MessageKey = new MessageKey("WorkflowActivityMonitorMessage", "Filters");
  export const Columns: MessageKey = new MessageKey("WorkflowActivityMonitorMessage", "Columns");
  export const OpenWorkflow: MessageKey = new MessageKey("WorkflowActivityMonitorMessage", "OpenWorkflow");
}

export namespace WorkflowActivityOperation {
  export const Save : Operations.ExecuteSymbol<WorkflowActivityEntity> = registerSymbol("Operation", "WorkflowActivityOperation.Save");
  export const Delete : Operations.DeleteSymbol<WorkflowActivityEntity> = registerSymbol("Operation", "WorkflowActivityOperation.Delete");
}

export const WorkflowActivityType: EnumType<WorkflowActivityType> = new EnumType<WorkflowActivityType>("WorkflowActivityType");
export type WorkflowActivityType =
  "Task" |
  "Decision" |
  "DecompositionWorkflow" |
  "CallWorkflow" |
  "Script";

export const WorkflowConditionEntity: Type<WorkflowConditionEntity> = new Type<WorkflowConditionEntity>("WorkflowCondition");
export interface WorkflowConditionEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "WorkflowCondition";
  name: string;
  guid: string /*Guid*/;
  mainEntityType: Basics.TypeEntity;
  eval: WorkflowConditionEval;
}

export const WorkflowConditionEval: Type<WorkflowConditionEval> = new Type<WorkflowConditionEval>("WorkflowConditionEval");
export interface WorkflowConditionEval extends Eval.EvalEmbedded<IWorkflowConditionEvaluator> {
  Type: "WorkflowConditionEval";
}

export namespace WorkflowConditionOperation {
  export const Clone : Operations.ConstructSymbol_From<WorkflowConditionEntity, WorkflowConditionEntity> = registerSymbol("Operation", "WorkflowConditionOperation.Clone");
  export const Save : Operations.ExecuteSymbol<WorkflowConditionEntity> = registerSymbol("Operation", "WorkflowConditionOperation.Save");
  export const Delete : Operations.DeleteSymbol<WorkflowConditionEntity> = registerSymbol("Operation", "WorkflowConditionOperation.Delete");
}

export const WorkflowConfigurationEmbedded: Type<WorkflowConfigurationEmbedded> = new Type<WorkflowConfigurationEmbedded>("WorkflowConfigurationEmbedded");
export interface WorkflowConfigurationEmbedded extends Entities.EmbeddedEntity {
  Type: "WorkflowConfigurationEmbedded";
  scriptRunnerPeriod: number;
  avoidExecutingScriptsOlderThan: number | null;
  chunkSizeRunningScripts: number;
}

export const WorkflowConnectionEntity: Type<WorkflowConnectionEntity> = new Type<WorkflowConnectionEntity>("WorkflowConnection");
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

export const WorkflowConnectionModel: Type<WorkflowConnectionModel> = new Type<WorkflowConnectionModel>("WorkflowConnectionModel");
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

export namespace WorkflowConnectionOperation {
  export const Save : Operations.ExecuteSymbol<WorkflowConnectionEntity> = registerSymbol("Operation", "WorkflowConnectionOperation.Save");
  export const Delete : Operations.DeleteSymbol<WorkflowConnectionEntity> = registerSymbol("Operation", "WorkflowConnectionOperation.Delete");
}

export const WorkflowEntity: Type<WorkflowEntity> = new Type<WorkflowEntity>("Workflow");
export interface WorkflowEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "Workflow";
  name: string;
  mainEntityType: Basics.TypeEntity;
  mainEntityStrategies: Entities.MList<WorkflowMainEntityStrategy>;
  expirationDate: string /*DateTime*/ | null;
  guid: string /*Guid*/;
}

export const WorkflowEventEntity: Type<WorkflowEventEntity> = new Type<WorkflowEventEntity>("WorkflowEvent");
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

export const WorkflowEventModel: Type<WorkflowEventModel> = new Type<WorkflowEventModel>("WorkflowEventModel");
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

export namespace WorkflowEventOperation {
  export const Save : Operations.ExecuteSymbol<WorkflowEventEntity> = registerSymbol("Operation", "WorkflowEventOperation.Save");
  export const Delete : Operations.DeleteSymbol<WorkflowEventEntity> = registerSymbol("Operation", "WorkflowEventOperation.Delete");
}

export const WorkflowEventTaskActionEval: Type<WorkflowEventTaskActionEval> = new Type<WorkflowEventTaskActionEval>("WorkflowEventTaskActionEval");
export interface WorkflowEventTaskActionEval extends Eval.EvalEmbedded<IWorkflowEventTaskActionEval> {
  Type: "WorkflowEventTaskActionEval";
}

export const WorkflowEventTaskConditionEval: Type<WorkflowEventTaskConditionEval> = new Type<WorkflowEventTaskConditionEval>("WorkflowEventTaskConditionEval");
export interface WorkflowEventTaskConditionEval extends Eval.EvalEmbedded<IWorkflowEventTaskConditionEvaluator> {
  Type: "WorkflowEventTaskConditionEval";
}

export const WorkflowEventTaskConditionResultEntity: Type<WorkflowEventTaskConditionResultEntity> = new Type<WorkflowEventTaskConditionResultEntity>("WorkflowEventTaskConditionResult");
export interface WorkflowEventTaskConditionResultEntity extends Entities.Entity {
  Type: "WorkflowEventTaskConditionResult";
  creationDate: string /*DateTime*/;
  workflowEventTask: Entities.Lite<WorkflowEventTaskEntity> | null;
  result: boolean;
}

export const WorkflowEventTaskEntity: Type<WorkflowEventTaskEntity> = new Type<WorkflowEventTaskEntity>("WorkflowEventTask");
export interface WorkflowEventTaskEntity extends Entities.Entity, Scheduler.ITaskEntity {
  Type: "WorkflowEventTask";
  workflow: Entities.Lite<WorkflowEntity>;
  event: Entities.Lite<WorkflowEventEntity>;
  triggeredOn: TriggeredOn;
  condition: WorkflowEventTaskConditionEval | null;
  action: WorkflowEventTaskActionEval | null;
}

export const WorkflowEventTaskModel: Type<WorkflowEventTaskModel> = new Type<WorkflowEventTaskModel>("WorkflowEventTaskModel");
export interface WorkflowEventTaskModel extends Entities.ModelEntity {
  Type: "WorkflowEventTaskModel";
  suspended: boolean;
  rule: Scheduler.IScheduleRuleEntity | null;
  triggeredOn: TriggeredOn;
  condition: WorkflowEventTaskConditionEval | null;
  action: WorkflowEventTaskActionEval | null;
}

export namespace WorkflowEventTaskOperation {
  export const Save : Operations.ExecuteSymbol<WorkflowEventTaskEntity> = registerSymbol("Operation", "WorkflowEventTaskOperation.Save");
  export const Delete : Operations.DeleteSymbol<WorkflowEventTaskEntity> = registerSymbol("Operation", "WorkflowEventTaskOperation.Delete");
}

export const WorkflowEventType: EnumType<WorkflowEventType> = new EnumType<WorkflowEventType>("WorkflowEventType");
export type WorkflowEventType =
  "Start" |
  "ScheduledStart" |
  "Finish" |
  "BoundaryForkTimer" |
  "BoundaryInterruptingTimer" |
  "IntermediateTimer";

export const WorkflowGatewayDirection: EnumType<WorkflowGatewayDirection> = new EnumType<WorkflowGatewayDirection>("WorkflowGatewayDirection");
export type WorkflowGatewayDirection =
  "Split" |
  "Join";

export const WorkflowGatewayEntity: Type<WorkflowGatewayEntity> = new Type<WorkflowGatewayEntity>("WorkflowGateway");
export interface WorkflowGatewayEntity extends Entities.Entity, IWorkflowNodeEntity, IWorkflowObjectEntity {
  Type: "WorkflowGateway";
  lane: WorkflowLaneEntity;
  name: string | null;
  bpmnElementId: string;
  type: WorkflowGatewayType;
  direction: WorkflowGatewayDirection;
  xml: WorkflowXmlEmbedded;
}

export const WorkflowGatewayModel: Type<WorkflowGatewayModel> = new Type<WorkflowGatewayModel>("WorkflowGatewayModel");
export interface WorkflowGatewayModel extends Entities.ModelEntity {
  Type: "WorkflowGatewayModel";
  name: string | null;
  type: WorkflowGatewayType;
  direction: WorkflowGatewayDirection;
}

export namespace WorkflowGatewayOperation {
  export const Save : Operations.ExecuteSymbol<WorkflowGatewayEntity> = registerSymbol("Operation", "WorkflowGatewayOperation.Save");
  export const Delete : Operations.DeleteSymbol<WorkflowGatewayEntity> = registerSymbol("Operation", "WorkflowGatewayOperation.Delete");
}

export const WorkflowGatewayType: EnumType<WorkflowGatewayType> = new EnumType<WorkflowGatewayType>("WorkflowGatewayType");
export type WorkflowGatewayType =
  "Exclusive" |
  "Inclusive" |
  "Parallel";

export const WorkflowIssueType: EnumType<WorkflowIssueType> = new EnumType<WorkflowIssueType>("WorkflowIssueType");
export type WorkflowIssueType =
  "Warning" |
  "Error";

export const WorkflowLaneActorsEval: Type<WorkflowLaneActorsEval> = new Type<WorkflowLaneActorsEval>("WorkflowLaneActorsEval");
export interface WorkflowLaneActorsEval extends Eval.EvalEmbedded<IWorkflowLaneActorsEvaluator> {
  Type: "WorkflowLaneActorsEval";
}

export const WorkflowLaneEntity: Type<WorkflowLaneEntity> = new Type<WorkflowLaneEntity>("WorkflowLane");
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

export const WorkflowLaneModel: Type<WorkflowLaneModel> = new Type<WorkflowLaneModel>("WorkflowLaneModel");
export interface WorkflowLaneModel extends Entities.ModelEntity {
  Type: "WorkflowLaneModel";
  mainEntityType: Basics.TypeEntity;
  name: string;
  actors: Entities.MList<Entities.Lite<Entities.Entity>>;
  actorsEval: WorkflowLaneActorsEval | null;
  useActorEvalForStart: boolean;
  combineActorAndActorEvalWhenContinuing: boolean;
}

export namespace WorkflowLaneOperation {
  export const Save : Operations.ExecuteSymbol<WorkflowLaneEntity> = registerSymbol("Operation", "WorkflowLaneOperation.Save");
  export const Delete : Operations.DeleteSymbol<WorkflowLaneEntity> = registerSymbol("Operation", "WorkflowLaneOperation.Delete");
}

export const WorkflowMainEntityStrategy: EnumType<WorkflowMainEntityStrategy> = new EnumType<WorkflowMainEntityStrategy>("WorkflowMainEntityStrategy");
export type WorkflowMainEntityStrategy =
  "CreateNew" |
  "SelectByUser" |
  "Clone";

export namespace WorkflowMessage {
  export const _0BelongsToADifferentWorkflow: MessageKey = new MessageKey("WorkflowMessage", "_0BelongsToADifferentWorkflow");
  export const Condition0IsDefinedFor1Not2: MessageKey = new MessageKey("WorkflowMessage", "Condition0IsDefinedFor1Not2");
  export const JumpsToSameActivityNotAllowed: MessageKey = new MessageKey("WorkflowMessage", "JumpsToSameActivityNotAllowed");
  export const JumpTo0FailedBecause1: MessageKey = new MessageKey("WorkflowMessage", "JumpTo0FailedBecause1");
  export const ToUse0YouSouldSaveWorkflow: MessageKey = new MessageKey("WorkflowMessage", "ToUse0YouSouldSaveWorkflow");
  export const ToUseNewNodesOnJumpsYouSouldSaveWorkflow: MessageKey = new MessageKey("WorkflowMessage", "ToUseNewNodesOnJumpsYouSouldSaveWorkflow");
  export const ToUse0YouSouldSetTheWorkflow1: MessageKey = new MessageKey("WorkflowMessage", "ToUse0YouSouldSetTheWorkflow1");
  export const ChangeWorkflowMainEntityTypeIsNotAllowedBecauseWeHaveNodesThatUseIt: MessageKey = new MessageKey("WorkflowMessage", "ChangeWorkflowMainEntityTypeIsNotAllowedBecauseWeHaveNodesThatUseIt");
  export const WorkflowUsedIn0ForDecompositionOrCallWorkflow: MessageKey = new MessageKey("WorkflowMessage", "WorkflowUsedIn0ForDecompositionOrCallWorkflow");
  export const Workflow0AlreadyActivated: MessageKey = new MessageKey("WorkflowMessage", "Workflow0AlreadyActivated");
  export const Workflow0HasExpiredOn1: MessageKey = new MessageKey("WorkflowMessage", "Workflow0HasExpiredOn1");
  export const HasExpired: MessageKey = new MessageKey("WorkflowMessage", "HasExpired");
  export const DeactivateWorkflow: MessageKey = new MessageKey("WorkflowMessage", "DeactivateWorkflow");
  export const PleaseChooseExpirationDate: MessageKey = new MessageKey("WorkflowMessage", "PleaseChooseExpirationDate");
  export const ResetZoom: MessageKey = new MessageKey("WorkflowMessage", "ResetZoom");
  export const Color: MessageKey = new MessageKey("WorkflowMessage", "Color");
  export const WorkflowIssues: MessageKey = new MessageKey("WorkflowMessage", "WorkflowIssues");
  export const WorkflowProperties: MessageKey = new MessageKey("WorkflowMessage", "WorkflowProperties");
  export const _0NotAllowedFor1NoConstructorHasBeenDefinedInWithWorkflow: MessageKey = new MessageKey("WorkflowMessage", "_0NotAllowedFor1NoConstructorHasBeenDefinedInWithWorkflow");
  export const YouAreNotMemberOfAnyLaneContainingAnStartEventInWorkflow0: MessageKey = new MessageKey("WorkflowMessage", "YouAreNotMemberOfAnyLaneContainingAnStartEventInWorkflow0");
  export const EvaluationOrderOfTheConnectionForIfElse: MessageKey = new MessageKey("WorkflowMessage", "EvaluationOrderOfTheConnectionForIfElse");
  export const SaveAsSVG: MessageKey = new MessageKey("WorkflowMessage", "SaveAsSVG");
}

export const WorkflowModel: Type<WorkflowModel> = new Type<WorkflowModel>("WorkflowModel");
export interface WorkflowModel extends Entities.ModelEntity {
  Type: "WorkflowModel";
  diagramXml: string;
  entities: Entities.MList<BpmnEntityPairEmbedded>;
}

export namespace WorkflowOperation {
  export const Create : Operations.ConstructSymbol_Simple<WorkflowEntity> = registerSymbol("Operation", "WorkflowOperation.Create");
  export const Clone : Operations.ConstructSymbol_From<WorkflowEntity, WorkflowEntity> = registerSymbol("Operation", "WorkflowOperation.Clone");
  export const Save : Operations.ExecuteSymbol<WorkflowEntity> = registerSymbol("Operation", "WorkflowOperation.Save");
  export const Delete : Operations.DeleteSymbol<WorkflowEntity> = registerSymbol("Operation", "WorkflowOperation.Delete");
  export const Activate : Operations.ExecuteSymbol<WorkflowEntity> = registerSymbol("Operation", "WorkflowOperation.Activate");
  export const Deactivate : Operations.ExecuteSymbol<WorkflowEntity> = registerSymbol("Operation", "WorkflowOperation.Deactivate");
}

export namespace WorkflowPermission {
  export const ViewWorkflowPanel : Basics.PermissionSymbol = registerSymbol("Permission", "WorkflowPermission.ViewWorkflowPanel");
  export const ViewCaseFlow : Basics.PermissionSymbol = registerSymbol("Permission", "WorkflowPermission.ViewCaseFlow");
  export const WorkflowToolbarMenu : Basics.PermissionSymbol = registerSymbol("Permission", "WorkflowPermission.WorkflowToolbarMenu");
}

export const WorkflowPoolEntity: Type<WorkflowPoolEntity> = new Type<WorkflowPoolEntity>("WorkflowPool");
export interface WorkflowPoolEntity extends Entities.Entity, IWorkflowObjectEntity {
  Type: "WorkflowPool";
  workflow: WorkflowEntity;
  name: string;
  bpmnElementId: string;
  xml: WorkflowXmlEmbedded;
}

export const WorkflowPoolModel: Type<WorkflowPoolModel> = new Type<WorkflowPoolModel>("WorkflowPoolModel");
export interface WorkflowPoolModel extends Entities.ModelEntity {
  Type: "WorkflowPoolModel";
  name: string;
}

export namespace WorkflowPoolOperation {
  export const Save : Operations.ExecuteSymbol<WorkflowPoolEntity> = registerSymbol("Operation", "WorkflowPoolOperation.Save");
  export const Delete : Operations.DeleteSymbol<WorkflowPoolEntity> = registerSymbol("Operation", "WorkflowPoolOperation.Delete");
}

export const WorkflowReplacementItemEmbedded: Type<WorkflowReplacementItemEmbedded> = new Type<WorkflowReplacementItemEmbedded>("WorkflowReplacementItemEmbedded");
export interface WorkflowReplacementItemEmbedded extends Entities.EmbeddedEntity {
  Type: "WorkflowReplacementItemEmbedded";
  oldNode: Entities.Lite<IWorkflowNodeEntity>;
  subWorkflow: Entities.Lite<WorkflowEntity> | null;
  newNode: string;
}

export const WorkflowReplacementModel: Type<WorkflowReplacementModel> = new Type<WorkflowReplacementModel>("WorkflowReplacementModel");
export interface WorkflowReplacementModel extends Entities.ModelEntity {
  Type: "WorkflowReplacementModel";
  replacements: Entities.MList<WorkflowReplacementItemEmbedded>;
  newTasks: Entities.MList<NewTasksEmbedded>;
}

export const WorkflowScriptEntity: Type<WorkflowScriptEntity> = new Type<WorkflowScriptEntity>("WorkflowScript");
export interface WorkflowScriptEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "WorkflowScript";
  guid: string /*Guid*/;
  name: string;
  mainEntityType: Basics.TypeEntity;
  eval: WorkflowScriptEval;
}

export const WorkflowScriptEval: Type<WorkflowScriptEval> = new Type<WorkflowScriptEval>("WorkflowScriptEval");
export interface WorkflowScriptEval extends Eval.EvalEmbedded<IWorkflowScriptExecutor> {
  Type: "WorkflowScriptEval";
  customTypes: string | null;
}

export namespace WorkflowScriptOperation {
  export const Clone : Operations.ConstructSymbol_From<WorkflowScriptEntity, WorkflowScriptEntity> = registerSymbol("Operation", "WorkflowScriptOperation.Clone");
  export const Save : Operations.ExecuteSymbol<WorkflowScriptEntity> = registerSymbol("Operation", "WorkflowScriptOperation.Save");
  export const Delete : Operations.DeleteSymbol<WorkflowScriptEntity> = registerSymbol("Operation", "WorkflowScriptOperation.Delete");
}

export const WorkflowScriptPartEmbedded: Type<WorkflowScriptPartEmbedded> = new Type<WorkflowScriptPartEmbedded>("WorkflowScriptPartEmbedded");
export interface WorkflowScriptPartEmbedded extends Entities.EmbeddedEntity {
  Type: "WorkflowScriptPartEmbedded";
  script: Entities.Lite<WorkflowScriptEntity>;
  retryStrategy: WorkflowScriptRetryStrategyEntity | null;
}

export const WorkflowScriptRetryStrategyEntity: Type<WorkflowScriptRetryStrategyEntity> = new Type<WorkflowScriptRetryStrategyEntity>("WorkflowScriptRetryStrategy");
export interface WorkflowScriptRetryStrategyEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "WorkflowScriptRetryStrategy";
  rule: string;
  guid: string /*Guid*/;
}

export namespace WorkflowScriptRetryStrategyOperation {
  export const Save : Operations.ExecuteSymbol<WorkflowScriptRetryStrategyEntity> = registerSymbol("Operation", "WorkflowScriptRetryStrategyOperation.Save");
  export const Delete : Operations.DeleteSymbol<WorkflowScriptRetryStrategyEntity> = registerSymbol("Operation", "WorkflowScriptRetryStrategyOperation.Delete");
}

export const WorkflowTimerConditionEntity: Type<WorkflowTimerConditionEntity> = new Type<WorkflowTimerConditionEntity>("WorkflowTimerCondition");
export interface WorkflowTimerConditionEntity extends Entities.Entity, UserAssets.IUserAssetEntity {
  Type: "WorkflowTimerCondition";
  name: string;
  guid: string /*Guid*/;
  mainEntityType: Basics.TypeEntity;
  eval: WorkflowTimerConditionEval;
}

export const WorkflowTimerConditionEval: Type<WorkflowTimerConditionEval> = new Type<WorkflowTimerConditionEval>("WorkflowTimerConditionEval");
export interface WorkflowTimerConditionEval extends Eval.EvalEmbedded<IWorkflowTimerConditionEvaluator> {
  Type: "WorkflowTimerConditionEval";
}

export namespace WorkflowTimerConditionOperation {
  export const Clone : Operations.ConstructSymbol_From<WorkflowTimerConditionEntity, WorkflowTimerConditionEntity> = registerSymbol("Operation", "WorkflowTimerConditionOperation.Clone");
  export const Save : Operations.ExecuteSymbol<WorkflowTimerConditionEntity> = registerSymbol("Operation", "WorkflowTimerConditionOperation.Save");
  export const Delete : Operations.DeleteSymbol<WorkflowTimerConditionEntity> = registerSymbol("Operation", "WorkflowTimerConditionOperation.Delete");
}

export const WorkflowTimerEmbedded: Type<WorkflowTimerEmbedded> = new Type<WorkflowTimerEmbedded>("WorkflowTimerEmbedded");
export interface WorkflowTimerEmbedded extends Entities.EmbeddedEntity {
  Type: "WorkflowTimerEmbedded";
  duration: TimeSpanEmbedded | null;
  avoidExecuteConditionByTimer: boolean;
  condition: Entities.Lite<WorkflowTimerConditionEntity> | null;
}

export namespace WorkflowValidationMessage {
  export const NodeType0WithId1IsInvalid: MessageKey = new MessageKey("WorkflowValidationMessage", "NodeType0WithId1IsInvalid");
  export const ParticipantsAndProcessesAreNotSynchronized: MessageKey = new MessageKey("WorkflowValidationMessage", "ParticipantsAndProcessesAreNotSynchronized");
  export const MultipleStartEventsAreNotAllowed: MessageKey = new MessageKey("WorkflowValidationMessage", "MultipleStartEventsAreNotAllowed");
  export const SomeStartEventIsRequired: MessageKey = new MessageKey("WorkflowValidationMessage", "SomeStartEventIsRequired");
  export const NormalStartEventIsRequiredWhenThe0Are1Or2: MessageKey = new MessageKey("WorkflowValidationMessage", "NormalStartEventIsRequiredWhenThe0Are1Or2");
  export const TheFollowingTasksAreGoingToBeDeleted: MessageKey = new MessageKey("WorkflowValidationMessage", "TheFollowingTasksAreGoingToBeDeleted");
  export const FinishEventIsRequired: MessageKey = new MessageKey("WorkflowValidationMessage", "FinishEventIsRequired");
  export const _0HasInputs: MessageKey = new MessageKey("WorkflowValidationMessage", "_0HasInputs");
  export const _0HasOutputs: MessageKey = new MessageKey("WorkflowValidationMessage", "_0HasOutputs");
  export const _0HasNoInputs: MessageKey = new MessageKey("WorkflowValidationMessage", "_0HasNoInputs");
  export const _0HasNoOutputs: MessageKey = new MessageKey("WorkflowValidationMessage", "_0HasNoOutputs");
  export const _0HasJustOneInputAndOneOutput: MessageKey = new MessageKey("WorkflowValidationMessage", "_0HasJustOneInputAndOneOutput");
  export const _0HasMultipleOutputs: MessageKey = new MessageKey("WorkflowValidationMessage", "_0HasMultipleOutputs");
  export const IsNotInWorkflow: MessageKey = new MessageKey("WorkflowValidationMessage", "IsNotInWorkflow");
  export const Activity0CanNotJumpTo1Because2: MessageKey = new MessageKey("WorkflowValidationMessage", "Activity0CanNotJumpTo1Because2");
  export const Activity0CanNotTimeoutTo1Because2: MessageKey = new MessageKey("WorkflowValidationMessage", "Activity0CanNotTimeoutTo1Because2");
  export const IsStart: MessageKey = new MessageKey("WorkflowValidationMessage", "IsStart");
  export const IsSelfJumping: MessageKey = new MessageKey("WorkflowValidationMessage", "IsSelfJumping");
  export const IsInDifferentParallelTrack: MessageKey = new MessageKey("WorkflowValidationMessage", "IsInDifferentParallelTrack");
  export const _0Track1CanNotBeConnectedTo2Track3InsteadOfTrack4: MessageKey = new MessageKey("WorkflowValidationMessage", "_0Track1CanNotBeConnectedTo2Track3InsteadOfTrack4");
  export const StartEventNextNodeShouldBeAnActivity: MessageKey = new MessageKey("WorkflowValidationMessage", "StartEventNextNodeShouldBeAnActivity");
  export const ParallelGatewaysShouldPair: MessageKey = new MessageKey("WorkflowValidationMessage", "ParallelGatewaysShouldPair");
  export const TimerOrConditionalStartEventsCanNotGoToJoinGateways: MessageKey = new MessageKey("WorkflowValidationMessage", "TimerOrConditionalStartEventsCanNotGoToJoinGateways");
  export const InclusiveGateway0ShouldHaveOneConnectionWithoutCondition: MessageKey = new MessageKey("WorkflowValidationMessage", "InclusiveGateway0ShouldHaveOneConnectionWithoutCondition");
  export const Gateway0ShouldHasConditionOrDecisionOnEachOutputExceptTheLast: MessageKey = new MessageKey("WorkflowValidationMessage", "Gateway0ShouldHasConditionOrDecisionOnEachOutputExceptTheLast");
  export const _0CanNotBeConnectedToAParallelJoinBecauseHasNoPreviousParallelSplit: MessageKey = new MessageKey("WorkflowValidationMessage", "_0CanNotBeConnectedToAParallelJoinBecauseHasNoPreviousParallelSplit");
  export const Activity0WithDecisionTypeShouldGoToAnExclusiveOrInclusiveGateways: MessageKey = new MessageKey("WorkflowValidationMessage", "Activity0WithDecisionTypeShouldGoToAnExclusiveOrInclusiveGateways");
  export const Activity0ShouldBeDecision: MessageKey = new MessageKey("WorkflowValidationMessage", "Activity0ShouldBeDecision");
  export const _0IsTimerStartAndSchedulerIsMandatory: MessageKey = new MessageKey("WorkflowValidationMessage", "_0IsTimerStartAndSchedulerIsMandatory");
  export const _0IsTimerStartAndTaskIsMandatory: MessageKey = new MessageKey("WorkflowValidationMessage", "_0IsTimerStartAndTaskIsMandatory");
  export const _0IsConditionalStartAndTaskConditionIsMandatory: MessageKey = new MessageKey("WorkflowValidationMessage", "_0IsConditionalStartAndTaskConditionIsMandatory");
  export const DelayActivitiesShouldHaveExactlyOneInterruptingTimer: MessageKey = new MessageKey("WorkflowValidationMessage", "DelayActivitiesShouldHaveExactlyOneInterruptingTimer");
  export const Activity0OfType1ShouldHaveExactlyOneConnectionOfType2: MessageKey = new MessageKey("WorkflowValidationMessage", "Activity0OfType1ShouldHaveExactlyOneConnectionOfType2");
  export const Activity0OfType1CanNotHaveConnectionsOfType2: MessageKey = new MessageKey("WorkflowValidationMessage", "Activity0OfType1CanNotHaveConnectionsOfType2");
  export const BoundaryTimer0OfActivity1ShouldHaveExactlyOneConnectionOfType2: MessageKey = new MessageKey("WorkflowValidationMessage", "BoundaryTimer0OfActivity1ShouldHaveExactlyOneConnectionOfType2");
  export const IntermediateTimer0ShouldHaveOneOutputOfType1: MessageKey = new MessageKey("WorkflowValidationMessage", "IntermediateTimer0ShouldHaveOneOutputOfType1");
  export const IntermediateTimer0ShouldHaveName: MessageKey = new MessageKey("WorkflowValidationMessage", "IntermediateTimer0ShouldHaveName");
  export const ParallelSplit0ShouldHaveAtLeastOneConnection: MessageKey = new MessageKey("WorkflowValidationMessage", "ParallelSplit0ShouldHaveAtLeastOneConnection");
  export const ParallelSplit0ShouldHaveOnlyNormalConnectionsWithoutConditions: MessageKey = new MessageKey("WorkflowValidationMessage", "ParallelSplit0ShouldHaveOnlyNormalConnectionsWithoutConditions");
  export const Join0OfType1DoesNotMatchWithItsPairTheSplit2OfType3: MessageKey = new MessageKey("WorkflowValidationMessage", "Join0OfType1DoesNotMatchWithItsPairTheSplit2OfType3");
  export const DecisionOption0IsDeclaredButNeverUsedInAConnection: MessageKey = new MessageKey("WorkflowValidationMessage", "DecisionOption0IsDeclaredButNeverUsedInAConnection");
  export const DecisionOptionName0IsNotDeclaredInAnyActivity: MessageKey = new MessageKey("WorkflowValidationMessage", "DecisionOptionName0IsNotDeclaredInAnyActivity");
  export const BoundaryTimer0OfActivity1CanNotHave2BecauseActivityIsNot3: MessageKey = new MessageKey("WorkflowValidationMessage", "BoundaryTimer0OfActivity1CanNotHave2BecauseActivityIsNot3");
  export const BoundaryTimer0OfActivity1ShouldHave2BecauseActivityIs3: MessageKey = new MessageKey("WorkflowValidationMessage", "BoundaryTimer0OfActivity1ShouldHave2BecauseActivityIs3");
  export const BoundaryTimer0OfActivity1HasInvalid23: MessageKey = new MessageKey("WorkflowValidationMessage", "BoundaryTimer0OfActivity1HasInvalid23");
}

export const WorkflowXmlEmbedded: Type<WorkflowXmlEmbedded> = new Type<WorkflowXmlEmbedded>("WorkflowXmlEmbedded");
export interface WorkflowXmlEmbedded extends Entities.EmbeddedEntity {
  Type: "WorkflowXmlEmbedded";
  diagramXml: string;
}

