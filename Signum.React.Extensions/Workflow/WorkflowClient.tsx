import * as React from 'react'
import * as QueryString from 'query-string';
import { ifError, Dic } from '@framework/Globals';
import { ajaxPost, ajaxGet, ValidationError } from '@framework/Services';
import { EntitySettings } from '@framework/Navigator'
import * as DynamicClientOptions from '../Dynamic/DynamicClientOptions';
import {
  EntityPack, Lite, toLite,
  newMListElement, Entity, ExecuteSymbol, isEntityPack, isEntity
} from '@framework/Signum.Entities'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import { TypeEntity, IUserEntity } from '@framework/Signum.Entities.Basics'
import { Type, PropertyRoute, OperationInfo } from '@framework/Reflection'
import { TypeContext } from '@framework/TypeContext'
import * as Navigator from '@framework/Navigator'
import * as Finder from '@framework/Finder'
import { EntityOperationSettings, EntityOperationContext, assertOperationInfoAllowed } from '@framework/Operations'
import * as Operations from '@framework/Operations'
import { confirmInNecessary, notifySuccess } from '@framework/Operations/EntityOperations'
import * as DynamicViewClient from '../Dynamic/DynamicViewClient'
import { CodeContext } from '../Dynamic/View/NodeUtils'
import { TimeSpanEmbedded } from '../Basics/Signum.Entities.Basics'
import TypeHelpButtonBarComponent from '../TypeHelp/TypeHelpButtonBarComponent'
import {
  WorkflowConditionEval, WorkflowTimerConditionEval, WorkflowActionEval, WorkflowMessage, WorkflowActivityMonitorMessage,
  ConnectionType, WorkflowTimerConditionEntity, WorkflowIssueType, WorkflowLaneActorsEval, CaseNotificationEntity
} from './Signum.Entities.Workflow'

import ActivityWithRemarks from './Case/ActivityWithRemarks'
import * as QuickLinks from '@framework/QuickLinks'
import * as Constructor from '@framework/Constructor'
import SelectorModal from '@framework/SelectorModal'
import ValueLineModal from '@framework/ValueLineModal'
import {
  WorkflowEntity, WorkflowLaneEntity, WorkflowActivityEntity, WorkflowConnectionEntity, WorkflowConditionEntity, WorkflowActionEntity, CaseActivityQuery, CaseActivityEntity,
  CaseActivityOperation, CaseEntity, CaseNotificationState, WorkflowOperation, WorkflowPoolEntity, WorkflowScriptEntity, WorkflowScriptEval,
  WorkflowReplacementModel, WorkflowModel, BpmnEntityPairEmbedded, WorkflowActivityModel, ICaseMainEntity, WorkflowGatewayEntity, WorkflowEventEntity,
  WorkflowLaneModel, WorkflowConnectionModel, IWorkflowNodeEntity, WorkflowActivityMessage, WorkflowTimerEmbedded, CaseTagsModel, CaseTagTypeEntity,
  WorkflowPanelPermission, WorkflowEventModel, WorkflowEventTaskEntity, DoneType, CaseOperation, WorkflowMainEntityStrategy, WorkflowActivityType
} from './Signum.Entities.Workflow'

import InboxFilter from './Case/InboxFilter'
import Workflow from './Workflow/Workflow'
import * as AuthClient from '../Authorization/AuthClient'
import { ImportRoute } from "@framework/AsyncImport";
import { FilterRequest, ColumnRequest } from '@framework/FindOptions';
import { BsColor } from '@framework/Components/Basic';
import { GraphExplorer } from '@framework/Reflection';
import WorkflowHelpComponent from './Workflow/WorkflowHelpComponent';

export function start(options: { routes: JSX.Element[] }) {

  options.routes.push(
    <ImportRoute path="~/workflow/activity/:caseActivityId" onImportModule={() => import("./Case/CaseFramePage")} />,
    <ImportRoute path="~/workflow/new/:workflowId/:mainEntityStrategy" onImportModule={() => import("./Case/CaseFramePage")} />,
    <ImportRoute path="~/workflow/panel" onImportModule={() => import("./Workflow/WorkflowPanelPage")} />,
    <ImportRoute path="~/workflow/activityMonitor/:workflowId" onImportModule={() => import("./ActivityMonitor/WorkflowActivityMonitorPage")} />,
  );

  DynamicClientOptions.Options.checkEvalFindOptions.push({ queryName: WorkflowLaneEntity, filterOptions: [{ token: WorkflowLaneEntity.token().entity(e => e.actorsEval), operation: "DistinctTo", value: null }] });
  DynamicClientOptions.Options.checkEvalFindOptions.push({ queryName: WorkflowConditionEntity });
  DynamicClientOptions.Options.checkEvalFindOptions.push({ queryName: WorkflowScriptEntity });
  DynamicClientOptions.Options.checkEvalFindOptions.push({ queryName: WorkflowActivityEntity, filterOptions: [{ token: WorkflowActivityEntity.token().entity(e => e.subWorkflow), operation: "DistinctTo", value: null }] });
  DynamicClientOptions.Options.checkEvalFindOptions.push({ queryName: WorkflowActionEntity });
  DynamicClientOptions.Options.checkEvalFindOptions.push({ queryName: WorkflowTimerConditionEntity });

  QuickLinks.registerQuickLink(CaseActivityEntity, ctx => [
    new QuickLinks.QuickLinkAction("caseFlow", WorkflowActivityMessage.CaseFlow.niceToString(), e => {
      Navigator.API.fetchAndForget(ctx.lite)
        .then(ca => Navigator.navigate(ca.case, { extraComponentProps: { caseActivity: ca } }))
        .then(() => ctx.contextualContext && ctx.contextualContext.markRows({}))
        .done();
    }, { icon: "random", iconColor: "green" })
  ]);

  QuickLinks.registerQuickLink(WorkflowEntity, ctx => [
    new QuickLinks.QuickLinkExplore({ queryName: CaseEntity, parentToken: CaseEntity.token(e => e.workflow), parentValue: ctx.lite },
      { icon: "tasks", iconColor: "blue" })
  ]);

  OmniboxClient.registerSpecialAction({
    allowed: () => AuthClient.isPermissionAuthorized(WorkflowPanelPermission.ViewWorkflowPanel),
    key: "WorkflowPanel",
    onClick: () => Promise.resolve("~/workflow/panel")
  });

  Finder.addSettings({
    queryName: CaseActivityQuery.Inbox,
    hiddenColumns: [
      { token: CaseNotificationEntity.token(e => e.state) },
    ],
    rowAttributes: (row, columns) => {
      var rowState = row.columns[columns.indexOf("State")] as CaseNotificationState;
      switch (rowState) {
        case "New": return { className: "new-row" };
        case "Opened": return { className: "opened-row" };
        case "InProgress": return { className: "in-progress-row" };
        case "Done": return { className: "done-row" };
        case "DoneByOther": return { className: "done-by-other-row" };
        default: return {};
      };
    },
    formatters: {
      "Activity": new Finder.CellFormatter(cell => <ActivityWithRemarks data={cell} />)
    },
    defaultOrderColumn: "StartDate",
    simpleFilterBuilder: (qd, fos) => {
      var model = InboxFilter.extract(fos);

      if (!model)
        return undefined;

      return <InboxFilter ctx={TypeContext.root(model)} />;
    }
  });

  Navigator.addSettings(new EntitySettings(CaseEntity, w => import('./Case/Case')));
  Navigator.addSettings(new EntitySettings(CaseTagTypeEntity, w => import('./Case/CaseTagType')));
  Navigator.addSettings(new EntitySettings(CaseTagsModel, w => import('./Case/CaseTagsModel')));

  Navigator.addSettings(new EntitySettings(CaseActivityEntity, undefined, {
    onNavigateRoute: (typeName, id) => Navigator.toAbsoluteUrl("~/workflow/activity/" + id),
    onNavigate: (entityOrPack, options) => navigateCase(isEntityPack(entityOrPack) ? entityOrPack.entity : entityOrPack, options && options.readOnly),
    onView: (entityOrPack, options) => viewCase(isEntityPack(entityOrPack) ? entityOrPack.entity : entityOrPack, options && options.readOnly),
  }));

  Operations.addSettings(new EntityOperationSettings(CaseOperation.SetTags, { isVisible: ctx => false }));
  Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Register, { hideOnCanExecute: true, color: "primary", onClick: eoc => executeCaseActivity(eoc, e => e.defaultClick()), }));
  Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Delete, { hideOnCanExecute: true, isVisible: ctx => false, contextual: { isVisible: ctx => true } }));
  Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Jump, { onClick: eoc => executeCaseActivity(eoc, executeWorkflowJump), contextual: { isVisible: ctx => true, onClick: executeWorkflowJumpContextual } }));
  Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Timer, { isVisible: ctx => false }));
  Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.MarkAsUnread, { hideOnCanExecute: true, isVisible: ctx => false, contextual: { isVisible: ctx => true } }));
  Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.ScriptExecute, { isVisible: ctx => false }));
  Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.ScriptFailureJump, { isVisible: ctx => false }));
  Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.ScriptScheduleRetry, { isVisible: ctx => false }));
  Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.CreateCaseActivityFromWorkflow, { isVisible: ctx => false }));
  Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.CreateCaseFromWorkflowEventTask, { isVisible: ctx => false }));

  caseActivityOperation(CaseActivityOperation.Next, "primary");
  caseActivityOperation(CaseActivityOperation.Approve, "success");
  caseActivityOperation(CaseActivityOperation.Decline, "warning");
  caseActivityOperation(CaseActivityOperation.Undo, "danger");

  QuickLinks.registerQuickLink(WorkflowEntity, ctx => new QuickLinks.QuickLinkLink("bam",
    WorkflowActivityMonitorMessage.WorkflowActivityMonitor.niceToString(),
    workflowActivityMonitorUrl(ctx.lite),
    { icon: "tachometer-alt", iconColor: "green" }));

  Operations.addSettings(new EntityOperationSettings(WorkflowOperation.Save, { color: "primary", onClick: executeWorkflowSave }));
  Operations.addSettings(new EntityOperationSettings(WorkflowOperation.Delete, { contextualFromMany: { isVisible: ctx => false } }));
  Operations.addSettings(new EntityOperationSettings(WorkflowOperation.Activate, {
    contextual: { icon: "heartbeat", iconColor: "red" },
    contextualFromMany: { icon: "heartbeat", iconColor: "red" },
  }));
  Operations.addSettings(new EntityOperationSettings(WorkflowOperation.Deactivate, {
    onClick: eoc => chooseWorkflowExpirationDate([toLite(eoc.entity)]).then(val => val && eoc.defaultClick(val)).done(),
    contextual: {
      onClick: coc => chooseWorkflowExpirationDate(coc.context.lites).then(val => val && coc.defaultContextualClick(val)).done(),
      icon: ["far", "heart"],
      iconColor: "gray"
    },
    contextualFromMany: {
      onClick: coc => chooseWorkflowExpirationDate(coc.context.lites).then(val => val && coc.defaultContextualClick(val)).done(),
      icon: ["far", "heart"],
      iconColor: "gray"
    },
  }));
  Navigator.addSettings(new EntitySettings(WorkflowEntity, w => import('./Workflow/Workflow'), { avoidPopup: true }));

  hide(WorkflowPoolEntity);
  hide(WorkflowLaneEntity);
  hide(WorkflowActivityEntity);
  hide(WorkflowGatewayEntity);
  hide(WorkflowEventEntity);
  hide(WorkflowConnectionEntity);

  Navigator.addSettings(new EntitySettings(WorkflowActivityModel, w => import('./Workflow/WorkflowActivityModel')));
  Navigator.addSettings(new EntitySettings(WorkflowConnectionModel, w => import('./Workflow/WorkflowConnectionModel')));
  Navigator.addSettings(new EntitySettings(WorkflowReplacementModel, w => import('./Workflow/WorkflowReplacementComponent')));
  Navigator.addSettings(new EntitySettings(WorkflowConditionEntity, w => import('./Workflow/WorkflowCondition')));
  Navigator.addSettings(new EntitySettings(WorkflowTimerConditionEntity, w => import('./Workflow/WorkflowTimerCondition')));
  Navigator.addSettings(new EntitySettings(WorkflowActionEntity, w => import('./Workflow/WorkflowAction')));
  Navigator.addSettings(new EntitySettings(WorkflowScriptEntity, w => import('./Workflow/WorkflowScript')));
  Navigator.addSettings(new EntitySettings(WorkflowLaneModel, w => import('./Workflow/WorkflowLaneModel')));
  Navigator.addSettings(new EntitySettings(WorkflowEventModel, w => import('./Workflow/WorkflowEventModel')));
  Navigator.addSettings(new EntitySettings(WorkflowEventTaskEntity, w => import('./Workflow/WorkflowEventTask')));

  Constructor.registerConstructor(WorkflowEntity, () => WorkflowEntity.New({ mainEntityStrategies: [newMListElement(WorkflowMainEntityStrategy.value("CreateNew"))] }));
  Constructor.registerConstructor(WorkflowConditionEntity, () => WorkflowConditionEntity.New({ eval: WorkflowConditionEval.New() }));
  Constructor.registerConstructor(WorkflowTimerConditionEntity, () => WorkflowTimerConditionEntity.New({ eval: WorkflowTimerConditionEval.New() }));
  Constructor.registerConstructor(WorkflowActionEntity, () => WorkflowActionEntity.New({ eval: WorkflowActionEval.New() }));
  Constructor.registerConstructor(WorkflowScriptEntity, () => WorkflowScriptEntity.New({ eval: WorkflowScriptEval.New() }));
  Constructor.registerConstructor(WorkflowTimerEmbedded, () => Constructor.construct(TimeSpanEmbedded).then(ep => ep && WorkflowTimerEmbedded.New({ duration: ep.entity })));

  registerCustomContexts();

  TypeHelpButtonBarComponent.getTypeHelpButtons.push(props => [({
    element: <WorkflowHelpComponent typeName={props.typeName} mode={props.mode} />,
    order: 0,
  })]);
}

function chooseWorkflowExpirationDate(workflows: Lite<WorkflowEntity>[]): Promise<string | undefined> {
  return ValueLineModal.show({
    type: { name: "string" },
    valueLineType: "DateTime",
    modalSize: "md",
    title: WorkflowMessage.DeactivateWorkflow.niceToString(),
    message:
      <div>
        <strong>{WorkflowMessage.PleaseChooseExpirationDate.niceToString()}</strong>
        <ul>{workflows.map((w, i) => <li key={i}>{w.toStr}</li>)}</ul>
      </div>
  });
}

export function workflowActivityMonitorUrl
  (workflow: Lite<WorkflowEntity>) {
  return `~/workflow/activityMonitor/${workflow.id}`;
}

function registerCustomContexts() {

  function addActx(cc: CodeContext) {
    if (!cc.assignments["actx"]) {
      cc.assignments["actx"] = "getCaseActivityContext(ctx)";
      cc.imports.push("import { getCaseActivityContext } as WorkflowClient from '../../Workflow/WorkflowClient'");
    }
  }

  DynamicViewClient.registeredCustomContexts["caseActivity"] = {
    getTypeContext: ctx => {
      var actx = getCaseActivityContext(ctx);
      return actx;
    },
    getCodeContext: cc => {
      addActx(cc);
      return cc.createNewContext("actx");
    },
    getPropertyRoute: dn => PropertyRoute.root(CaseActivityEntity)
  };

  DynamicViewClient.registeredCustomContexts["case"] = {
    getTypeContext: ctx => {
      var actx = getCaseActivityContext(ctx);
      return actx && actx.subCtx(a => a.case);
    },
    getCodeContext: cc => {
      addActx(cc);
      cc.assignments["cctx"] = "actx && actx.subCtx(a => a.case)";
      return cc.createNewContext("cctx");
    },
    getPropertyRoute: dn => CaseActivityEntity.propertyRoute(a => a.case)
  };


  DynamicViewClient.registeredCustomContexts["parentCase"] = {
    getTypeContext: ctx => {
      var actx = getCaseActivityContext(ctx);
      return actx && actx.value.case.parentCase ? actx.subCtx(a => a.case.parentCase) : undefined;
    },
    getCodeContext: cc => {
      addActx(cc);
      cc.assignments["pcctx"] = "actx && actx.value.case.parentCase && actx.subCtx(a => a.case.parentCase)";
      return cc.createNewContext("pcctx");
    },
    getPropertyRoute: dn => CaseActivityEntity.propertyRoute(a => a.case.parentCase)
  };

  DynamicViewClient.registeredCustomContexts["parentCaseMainEntity"] = {
    getTypeContext: ctx => {
      var actx = getCaseActivityContext(ctx);
      return actx && actx.value.case.parentCase ? actx.subCtx(a => a.case.parentCase!.mainEntity) : undefined;
    },
    getCodeContext: cc => {
      addActx(cc);
      cc.assignments["pmctx"] = "actx && actx.value.case.parentCase && actx.subCtx(a => a.case.parentCase!.mainEntity)";
      return cc.createNewContext("pmctx");
    },
    getPropertyRoute: dn => CaseActivityEntity.propertyRoute(a => a.case.parentCase!.mainEntity)
  };
}

export function getCaseActivityContext(ctx: TypeContext<any>): TypeContext<CaseActivityEntity> | undefined {
  const f = ctx.frame;
  const fc = f && f.frameComponent as any;
  const activity = fc && fc.getCaseActivity && fc.getCaseActivity() as CaseActivityEntity;
  return activity && TypeContext.root(activity, undefined, ctx);
}

export function getDefaultInboxUrl() {
  return Finder.findOptionsPath({
    queryName: CaseActivityQuery.Inbox,
    filterOptions: [{
      token: CaseNotificationEntity.token(e => e.state),
      operation: "IsIn",
      value: ["New", "Opened", "InProgress"]
    }]
  });
}

export function showWorkflowTransitionContextCodeHelp() {

  var value = `public CaseActivityEntity PreviousCaseActivity { get; internal set; }
public DecisionResult? DecisionResult { get; internal set; }
public IWorkflowTransition Connection { get; internal set; }
public CaseEntity Case { get; set; }

public interface IWorkflowTransition
{
    Lite<WorkflowConditionEntity> Condition { get; }
    Lite<WorkflowActionEntity> Action { get; }
}`;

  ValueLineModal.show({
    type: { name: "string" },
    initialValue: value,
    valueLineType: "TextArea",
    title: "WorkflowTransitionContext Members",
    message: "Copy to clipboard: Ctrl+C, ESC",
    initiallyFocused: true,
    valueHtmlAttributes: { style: { height: 215 } },
  }).done();
}

function caseActivityOperation(operation: ExecuteSymbol<CaseActivityEntity>, color: BsColor) {
  Operations.addSettings(new EntityOperationSettings(operation, {
    hideOnCanExecute: true,
    color: color,
    onClick: eoc => executeCaseActivity(eoc, executeAndClose),
    contextual: { isVisible: ctx => true },
    contextualFromMany: {
      isVisible: ctx => true,
      color: color
    },
  }));
}

function hide<T extends Entity>(type: Type<T>) {
  Navigator.addSettings(new EntitySettings(type, undefined, { isNavigable: "Never", isViewable: false, isCreable: "Never" }));
}

export function executeCaseActivity(eoc: Operations.EntityOperationContext<CaseActivityEntity>, defaultOnClick: (eoc: Operations.EntityOperationContext<CaseActivityEntity>) => void) {
  const op = customOnClicks[eoc.operationInfo.key];

  const onClick = op && op[eoc.entity.case.mainEntity.Type];

  if (onClick)
    onClick(eoc);
  else
    defaultOnClick(eoc);
}

export function executeWorkflowSave(eoc: Operations.EntityOperationContext<WorkflowEntity>) {


  function saveAndSetErrors(entity: WorkflowEntity, model: WorkflowModel, replacementModel: WorkflowReplacementModel | undefined) {
    API.saveWorkflow(entity, model, replacementModel)
      .then(packWithIssues => {
        eoc.frame.onReload(packWithIssues.entityPack);
        (eoc.frame.entityComponent as any).setIssues(packWithIssues.issues);
        notifySuccess();
        if (eoc.closeRequested)
          eoc.frame.onClose(true);
      })
      .catch(ifError(ValidationError, e => {

        var issuesString = e.modelState["workflowIssues"];
        if (issuesString) {
          (eoc.frame.entityComponent as any).setIssues(JSON.parse(issuesString[0]));
          delete e.modelState["workflowIssues"];
        }
        eoc.frame.setError(e.modelState, "entity");

      }))
      .done();
  }

  let wf = eoc.frame.entityComponent as Workflow;
  wf.getXml()
    .then(xml => {
      var model = WorkflowModel.New({
        diagramXml: xml,
        entities: Dic.map(wf.state.entities!, (bpmnId, model) => newMListElement(BpmnEntityPairEmbedded.New({
          bpmnElementId: bpmnId,
          model: model
        })))
      });

      var promise = eoc.entity.isNew ?
        Promise.resolve<PreviewResult | undefined>(undefined) :
        API.previewChanges(toLite(eoc.entity), model);

      promise.then(pr => {
        if (!pr || pr.model.replacements.length == 0)
          saveAndSetErrors(eoc.entity, model, undefined);
        else
          Navigator.view(pr.model, { extraComponentProps: { previewTasks: pr.newTasks } }).then(replacementModel => {
            if (!replacementModel)
              return;

            saveAndSetErrors(eoc.entity, model, replacementModel);
          }).done();
      }).done();
    }).done();



}

export function executeWorkflowJumpContextual(coc: Operations.ContextualOperationContext<CaseActivityEntity>) {

  Navigator.API.fetchAndForget(coc.context.lites[0])
    .then(ca => {

      getWorkflowJumpSelector(toLite(ca.workflowActivity as WorkflowActivityEntity))
        .then(dest => dest && coc.defaultContextualClick(dest));
    })
    .done();
}

export function executeWorkflowJump(eoc: Operations.EntityOperationContext<CaseActivityEntity>) {

  eoc.closeRequested = true;

  getWorkflowJumpSelector(toLite(eoc.entity.workflowActivity as WorkflowActivityEntity))
    .then(dest => dest && eoc.defaultClick(dest))
    .done();
}

function getWorkflowJumpSelector(activity: Lite<WorkflowActivityEntity>): Promise<Lite<IWorkflowNodeEntity> | undefined> {

  return API.nextConnections({ workflowActivity: activity, connectionType: "Jump" })
    .then(jumps => SelectorModal.chooseElement(jumps,
      {
        title: WorkflowActivityMessage.ChooseADestinationForWorkflowJumping.niceToString(),
        buttonDisplay: a => a.toStr || "",
        forceShow: true
      }));
}

export function executeAndClose(eoc: Operations.EntityOperationContext<CaseActivityEntity>) {

  confirmInNecessary(eoc).then(conf => {
    if (!conf)
      return;

    Operations.API.executeEntity(eoc.entity, eoc.operationInfo.key)
      .then(pack => { eoc.frame.onClose(); return notifySuccess(); })
      .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")))
      .done();
  });
}

export function navigateCase(entityOrPack: Lite<CaseActivityEntity> | CaseActivityEntity | CaseEntityPack, readOnly?: boolean): Promise<void> {

  return import("./Case/CaseFrameModal")
    .then(NP => NP.default.openNavigate(entityOrPack, readOnly)) as Promise<void>;
}

export function viewCase(entityOrPack: Lite<CaseActivityEntity> | CaseActivityEntity | CaseEntityPack, readOnly?: boolean): Promise<CaseActivityEntity | undefined> {
  return import("./Case/CaseFrameModal")
    .then(NP => NP.default.openView(entityOrPack, readOnly));

}

export function createNewCase(workflowId: number | string, mainEntityStrategy: WorkflowMainEntityStrategy): Promise<CaseEntityPack | undefined> {
  return Navigator.API.fetchEntity(WorkflowEntity, workflowId)
    .then(wf => {
      if (mainEntityStrategy == "CreateNew")
        return Operations.API.constructFromEntity(wf, CaseActivityOperation.CreateCaseActivityFromWorkflow);

      var coi: OperationInfo;

      if (mainEntityStrategy == "Clone") {
        coi = Operations.getOperationInfo(`${wf.mainEntityType!.cleanName}Operation.Clone`, wf.mainEntityType!.cleanName);
        assertOperationInfoAllowed(coi);
      }

      return Finder.find({ queryName: wf.mainEntityType!.cleanName })
        .then(lite => {
          if (!lite)
            return Promise.resolve(undefined);

          return Navigator.API.fetchAndForget(lite!)
            .then(entity => {
              if (mainEntityStrategy == "Clone") {
                return Operations.API.constructFromEntity(entity, coi.key)
                  .then(pack => Operations.API.constructFromEntity(wf, CaseActivityOperation.CreateCaseActivityFromWorkflow, pack.entity));
              }
              else
                return Operations.API.constructFromEntity(wf, CaseActivityOperation.CreateCaseActivityFromWorkflow, entity);
            });
        });
    })
    .then(ep => ep && ({
      activity: ep.entity,
      canExecuteActivity: ep.canExecute,
      canExecuteMainEntity: {}
    }) as CaseEntityPack);
}

export function toEntityPackWorkflow(entityOrEntityPack: Lite<CaseActivityEntity> | CaseActivityEntity | CaseEntityPack): Promise<CaseEntityPack> {
  if ((entityOrEntityPack as CaseEntityPack).canExecuteActivity)
    return Promise.resolve(entityOrEntityPack as CaseEntityPack);

  const lite = isEntity(entityOrEntityPack) ? toLite(entityOrEntityPack) : entityOrEntityPack as Lite<CaseActivityEntity>;

  return API.fetchActivityForViewing(lite);
}

export const customOnClicks: { [operationKey: string]: { [typeName: string]: (ctx: EntityOperationContext<CaseActivityEntity>) => void } } = {};

export function registerOnClick<T extends ICaseMainEntity>(type: Type<T>, operationKey: ExecuteSymbol<CaseActivityEntity>, action: (ctx: EntityOperationContext<CaseActivityEntity>) => void) {
  var op = customOnClicks[operationKey.key] || (customOnClicks[operationKey.key] = {});
  op[type.typeName] = action;
}

export interface IHasCaseActivity {
  getCaseActivity(): CaseActivityEntity | undefined;
}

export function inWorkflow(ctx: TypeContext<any>, workflowName: string, activityName: string): boolean {
  var f = ctx.frame && ctx.frame.frameComponent as any as IHasCaseActivity;

  var ca = f && f.getCaseActivity && f.getCaseActivity();

  if (!ca)
    return false;

  var wa = ca.workflowActivity as WorkflowActivityEntity;

  return wa.lane!.pool!.workflow!.name == workflowName && wa.name == activityName;
}

export namespace API {
  export function fetchActivityForViewing(caseActivity: Lite<CaseActivityEntity>): Promise<CaseEntityPack> {
    return ajaxGet<CaseEntityPack>({ url: `~/api/workflow/fetchForViewing/${caseActivity.id}` });
  }

  export function fetchCaseTags(caseLite: Lite<CaseEntity>): Promise<CaseTagTypeEntity[]> {
    return ajaxGet<CaseTagTypeEntity[]>({ url: `~/api/workflow/tags/${caseLite.id}` });
  }

  export function starts(): Promise<Array<WorkflowEntity>> {
    return ajaxGet<Array<WorkflowEntity>>({ url: `~/api/workflow/starts` });
  }

  export function getWorkflowModel(workflow: Lite<WorkflowEntity>): Promise<WorkflowModelAndIssues> {
    return ajaxGet<WorkflowModelAndIssues>({ url: `~/api/workflow/workflowModel/${workflow.id}` });
  }

  interface WorkflowModelAndIssues {
    model: WorkflowModel;
    issues: Array<WorkflowIssue>;
  }

  export function previewChanges(workflow: Lite<WorkflowEntity>, model: WorkflowModel): Promise<PreviewResult> {
    return ajaxPost<PreviewResult>({ url: `~/api/workflow/previewChanges/${workflow.id} ` }, model);
  }

  export function saveWorkflow(entity: WorkflowEntity, model: WorkflowModel, replacementModel: WorkflowReplacementModel | undefined): Promise<EntityPackWithIssues> {
    GraphExplorer.propagateAll(entity, model, replacementModel);
    return ajaxPost<EntityPackWithIssues>({ url: "~/api/workflow/save" }, { entity: entity, operationKey: WorkflowOperation.Save.key, args: [model, replacementModel] } as Operations.API.EntityOperationRequest);
  }

  interface EntityPackWithIssues {
    entityPack: EntityPack<WorkflowEntity>;
    issues: Array<WorkflowIssue>;
  }

  export interface WorkflowIssue {
    type: WorkflowIssueType;
    bpmnElementId: string;
    message: string;
  }

  export function findMainEntityType(request: { subString: string, count: number }, signal?: AbortSignal): Promise<Lite<TypeEntity>[]> {
    return ajaxGet<Lite<TypeEntity>[]>({
      url: "~/api/workflow/findMainEntityType?" + QueryString.stringify(request),
      signal
    });
  }

  export function findNode(request: WorkflowFindNodeRequest, signal?: AbortSignal): Promise<Lite<IWorkflowNodeEntity>[]> {
    return ajaxPost<Lite<IWorkflowNodeEntity>[]>({ url: "~/api/workflow/findNode", signal }, request);
  }

  export function conditionTest(request: WorkflowConditionTestRequest): Promise<WorkflowConditionTestResponse> {
    return ajaxPost<WorkflowConditionTestResponse>({ url: `~/api/workflow/condition/test` }, request);
  }

  export function view(): Promise<WorkflowScriptRunnerState> {
    return ajaxGet<WorkflowScriptRunnerState>({ url: "~/api/workflow/scriptRunner/view" });
  }

  export function start(): Promise<void> {
    return ajaxPost<void>({ url: "~/api/workflow/scriptRunner/start" }, undefined);
  }

  export function stop(): Promise<void> {
    return ajaxPost<void>({ url: "~/api/workflow/scriptRunner/stop" }, undefined);
  }

  export function caseFlow(c: Lite<CaseEntity>): Promise<CaseFlow> {
    return ajaxGet<CaseFlow>({ url: `~/api/workflow/caseFlow/${c.id}` });
  }

  export function workflowActivityMonitor(request: WorkflowActivityMonitorRequest): Promise<WorkflowActivityMonitor> {
    return ajaxPost<WorkflowActivityMonitor>({ url: "~/api/workflow/activityMonitor" }, request);
  }

  export function nextConnections(request: NextConnectionsRequest): Promise<Array<Lite<IWorkflowNodeEntity>>> {
    return ajaxPost<Array<Lite<IWorkflowNodeEntity>>>({ url: "~/api/workflow/nextConnections" }, request);
  }
}

export interface NextConnectionsRequest {
  workflowActivity: Lite<WorkflowActivityEntity>;
  connectionType: ConnectionType;
}

export interface WorkflowFindNodeRequest {
  workflowId: number | string;
  subString: string;
  count: number;
  excludes?: Lite<IWorkflowNodeEntity>[];
}

export interface WorkflowConditionTestRequest {
  workflowCondition: WorkflowConditionEntity;
  exampleEntity: ICaseMainEntity;
}

export interface WorkflowConditionTestResponse {
  compileError?: string;
  validationException?: string;
  validationResult?: boolean;
}

export const DecisionResultValues = ["Approve", "Decline"];

export interface PreviewResult {
  model: WorkflowReplacementModel;
  newTasks: PreviewTask[];
}

export interface PreviewTask {
  bpmnId: string;
  name: string;
  subWorkflow: Lite<WorkflowEntity>;
}

export interface CaseEntityPack {
  activity: CaseActivityEntity;
  canExecuteActivity: { [key: string]: string };
  canExecuteMainEntity: { [key: string]: string };
}

export interface WorkflowScriptRunnerState {
  scriptRunnerPeriod: number;
  running: boolean;
  isCancelationRequested: boolean;
  nextPlannedExecution: string;
  queuedItems: number;
  currentProcessIdentifier: string;
}

export interface CaseActivityStats {
  caseActivity: Lite<CaseActivityEntity>;
  previousActivity: Lite<CaseActivityEntity>;
  workflowActivity: Lite<WorkflowActivityEntity>;
  workflowActivityType: WorkflowActivityType;
  subWorkflow: Lite<WorkflowEntity>;
  notifications: number;
  startDate: string;
  doneDate?: string;
  doneType?: DoneType;
  doneBy: Lite<IUserEntity>;
  duration?: number;
  averageDuration?: number;
  estimatedDuration?: number;

}
export interface CaseConnectionStats {
  connection?: Lite<WorkflowConnectionEntity>;
  doneDate: string;
  doneBy: Lite<IUserEntity>;
  doneType: DoneType;

  bpmnElementId?: string;
  fromBpmnElementId: string;
  toBpmnElementId: string;
}

export interface CaseFlow {
  activities: { [bpmnElementId: string]: CaseActivityStats[] };
  connections: { [bpmnElementId: string]: CaseConnectionStats[] };
  jumps: CaseConnectionStats[];
  allNodes: string[];
}

export interface WorkflowActivityMonitorRequest {
  workflow: Lite<WorkflowEntity>;
  filters: FilterRequest[];
  columns: ColumnRequest[];
}

export interface WorkflowActivityStats {
  workflowActivity: Lite<WorkflowActivityEntity>;
  caseActivityCount: number;
  customValues: any[];
}

export interface WorkflowActivityMonitor {
  workflow: Lite<WorkflowEntity>;
  customColumns: string[];
  activities: WorkflowActivityStats[];
}

