import * as React from 'react'
import { RouteObject } from 'react-router'
import { Duration, DurationUnit } from 'luxon';
import { ifError, Dic } from '@framework/Globals';
import { ajaxPost, ajaxGet, ValidationError } from '@framework/Services';
import { Navigator, EntitySettings } from '@framework/Navigator'
import { EvalClient } from '../Signum.Eval/EvalClient';
import {
  EntityPack, Lite, toLite, newMListElement, Entity, isEntityPack, isEntity, getToString,
  JavascriptMessage
} from '@framework/Signum.Entities'
import { TypeEntity } from '@framework/Signum.Basics'
import { Type, PropertyRoute, OperationInfo, toNumberFormat, getQueryKey, getQueryNiceName, getOperationInfo } from '@framework/Reflection'
import { TypeContext } from '@framework/TypeContext'
import * as OmniboxSpecialAction from '@framework/OmniboxSpecialAction'
import { Finder } from '@framework/Finder'
import { Operations, EntityOperationSettings, EntityOperationContext, ContextualOperationContext } from '@framework/Operations'
import { EntityOperations, OperationButton } from '@framework/Operations/EntityOperations'
import TypeHelpButtonBarComponent from '../Signum.Eval/TypeHelp/TypeHelpButtonBarComponent'
import {
  WorkflowConditionEval, WorkflowTimerConditionEval, WorkflowActionEval, WorkflowMessage, WorkflowActivityMonitorMessage,
  ConnectionType, WorkflowTimerConditionEntity, WorkflowIssueType, CaseNotificationEntity, CaseNotificationOperation, CaseMessage, WorkflowScriptRetryStrategyEntity, WorkflowEventType, TimeSpanEmbedded
} from './Signum.Workflow'

import ActivityWithRemarks from './Case/ActivityWithRemarks'
import { QuickLinkClient, QuickLinkAction, QuickLinkExplore, QuickLinkLink } from '@framework/QuickLinkClient'
import { Constructor } from '@framework/Constructor'
import SelectorModal from '@framework/SelectorModal'
import AutoLineModal from '@framework/AutoLineModal'
import {
  WorkflowEntity, WorkflowLaneEntity, WorkflowActivityEntity, WorkflowConnectionEntity, WorkflowConditionEntity, WorkflowActionEntity, CaseActivityQuery, CaseActivityEntity,
  CaseActivityOperation, CaseEntity, CaseNotificationState, WorkflowOperation, WorkflowPoolEntity, WorkflowScriptEntity, WorkflowScriptEval,
  WorkflowReplacementModel, WorkflowModel, BpmnEntityPairEmbedded, WorkflowActivityModel, ICaseMainEntity, WorkflowGatewayEntity, WorkflowEventEntity,
  WorkflowLaneModel, WorkflowConnectionModel, IWorkflowNodeEntity, WorkflowActivityMessage, WorkflowTimerEmbedded, CaseTagsModel, CaseTagTypeEntity,
  WorkflowPermission, WorkflowEventModel, WorkflowEventTaskEntity, DoneType, CaseOperation, WorkflowMainEntityStrategy, WorkflowActivityType, CaseActivityMixin,
} from './Signum.Workflow'


import InboxFilter from './Case/InboxFilter'
import { WorkflowHandle } from './Workflow/Workflow'
import { AuthClient } from '../Signum.Authorization/AuthClient'
import { ImportComponent } from '@framework/ImportComponent'
import { FilterRequest, ColumnRequest } from '@framework/FindOptions';
import { BsColor } from '@framework/Components/Basic';
import { GraphExplorer } from '@framework/Reflection';
import WorkflowHelpComponent from './Workflow/WorkflowHelpComponent';
import { EntityLine, TextAreaLine } from '@framework/Lines';
import { SMSMessageEntity } from '../Signum.SMS/Signum.SMS';
import { EmailMessageEntity } from '../Signum.Mailing/Signum.Mailing';
import { FunctionalAdapter } from '@framework/Modals';
import { QueryString } from '@framework/QueryString';
import { UserAssetClient } from '../Signum.UserAssets/UserAssetClient'
import { ContextualOperations } from '@framework/Operations/ContextualOperations';
import { UserEntity } from '../Signum.Authorization/Signum.Authorization';
import MessageModal from '@framework/Modals/MessageModal';
import { ExecuteSymbol } from '@framework/Signum.Operations';
import { IUserEntity } from '@framework/Signum.Security';
import { ToolbarClient } from '../Signum.Toolbar/ToolbarClient';
import WorkflowToolbarConfig from './WorkflowToolbarConfig';
import WorkflowToolbarMenuConfig from './WorkflowToolbarMenuConfig';
import { isPermissionAuthorized } from '@framework/AppContext';
import "./Case/Inbox.css";
import { useAPI } from '../../Signum/React/Hooks';


export namespace WorkflowClient {

  export interface WorkflowCustomClick<T extends Entity> {
    onClick: (eoc: EntityOperationContext<T>) => Promise<void>
    onContextualClick?: (coc: ContextualOperationContext<T>) => Promise<void>;
    onContextualFromManyClick?: (coc: ContextualOperationContext<T>) => Promise<void>;
  }

  export const registeredOnClick: Record<string, Record<string, WorkflowCustomClick<any>>> = {};

  export function registerCustomClick<T extends Entity>(type: Type<T>, workflowActivity: string, customClickName: string, wcc: WorkflowCustomClick<T>): void {

    if (!registeredOnClick[workflowActivity])
      registeredOnClick[workflowActivity] = {}; 

    registeredOnClick[workflowActivity][customClickName] = wcc;

  }
  
  export function start(options: { routes: RouteObject[], overrideCaseActivityMixin?: boolean }): void {

    UserAssetClient.start({ routes: options.routes });
    UserAssetClient.registerExportAssertLink(WorkflowEntity);

    ToolbarClient.registerConfig(new WorkflowToolbarConfig());
    ToolbarClient.registerConfig(new WorkflowToolbarMenuConfig());

    options.routes.push(
      { path: "/workflow/activity/:caseActivityId", element: <ImportComponent onImport={() => import("./Case/CaseFramePage")} /> },
      { path: "/workflow/new/:workflowId/:mainEntityStrategy", element: <ImportComponent onImport={() => import("./Case/CaseFramePage")} /> },
      { path: "/workflow/panel", element: <ImportComponent onImport={() => import("./Workflow/WorkflowPanelPage")} /> },
      { path: "/workflow/activityMonitor/:workflowId", element: <ImportComponent onImport={() => import("./ActivityMonitor/WorkflowActivityMonitorPage")} /> },
    );

    EvalClient.Options.checkEvalFindOptions.push({ queryName: WorkflowLaneEntity, filterOptions: [{ token: WorkflowLaneEntity.token(e => e.entity.actorsEval), operation: "DistinctTo", value: null }] });
    EvalClient.Options.checkEvalFindOptions.push({ queryName: WorkflowConditionEntity });
    EvalClient.Options.checkEvalFindOptions.push({ queryName: WorkflowScriptEntity });
    EvalClient.Options.checkEvalFindOptions.push({ queryName: WorkflowActivityEntity, filterOptions: [{ token: WorkflowActivityEntity.token(e => e.entity.subWorkflow), operation: "DistinctTo", value: null }] });
    EvalClient.Options.checkEvalFindOptions.push({ queryName: WorkflowActionEntity });
    EvalClient.Options.checkEvalFindOptions.push({ queryName: WorkflowTimerConditionEntity });
    EvalClient.Options.checkEvalFindOptions.push({ queryName: WorkflowEventTaskEntity });

    Navigator.addSettings(new EntitySettings(TimeSpanEmbedded, e => import('./Workflow/TimeSpan')));
    Constructor.registerConstructor(TimeSpanEmbedded, () => TimeSpanEmbedded.New({ days: 0, hours: 0, minutes: 0, seconds: 0 }));

    EvalClient.Options.registerDynamicPanelSearch(WorkflowEntity, t => [
      { token: t.append(p => p.name), type: "Text" },
      { token: t.append(p => p.mainEntityType.cleanName), type: "Text" },
    ]);

    EvalClient.Options.registerDynamicPanelSearch(WorkflowActivityEntity, t => [
      { token: t.append(p => p.name), type: "Text" },
      { token: t.append(p => p.entity.viewName), type: "Text" },
    ]);

    EvalClient.Options.registerDynamicPanelSearch(WorkflowActionEntity, t => [
      { token: t.append(p => p.name), type: "Text" },
      { token: t.append(p => p.mainEntityType.cleanName), type: "Text" },
      { token: t.append(p => p.entity.eval.script), type: "Code" },
    ]);

    EvalClient.Options.registerDynamicPanelSearch(WorkflowScriptEntity, t => [
      { token: t.append(p => p.name), type: "Text" },
      { token: t.append(p => p.mainEntityType.cleanName), type: "Text" },
      { token: t.append(p => p.entity.eval.script), type: "Code" },
    ]);

    EvalClient.Options.registerDynamicPanelSearch(WorkflowConditionEntity, t => [
      { token: t.append(p => p.name), type: "Text" },
      { token: t.append(p => p.mainEntityType.cleanName), type: "Text" },
      { token: t.append(p => p.entity.eval.script), type: "Code" },
    ]);

    EvalClient.Options.registerDynamicPanelSearch(WorkflowTimerConditionEntity, t => [
      { token: t.append(p => p.name), type: "Text" },
      { token: t.append(p => p.mainEntityType.cleanName), type: "Text" },
      { token: t.append(p => p.entity.eval.script), type: "Code" },
    ]);

    EvalClient.Options.registerDynamicPanelSearch(WorkflowEventTaskEntity, t => [
      { token: t.append(p => p.entity.event.entity!.name), type: "Text" },
      { token: t.append(p => p.entity.condition!.script), type: "Code" },
      { token: t.append(p => p.entity.action!.script), type: "Code" },
    ]);

    QuickLinkClient.registerQuickLink(CaseActivityEntity, new QuickLinkAction("caseFlow", () => WorkflowActivityMessage.CaseFlow.niceToString(), ctx => {
      API.fetchCaseFlowPack(ctx.lite)
        .then(result => Navigator.view(result.pack, { extraProps: { workflowActivity: result.workflowActivity } }))
        .then(() => ctx.contextualContext && ctx.contextualContext.markRows({}))
    },
      {
        isVisible: isPermissionAuthorized(WorkflowPermission.ViewCaseFlow),
        icon: "shuffle",
        iconColor: "green"
      }
    ));

    Finder.addSettings({
      queryName: CaseActivityEntity,
      defaultFilters: [
        { token: CaseActivityEntity.token(a => a.doneDate).expression("HasValue"), value: null, pinned: { active: "WhenHasValue", column: 1, label: "Is Done" } },
        { token: CaseActivityEntity.token(a => a.workflowActivity).cast(WorkflowActivityEntity), pinned: { active: "WhenHasValue", column: 2, label: () => WorkflowActivityEntity.niceName() } },
        { token: CaseActivityEntity.token(a => a.workflowActivity).cast(WorkflowActivityEntity).append(w => w.lane.pool.workflow), pinned: { active: "WhenHasValue", column: 3 } },
        { token: CaseActivityEntity.token(a => a.case), pinned: { active: "WhenHasValue", column: 4 } },
      ]
    })

    /*  QuickLinkClient.registerQuickLink({
        type: WorkflowEntity,
        key: getQueryKey(CaseEntity),
        generator: {
          factory: ctx => new QuickLinkExplore({ queryName: CaseEntity, filterOptions: [{ token: CaseEntity.token(e => e.workflow), value: ctx.lite }] }),
          options: {
            text: () => getQueryNiceName(CaseEntity),
            icon: "list-check", iconColor: "blue"
          }
        }
      });*/

    /*  QuickLinkClient.registerQuickLink_New(WorkflowEntity,
        new QuickLinkExplore(CaseEntity, ctx => ({ queryName: CaseEntity, filterOptions: [{ token: CaseEntity.token(e => e.workflow), value: ctx.lite }] })));*/

    OmniboxSpecialAction.registerSpecialAction({
      allowed: () => isPermissionAuthorized(WorkflowPermission.ViewWorkflowPanel),
      key: "WorkflowPanel",
      onClick: () => Promise.resolve("/workflow/panel")
    });

    Finder.addSettings({
      queryName: CaseActivityQuery.Inbox,
      hiddenColumns: [
        { token: CaseNotificationEntity.token(e => e.state) },
      ],
      rowAttributes: (row, sc) => {
        var rowState = sc.tryGetRowValue(row, CaseNotificationEntity.token(e => e.state));
        switch (rowState?.value) {
          case "New": return { className: "new-row" };
          case "Opened": return { className: "opened-row" };
          case "InProgress": return { className: "in-progress-row" };
          case "Done": return { className: "done-row" };
          case "DoneByOther": return { className: "done-by-other-row" };
          default: return {};
        };
      },
      formatters: {
        "Activity": new Finder.CellFormatter(cell => <ActivityWithRemarks data={cell} />, true),
        "MainEntity": new Finder.CellFormatter(cell => <span>{getToString(cell)}</span>, true),
        "Actor": new Finder.CellFormatter(cell => <span>{getToString(cell)}</span>, true),
        "Sender": new Finder.CellFormatter(cell => cell && <span>{getToString(cell)}</span>, true),
        "Workflow": new Finder.CellFormatter(cell => <span>{getToString(cell)}</span>, true),
      },
      defaultOrders: [{
        token: "StartDate",
        orderType: "Ascending"
      }],
      simpleFilterBuilder: sfbc => {
        var model = InboxFilter.extract(sfbc.initialFilterOptions);

        if (!model)
          return undefined;

        return <InboxFilter ctx={TypeContext.root(model)} />;
      }
    });

    Navigator.addSettings(new EntitySettings(CaseEntity, w => import('./Case/Case')));
    Navigator.addSettings(new EntitySettings(CaseTagTypeEntity, w => import('./Case/CaseTagType')));
    Navigator.addSettings(new EntitySettings(CaseTagsModel, w => import('./Case/CaseTagsModel')));

    Navigator.addSettings(new EntitySettings(CaseActivityEntity, undefined, {
      onNavigateRoute: (typeName, id) => "/workflow/activity/" + id,
      onView: (entityOrPack, options) => viewCase(isEntityPack(entityOrPack) ? entityOrPack.entity : entityOrPack, options),
    }));

    Operations.addSettings(new EntityOperationSettings(CaseNotificationOperation.SetRemarks, { isVisible: v => false }));

    Operations.addSettings(new EntityOperationSettings(CaseNotificationOperation.CreateCaseNotificationFromCaseActivity, {
      onClick: eoc => {
        eoc.onConstructFromSuccess = pack => {
          Operations.notifySuccess(); return Promise.resolve();
        }
        return Finder.find(UserEntity)
          .then(u => u && eoc.defaultClick(u))
      }
    }));

    Operations.addSettings(new EntityOperationSettings(CaseOperation.Delete, {
      commonOnClick: oc => oc.getEntity().then(e => askDeleteMainEntity(e.mainEntity))
        .then(u => u == undefined ? undefined : oc.defaultClick(u)),
      contextualFromMany: {
        onClick: coc => askDeleteMainEntity()
          .then(u => u == undefined ? undefined : coc.defaultClick(u))
      },
    }));

    function askDeleteMainEntity(mainEntity?: ICaseMainEntity): Promise<boolean | undefined> {
      return MessageModal.show({
        title: CaseMessage.DeleteMainEntity.niceToString(),
        message: mainEntity == null ? CaseMessage.DoYouWAntToAlsoDeleteTheMainEntities.niceToString() : CaseMessage.DoYouWAntToAlsoDeleteTheMainEntity0.niceToString(getToString(mainEntity)),
        buttons: "yes_no_cancel",
        style: "warning"
      }).then(u => u == "cancel" ? undefined : u == "yes")
    }


    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Delete, {
      hideOnCanExecute: true,
      isVisible: ctx => false,
      commonOnClick: oc => oc.getEntity().then(e => askDeleteMainEntity(e.case.mainEntity))
        .then(u => u == undefined ? undefined : oc.defaultClick(u)),
      contextualFromMany: {
        onClick: coc => askDeleteMainEntity()
          .then(u => u == undefined ? undefined : coc.defaultClick(u))
      },
    }));



    Operations.addSettings(new EntityOperationSettings(CaseOperation.SetTags, { isVisible: ctx => false }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Register, { hideOnCanExecute: true, color: "primary", onClick: eoc => executeCaseActivity(eoc, e => e.defaultClick()), }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Jump, {
      icon: "share",
      iconColor: "blue",
      hideOnCanExecute: true,
      onClick: eoc => executeCaseActivity(eoc, eoc => {
        eoc.onExecuteSuccess = pack => {
          Operations.notifySuccess();
          eoc.frame.onClose!(pack);
          Navigator.raiseEntityChanged(pack.entity);
          return Promise.resolve();
        }
        return getWorkflowJumpSelector(toLite(eoc.entity.workflowActivity as WorkflowActivityEntity))
          .then(dest => dest && eoc.defaultClick(dest));
      }),
      contextual: {
        isVisible: ctx => true,
        onClick: coc =>
          Navigator.API.fetch(coc.context.lites[0])
            .then(ca => getWorkflowJumpSelector(toLite(ca.workflowActivity as WorkflowActivityEntity)))
            .then(dest => dest && coc.defaultClick(dest))

      }
    }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.FreeJump, {
      icon: "share-from-square",
      color: "danger",
      iconColor: "#800080",
      hideOnCanExecute: true,
      onClick: eoc => executeCaseActivity(eoc, eoc => {
        eoc.onExecuteSuccess = async pack => {
          Operations.notifySuccess();
          eoc.frame.onClose!(pack);
          Navigator.raiseEntityChanged(pack.entity);
        }
        return getWorkflowFreeJump(eoc.entity.case.workflow)
          .then(dest => dest && eoc.defaultClick(dest));
      }),
      contextual: {
        isVisible: ctx => true,
        onClick: coc =>
          Navigator.API.fetch(coc.context.lites[0])
            .then(ca => getWorkflowFreeJump(ca.case.workflow))
            .then(dest => dest && coc.defaultClick(dest))
      }
    }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Timer, { isVisible: ctx => false }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.MarkAsUnread, {
      color: "dark",
      hideOnCanExecute: true,
      isVisible: ctx => false,
      contextual: { isVisible: ctx => true }
    }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.ScriptExecute, { isVisible: ctx => false }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.ScriptFailureJump, { isVisible: ctx => false }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.ScriptScheduleRetry, { isVisible: ctx => false }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.CreateCaseActivityFromWorkflow, { isVisible: ctx => false }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.CreateCaseFromWorkflowEventTask, { isVisible: ctx => false }));

    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Next, {
      hideOnCanExecute: true,
      color: "primary",
      onClick: eoc => executeCaseActivity(eoc, executeAndClose),
      createButton: (eoc, group) => {
        const wa = eoc.entity.workflowActivity as WorkflowActivityEntity;
        const s = eoc.settings;
        if (wa.type == "Task") {
          return [{
            order: s?.order ?? 0,
            shortcut: e => eoc.onKeyDown(e),
            button: wa.customNextButton == null ? <OperationButton eoc={eoc} group={group} />
              : <OperationButton eoc={eoc} group={group} color={wa.customNextButton.style.toLowerCase() as BsColor}
                onOperationClick={async () => {
                  if (registeredOnClick[wa.name] != undefined && registeredOnClick[wa.name][wa.customNextButton!.name] != undefined) {
                    registeredOnClick[wa.name][wa.customNextButton!.name].onClick(eoc);
                  }
                  else {
                    eoc.frame.execute(() =>
                      eoc.defaultClick(wa.customNextButton!.name)
                    );
                  }
                }}>{wa.customNextButton.name} </OperationButton>
          }];
        } else if (wa.type == "Decision") {
          return wa.decisionOptions.map(mle => ({
            order: s?.order ?? 0,
            shortcut: undefined,
            button: <OperationButton eoc={eoc} group={group}
              color={mle.element.style.toLowerCase() as BsColor}
              onOperationClick={async () => {

                if (registeredOnClick[wa.name] != undefined && registeredOnClick[wa.name][mle.element.name] != undefined) { 
                  registeredOnClick[wa.name][mle.element.name].onClick;
                }

                if (mle.element.withConfirmation) {
                  const answer = await MessageModal.show({
                    title: WorkflowActivityMessage.Confirmation.niceToString(),
                    message: WorkflowActivityMessage.AreYouSureYouWantToExecute0.niceToString(mle.element.name),
                    buttons: "yes_no",
                    style: "warning",
                  });

                  if (answer != "yes")
                    return;
                }

                eoc.frame.execute(() =>
                  eoc.defaultClick(mle.element.name)
                );

              }}>
              {mle.element.name}
            </OperationButton>,
          }));
        }
        else
          return [];
      },
      contextual:
      {
        settersConfig: coc => "NoDialog",
        isVisible: ctx => true,
        createMenuItems: coc => {

          const wa = coc.pack!.entity.workflowActivity as WorkflowActivityEntity;
          if (wa.type == "Task") {

            return [wa.customNextButton == null ? <ContextualOperations.OperationMenuItem coc={coc} />
              : <ContextualOperations.OperationMenuItem coc={coc} color={wa.customNextButton.style.toLowerCase() as BsColor}>{wa.customNextButton.name}</ContextualOperations.OperationMenuItem>];

          } else if (wa.type == "Decision") {
            return wa.decisionOptions.map(mle => <ContextualOperations.OperationMenuItem coc={coc} onOperationClick={() => coc.defaultClick(mle.element.name)} color={mle.element.style.toLowerCase() as BsColor}>{mle.element.name}</ContextualOperations.OperationMenuItem>);
          }
          else
            return [];
        }
      },
      contextualFromMany: {
        isVisible: ctx => true,
        color: "primary",
        createMenuItems: coc => {
          return [<CaseActivitiyOperations caseActivities={coc.context.lites} coc={coc} />]
        },
        settersConfig: coc => "NoDialog",
      },
    }));


    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Undo, {
      hideOnCanExecute: true,
      color: "danger",
      onClick: eoc => executeCaseActivity(eoc, executeAndClose),
      contextual: { isVisible: ctx => true },
      contextualFromMany: {
        isVisible: ctx => true,
        color: "danger"
      },
    }));

    QuickLinkClient.registerQuickLink(WorkflowEntity, new QuickLinkLink("bam", () => WorkflowActivityMonitorMessage.WorkflowActivityMonitor.niceToString(), ctx => workflowActivityMonitorUrl(ctx.lite), {
      icon: "gauge",
      iconColor: "green"
    }));

    Operations.addSettings(new EntityOperationSettings(WorkflowOperation.Save, { color: "primary", onClick: executeWorkflowSave, alternatives: eoc => [] }));
    Operations.addSettings(new EntityOperationSettings(WorkflowOperation.Delete, { contextualFromMany: { isVisible: ctx => false } }));
    Operations.addSettings(new EntityOperationSettings(WorkflowOperation.Activate, {
      contextual: { icon: "heart-pulse", iconColor: "red" },
      contextualFromMany: { icon: "heart-pulse", iconColor: "red" },
    }));
    Operations.addSettings(new EntityOperationSettings(WorkflowOperation.Deactivate, {
      onClick: eoc => chooseWorkflowExpirationDate([toLite(eoc.entity)]).then(val => !val ? undefined : eoc.defaultClick(val)),
      contextual: {
        onClick: coc => chooseWorkflowExpirationDate(coc.context.lites).then(val => !val ? undefined : coc.defaultClick(val)),
        icon: ["far", "heart"],
        iconColor: "gray"
      },
      contextualFromMany: {
        onClick: coc => chooseWorkflowExpirationDate(coc.context.lites).then(val => !val ? undefined : coc.defaultClick(val)),
        icon: ["far", "heart"],
        iconColor: "gray"
      },
    }));

    function chooseWorkflowExpirationDate(workflows: Lite<WorkflowEntity>[]): Promise<string | undefined> {
      return AutoLineModal.show({
        type: { name: "DateTime" },
        modalSize: "md",
        title: WorkflowMessage.DeactivateWorkflow.niceToString(),
        message:
          <div>
            <strong>{WorkflowMessage.PleaseChooseExpirationDate.niceToString()}</strong>
            <ul>{workflows.map((w, i) => <li key={i}>{getToString(w)}</li>)}</ul>
          </div>
      });
    }

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
    Navigator.addSettings(new EntitySettings(WorkflowConditionEntity, w => import('./Workflow/WorkflowCondition'), { modalSize: "xl" }));
    Navigator.addSettings(new EntitySettings(WorkflowTimerConditionEntity, w => import('./Workflow/WorkflowTimerCondition')));
    Navigator.addSettings(new EntitySettings(WorkflowActionEntity, w => import('./Workflow/WorkflowAction'), { modalSize: "xl" }));
    Navigator.addSettings(new EntitySettings(WorkflowScriptEntity, w => import('./Workflow/WorkflowScript')));
    Navigator.addSettings(new EntitySettings(WorkflowLaneModel, w => import('./Workflow/WorkflowLaneModel'), { modalSize: "xl" }));
    Navigator.addSettings(new EntitySettings(WorkflowEventModel, w => import('./Workflow/WorkflowEventModel')));
    Navigator.addSettings(new EntitySettings(WorkflowEventTaskEntity, w => import('./Workflow/WorkflowEventTask')));
    Navigator.addSettings(new EntitySettings(WorkflowScriptRetryStrategyEntity, w => import('./Workflow/WorkflowScriptRetryStrategy')));

    Constructor.registerConstructor(WorkflowEntity, props => WorkflowEntity.New({ mainEntityStrategies: [newMListElement(WorkflowMainEntityStrategy.value("CreateNew"))], ...props }));
    Constructor.registerConstructor(WorkflowConditionEntity, props => WorkflowConditionEntity.New({ eval: WorkflowConditionEval.New(), ...props }));
    Constructor.registerConstructor(WorkflowTimerConditionEntity, props => WorkflowTimerConditionEntity.New({ eval: WorkflowTimerConditionEval.New(), ...props }));
    Constructor.registerConstructor(WorkflowActionEntity, props => WorkflowActionEntity.New({ eval: WorkflowActionEval.New(), ...props }));
    Constructor.registerConstructor(WorkflowScriptEntity, props => WorkflowScriptEntity.New({ eval: WorkflowScriptEval.New(), ...props }));
    Constructor.registerConstructor(WorkflowTimerEmbedded, props => Constructor.construct(TimeSpanEmbedded).then(ts => ts && WorkflowTimerEmbedded.New({ duration: ts, ...props })));

    TypeHelpButtonBarComponent.getTypeHelpButtons.push(props => [({
      element: <WorkflowHelpComponent typeName={props.typeName} mode={props.mode} />,
      order: 0,
    })]);

    if (options.overrideCaseActivityMixin == true) {
      if (Navigator.isViewable(SMSMessageEntity))
        if (SMSMessageEntity.hasMixin(CaseActivityMixin))
          Navigator.getSettings(SMSMessageEntity)!.overrideView(vr => {
            vr.insertAfterLine(a => a.referred, ctx => [
              <EntityLine ctx={ctx.subCtx(CaseActivityMixin).subCtx(m => m.caseActivity)} readOnly={true} />
            ]);
          });

      if (Navigator.isViewable(EmailMessageEntity))
        if (EmailMessageEntity.hasMixin(CaseActivityMixin))
          Navigator.getSettings(EmailMessageEntity)!.overrideView(vr => {
            vr.insertAfterLine(a => a.target, ctx => [
              <EntityLine ctx={ctx.subCtx(CaseActivityMixin).subCtx(m => m.caseActivity)} readOnly={true} />
            ]);
          });
    }
  }


  export function workflowActivityMonitorUrl
    (workflow: Lite<WorkflowEntity>) {
    return `/workflow/activityMonitor/${workflow.id}`;
  }

  export function workflowStartUrl(lite: Lite<WorkflowEntity>, strategy?: WorkflowMainEntityStrategy): string {
    return "/workflow/new/" + lite.id + (strategy == null ? "" : ("/" + strategy));
  }

  export function getCaseActivityContext(ctx: TypeContext<any>): TypeContext<CaseActivityEntity> | undefined {
    const f = ctx.frame;
    const fc = f?.frameComponent as any;
    const activity = fc?.getCaseActivity && fc.getCaseActivity() as CaseActivityEntity;
    return activity && TypeContext.root(activity, undefined, ctx);
  }

  export function getDefaultInboxUrl(): string {
    return Finder.findOptionsPath({
      queryName: CaseActivityQuery.Inbox,
      filterOptions: [{
        token: CaseNotificationEntity.token(e => e.state),
        operation: "IsIn",
        value: ["New", "Opened", "InProgress"]
      }]
    });
  }

  export function showWorkflowTransitionContextCodeHelp(): void {

    var value = `public CaseActivityEntity PreviousCaseActivity { get; internal set; }
  public DecisionResult? DecisionResult { get; internal set; }
  public IWorkflowTransition Connection { get; internal set; }
  public CaseEntity Case { get; set; }
  
  public interface IWorkflowTransition
  {
      Lite<WorkflowConditionEntity> Condition { get; }
      Lite<WorkflowActionEntity> Action { get; }
  }`;

    AutoLineModal.show({
      type: { name: "string" },
      initialValue: value,
      customComponent: props => <TextAreaLine {...props} />,
      title: "WorkflowTransitionContext Members",
      message: "Copy to clipboard: Ctrl+C, ESC",
      valueHtmlAttributes: { style: { height: 215 } },
    });
  }


  function hide<T extends Entity>(type: Type<T>) {
    Navigator.addSettings(new EntitySettings(type, undefined, { isViewable: "Never", isCreable: "Never" }));
  }

  export function executeCaseActivity(eoc: EntityOperationContext<CaseActivityEntity>, defaultOnClick: (eoc: EntityOperationContext<CaseActivityEntity>) => Promise<void>): Promise<void> {
    const op = customOnClicks[eoc.operationInfo.key];

    const onClick = op && op[eoc.entity.case.mainEntity.Type];

    if (onClick)
      return onClick(eoc);
    else
      return EntityOperations.defaultOnClick(eoc);
  }

  export function executeWorkflowSave(eoc: EntityOperationContext<WorkflowEntity>): Promise<void> {


    function saveAndSetErrors(entity: WorkflowEntity, model: WorkflowModel, replacementModel: WorkflowReplacementModel | undefined): Promise<void> {
      return API.saveWorkflow(entity, model, replacementModel)
        .then(packWithIssues => {
          eoc.frame.onReload(packWithIssues.entityPack);
          wf.setIssues(packWithIssues.issues);
          Operations.notifySuccess();
        })
        .catch(ifError(ValidationError, e => {

          var issuesString = e.modelState["workflowIssues"];
          if (issuesString) {
            wf.setIssues(JSON.parse(issuesString[0]));
            delete e.modelState["workflowIssues"];
          }
          eoc.frame.setError(e.modelState, "entity");

        }));
    }

    let wf = FunctionalAdapter.innerRef(eoc.frame.entityComponent) as WorkflowHandle;
    return wf.getXml()
      .then(xml => {
        var wfModel = WorkflowModel.New({
          diagramXml: xml,
          entities: Dic.map(wf.workflowState!.entities, (bpmnId, model) => newMListElement(BpmnEntityPairEmbedded.New({
            bpmnElementId: bpmnId,
            model: model
          })))
        });

        var promise = eoc.entity.isNew ?
          Promise.resolve<WorkflowReplacementModel | undefined>(undefined) :
          API.previewChanges(toLite(eoc.entity), wfModel);

        return promise.then(repoModel => {
          if (!repoModel || repoModel.replacements.length == 0)
            return saveAndSetErrors(eoc.entity, wfModel, undefined);
          else
            return Navigator.view(repoModel).then(replacementModel => {
              if (!replacementModel)
                return;

              return saveAndSetErrors(eoc.entity, wfModel, replacementModel);
            });
        });
      });
  }


  function getWorkflowJumpSelector(activity: Lite<WorkflowActivityEntity>): Promise<Lite<IWorkflowNodeEntity> | undefined> {

    return API.nextConnections({ workflowActivity: activity, connectionType: "Jump" })
      .then(jumps => SelectorModal.chooseElement(jumps,
        {
          title: WorkflowActivityMessage.ChooseADestinationForWorkflowJumping.niceToString(),
          buttonDisplay: a => getToString(a) ?? "",
          forceShow: true
        }));
  }

  function getWorkflowFreeJump(workflow: WorkflowEntity): Promise<Lite<WorkflowEntity> | undefined> {

    return Finder.find({
      queryName: WorkflowActivityEntity,
      filterOptions: [{ token: WorkflowActivityEntity.token(w => w.entity.lane.pool.workflow), value: workflow }]
    }, {
      message: <span className="text-danger">FreeJump is an unrestricted but dangerous operation! If you don't know what you're doing... don't do it!</span>
    });
  }

  export function executeAndClose(eoc: EntityOperationContext<CaseActivityEntity>): Promise<void> {

    return EntityOperations.confirmInNecessary(eoc).then(conf => {
      if (!conf)
        return;

      return Operations.API.executeEntity(eoc.entity, eoc.operationInfo.key)
        .then(pack => {
          eoc.frame.onClose!();
          Navigator.raiseEntityChanged(pack.entity);
          return Operations.notifySuccess();
        })
        .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "entity")));
    });
  }


  export function viewCase(entityOrPack: Lite<CaseActivityEntity> | CaseActivityEntity | CaseEntityPack, options?: { readOnly?: boolean }): Promise<CaseActivityEntity | undefined> {
    return import("./Case/CaseFrameModal")
      .then(NP => NP.CaseFrameModalManager.openView(entityOrPack, options));

  }

  export const customSelectByUser: { [typeName: string]: (workflow: Lite<WorkflowEntity>, strategy: WorkflowMainEntityStrategy) => Promise<Lite<Entity> | undefined> } = {};
  export function registerCustomSelectByUser<T extends Entity>(type: Type<T>, selectEntity: (workflow: Lite<WorkflowEntity>, strategy: WorkflowMainEntityStrategy) => Promise<Lite<T> | undefined>): void {
    customSelectByUser[type.typeName] = selectEntity;
  }

  export function createNewCase(workflowId: number | string, mainEntityStrategy: WorkflowMainEntityStrategy): Promise<CaseEntityPack | undefined> {
    return Navigator.API.fetchEntity(WorkflowEntity, workflowId)
      .then(wf => {
        if (mainEntityStrategy == "CreateNew")
          return Operations.API.constructFromEntity(wf, CaseActivityOperation.CreateCaseActivityFromWorkflow);

        var coi: OperationInfo;

        if (mainEntityStrategy == "Clone") {
          coi = getOperationInfo(`${wf.mainEntityType!.cleanName}Operation.Clone`, wf.mainEntityType!.cleanName);
        }

        const typeName = wf.mainEntityType!.cleanName;
        const promise = customSelectByUser[typeName]?.(toLite(wf), mainEntityStrategy) ?? Finder.find({ queryName: typeName });
        return promise
          .then(lite => {
            if (!lite)
              return Promise.resolve(undefined);

            return Navigator.API.fetch(lite!)
              .then(entity => {
                if (mainEntityStrategy == "Clone") {
                  return Operations.API.constructFromEntity(entity, coi.key)
                    .then(pack => Operations.API.constructFromEntity(wf, CaseActivityOperation.CreateCaseActivityFromWorkflow, pack!.entity));
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

  export const customOnClicks: { [operationKey: string]: { [typeName: string]: (ctx: EntityOperationContext<CaseActivityEntity>) => Promise<void> } } = {};

  export function registerOnClick<T extends ICaseMainEntity>(type: Type<T>, operationKey: ExecuteSymbol<CaseActivityEntity>, action: (ctx: EntityOperationContext<CaseActivityEntity>) => Promise<void>): void {
    var op = customOnClicks[operationKey.key] || (customOnClicks[operationKey.key] = {});
    op[type.typeName] = action;
  }

  export interface IHasCaseActivity {
    getCaseActivity(): CaseActivityEntity | undefined;
  }

  export function inWorkflow(ctx: TypeContext<any>, workflowName: string, activityName: string): boolean {
    var f = ctx.frame && ctx.frame.frameComponent as any as IHasCaseActivity;

    var ca = f?.getCaseActivity && f.getCaseActivity();

    if (!ca)
      return false;

    var wa = ca.workflowActivity as WorkflowActivityEntity;

    return wa.lane!.pool!.workflow!.name == workflowName && wa.name == activityName;
  }


  export function getViewPromiseCompoment(ca: CaseActivityEntity): Promise<(ctx: TypeContext<ICaseMainEntity>) => React.ReactElement<any>> {

    const wa = ca.workflowActivity as WorkflowActivityEntity;

    var viewPromise = Navigator.viewDispatcher.getViewPromise(ca.case.mainEntity, wa.viewName ?? undefined);

    if (wa.viewNameProps.length) {
      var props = wa.viewNameProps.toObject(a => a.element.name, a => !a.element.expression ? undefined : eval(a.element.expression));
      viewPromise = viewPromise.withProps(props);
    }

    return viewPromise.promise;
  }

  const intFormatter = toNumberFormat("D");

  export function formatDuration(d: Duration): string {

    var a = {
      years: "yr",
      quarters: "qua",
      months: "mon",
      weeks: "weeks",
      days: "d",
      hours: "h",
      minutes: "m",
      seconds: "s",
      milliseconds: "ms",
    };

    var result = Object.entries(a).map(([key, label]) => d.get(key as DurationUnit) == 0 ? null : intFormatter.format(d.get(key as DurationUnit)) + label).notNull().join(" ");

    return result;
  }

  export namespace API {
    export function fetchActivityForViewing(caseActivity: Lite<CaseActivityEntity>): Promise<CaseEntityPack> {
      return ajaxGet({ url: `/api/workflow/fetchForViewing/${caseActivity.id}` });
    }

    export function fetchCaseFlowPack(caseActivity: Lite<CaseActivityEntity>): Promise<CaseFlowEntityPack> {
      return ajaxGet({ url: `/api/workflow/caseFlowPack/${caseActivity.id}` });
    }

    export function fetchCaseTags(caseLite: Lite<CaseEntity>): Promise<CaseTagTypeEntity[]> {
      return ajaxGet({ url: `/api/workflow/tags/${caseLite.id}` });
    }

    export function starts(): Promise<Array<WorkflowEntity>> {
      return ajaxGet({ url: `/api/workflow/starts` });
    }

    export function getWorkflowModel(workflow: Lite<WorkflowEntity>): Promise<WorkflowModelAndIssues> {
      return ajaxGet({ url: `/api/workflow/workflowModel/${workflow.id}` });
    }

    interface WorkflowModelAndIssues {
      model: WorkflowModel;
      issues: Array<WorkflowIssue>;
    }

    export function previewChanges(workflow: Lite<WorkflowEntity>, model: WorkflowModel): Promise<WorkflowReplacementModel> {
      return ajaxPost({ url: `/api/workflow/previewChanges/${workflow.id} ` }, model);
    }

    export function saveWorkflow(entity: WorkflowEntity, model: WorkflowModel, replacementModel: WorkflowReplacementModel | undefined): Promise<EntityPackWithIssues> {
      GraphExplorer.propagateAll(entity, model, replacementModel);
      return ajaxPost({ url: "/api/workflow/save" }, { entity: entity, operationKey: WorkflowOperation.Save.key, args: [model, replacementModel] } as Operations.API.EntityOperationRequest);
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
      return ajaxGet({
        url: "/api/workflow/findMainEntityType?" + QueryString.stringify(request),
        signal
      });
    }

    export function findNode(request: WorkflowFindNodeRequest, signal?: AbortSignal): Promise<Lite<IWorkflowNodeEntity>[]> {
      return ajaxPost({ url: "/api/workflow/findNode", signal }, request);
    }

    export function conditionTest(request: WorkflowConditionTestRequest): Promise<WorkflowConditionTestResponse> {
      return ajaxPost({ url: `/api/workflow/condition/test` }, request);
    }

    export function view(): Promise<WorkflowScriptRunnerState> {
      return ajaxGet({ url: "/api/workflow/scriptRunner/view" });
    }

    export function start(): Promise<void> {
      return ajaxPost({ url: "/api/workflow/scriptRunner/start" }, undefined);
    }

    export function stop(): Promise<void> {
      return ajaxPost({ url: "/api/workflow/scriptRunner/stop" }, undefined);
    }

    export function caseFlow(c: Lite<CaseEntity>): Promise<CaseFlow> {
      return ajaxGet({ url: `/api/workflow/caseFlow/${c.id}` });
    }

    export function workflowActivityMonitor(request: WorkflowActivityMonitorRequest): Promise<WorkflowActivityMonitor> {
      return ajaxPost({ url: "/api/workflow/activityMonitor" }, request);
    }

    export function nextConnections(request: NextConnectionsRequest): Promise<Array<Lite<IWorkflowNodeEntity>>> {
      return ajaxPost({ url: "/api/workflow/nextConnections" }, request);
    }

    export function getOnlyWorkflowActivity(caseActivities: Lite<CaseActivityEntity>[]): Promise<WorkflowActivityEntity | null> {
      return ajaxPost({ url: "/api/workflow/onlyWorkflowActivity" }, caseActivities);
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

  export const DecisionResultValues: string[] = ["Approve", "Decline"];

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
    running: boolean;
    initialDelayMilliseconds: number | null;
    scriptRunnerPeriod: number;
    isCancelationRequested: boolean;
    nextPlannedExecution: string;
    queuedItems: number;
    currentProcessIdentifier: string;
  }

  export interface CaseActivityStats {
    caseActivity: Lite<CaseActivityEntity>;
    previousActivity: Lite<CaseActivityEntity>;
    workflowActivity: Lite<WorkflowActivityEntity>;
    workflowActivityType?: WorkflowActivityType;
    workflowEventType?: WorkflowEventType;
    subWorkflow?: Lite<WorkflowEntity>;
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

  export interface CaseFlowEntityPack {
    pack: EntityPack<CaseEntity>,
    workflowActivity: IWorkflowNodeEntity;
  }

  export function CaseActivitiyOperations(p: { caseActivities: Lite<CaseActivityEntity>[], coc: ContextualOperationContext<CaseActivityEntity> }): React.JSX.Element | null {

    const wa = useAPI(() => WorkflowClient.API.getOnlyWorkflowActivity(p.caseActivities), [p.caseActivities]);

    if (wa === undefined)
      return <div>{JavascriptMessage.loading.niceToString()}</div>;

    if (wa === null)
      return null;

    if (wa.type == "Task") {
      return (
        <ContextualOperations.OperationMenuItem coc={p.coc} color={wa.customNextButton?.style.toLowerCase() as BsColor}>
          {wa.customNextButton?.name}
        </ContextualOperations.OperationMenuItem>
      );
    }

    if (wa.type == "Decision") {
      return (<>
        {wa.decisionOptions.map(mle => <ContextualOperations.OperationMenuItem onOperationClick={() => p.coc.defaultClick(mle.element.name)}
          coc={p.coc} color={mle.element.style.toLowerCase() as BsColor}>{mle.element.name}</ContextualOperations.OperationMenuItem>)}
      </>);
    }

    return null;
  }
}
