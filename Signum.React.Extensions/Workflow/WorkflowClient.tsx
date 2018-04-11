import * as React from 'react'
import { Route } from 'react-router'
import * as QueryString from 'query-string';
import { ifError, Dic } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxGet, ValidationError } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise, ViewModule } from '../../../Framework/Signum.React/Scripts/Navigator'
import {
    EntityPack, Lite, toLite, MListElement, JavascriptMessage, EntityControlMessage,
    newMListElement, liteKey, getMixin, Entity, ExecuteSymbol, isEntityPack, isEntity
} from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeEntity, IUserEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { Type, PropertyRoute } from '../../../Framework/Signum.React/Scripts/Reflection'
import { EntityFrame, TypeContext } from '../../../Framework/Signum.React/Scripts/TypeContext'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { EntityOperationSettings, addSettings, EntityOperationContext } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { confirmInNecessary, notifySuccess } from '../../../Framework/Signum.React/Scripts/Operations/EntityOperations'

import * as EntityOperations from '../../../Framework/Signum.React/Scripts/Operations/EntityOperations'
import * as ContextualOperations from '../../../Framework/Signum.React/Scripts/Operations/ContextualOperations'

import { UserEntity } from '../Authorization/Signum.Entities.Authorization'
import * as DynamicViewClient from '../Dynamic/DynamicViewClient'
import { CodeContext } from '../Dynamic/View/NodeUtils'
import { TimeSpanEmbedded } from '../Basics/Signum.Entities.Basics'
import TypeHelpButtonBarComponent from '../TypeHelp/TypeHelpButtonBarComponent'

import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '../../../Framework/Signum.React/Scripts/Lines'
import { WorkflowConditionEval, WorkflowActionEval, WorkflowJumpEmbedded, DecisionResult, WorkflowMessage, WorkflowActivityMonitorMessage } from './Signum.Entities.Workflow'

import ActivityWithRemarks from './Case/ActivityWithRemarks'




import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import SelectorModal from '../../../Framework/Signum.React/Scripts/SelectorModal'
import ValueLineModal from '../../../Framework/Signum.React/Scripts/ValueLineModal'
import {
    WorkflowEntity, WorkflowLaneEntity, WorkflowActivityEntity, WorkflowConnectionEntity, WorkflowConditionEntity, WorkflowActionEntity, CaseActivityQuery, CaseActivityEntity,
    CaseActivityOperation, CaseEntity, CaseNotificationEntity, CaseNotificationState, InboxFilterModel, WorkflowOperation, WorkflowPoolEntity, WorkflowScriptEntity, WorkflowScriptEval,
    WorkflowActivityOperation, WorkflowReplacementModel, WorkflowModel, BpmnEntityPairEmbedded, WorkflowActivityModel, ICaseMainEntity, WorkflowGatewayEntity, WorkflowEventEntity,
    WorkflowLaneModel, WorkflowConnectionModel, IWorkflowNodeEntity, WorkflowActivityMessage, WorkflowTimerEmbedded, CaseTagEntity, CaseTagsModel, CaseTagTypeEntity,
    WorkflowScriptRunnerPanelPermission, WorkflowEventModel, WorkflowEventTaskEntity, DoneType, CaseOperation, WorkflowMainEntityStrategy, WorkflowActivityType
} from './Signum.Entities.Workflow'

import InboxFilter from './Case/InboxFilter'
import Workflow from './Workflow/Workflow'
import * as AuthClient from '../Authorization/AuthClient'
import * as OmniboxClient from '../Omnibox/OmniboxClient'

import { ImportRoute } from "../../../Framework/Signum.React/Scripts/AsyncImport";

import { SearchControl } from "../../../Framework/Signum.React/Scripts/Search";
import { getTypeInfo } from "../../../Framework/Signum.React/Scripts/Reflection";
import WorkflowHelpComponent from './Workflow/WorkflowHelpComponent';
import { globalModules } from '../Dynamic/View/GlobalModules';
import { FilterRequest, ColumnRequest } from '../../../Framework/Signum.React/Scripts/FindOptions';
import { BsColor } from '../../../Framework/Signum.React/Scripts/Components/Basic';

export function start(options: { routes: JSX.Element[] }) {

    options.routes.push(
        <ImportRoute path="~/workflow/activity/:caseActivityId" onImportModule={() => import("./Case/CaseFramePage")} />,
        <ImportRoute path="~/workflow/new/:workflowId/:mainEntityStrategy" onImportModule={() => import("./Case/CaseFramePage")} />,
        <ImportRoute path="~/workflow/panel" onImportModule={() => import("./Workflow/WorkflowScriptRunnerPanelPage")} />,
        <ImportRoute path="~/workflow/activityMonitor/:workflowId" onImportModule={() => import("./ActivityMonitor/WorkflowActivityMonitorPage")} />,
    );

    QuickLinks.registerQuickLink(CaseActivityEntity, ctx => [
        new QuickLinks.QuickLinkAction("caseFlow", WorkflowActivityMessage.CaseFlow.niceToString(), e => {
            Navigator.API.fetchAndForget(ctx.lite)
                .then(ca => Navigator.navigate(ca.case, { extraComponentProps: { caseActivity: ca } }))
                .then(() => ctx.contextualContext && ctx.contextualContext.markRows({}))
                .done();
        }, { icon: "fa fa-random", iconColor: "green" })
    ]);
    
    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(WorkflowScriptRunnerPanelPermission.ViewWorkflowScriptRunnerPanel),
        key: "WorkflowScriptRunnerPanel",
        onClick: () => Promise.resolve("~/workflow/panel")
    });

    Finder.addSettings({
        queryName: CaseActivityQuery.Inbox,
        hiddenColumns: [
            { columnName: "State" },
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
    caseActivityOperation(CaseActivityOperation.Reject, "secondary");

    QuickLinks.registerQuickLink(WorkflowEntity, ctx => new QuickLinks.QuickLinkLink("bam",
        WorkflowActivityMonitorMessage.WorkflowActivityMonitor.niceToString(),
        workflowActivityMonitorUrl(ctx.lite),
        { icon: "fa fa-tachometer", iconColor: "green" }));

    Operations.addSettings(new EntityOperationSettings(WorkflowOperation.Save, { color: "primary", onClick: executeWorkflowSave }));
    Operations.addSettings(new EntityOperationSettings(WorkflowOperation.Delete, { contextualFromMany: { isVisible: ctx => false } }));
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
    Navigator.addSettings(new EntitySettings(WorkflowActionEntity, w => import('./Workflow/WorkflowAction')));
    Navigator.addSettings(new EntitySettings(WorkflowScriptEntity, w => import('./Workflow/WorkflowScript')));
    Navigator.addSettings(new EntitySettings(WorkflowLaneModel, w => import('./Workflow/WorkflowLaneModel')));
    Navigator.addSettings(new EntitySettings(WorkflowEventModel, w => import('./Workflow/WorkflowEventModel')));
    Navigator.addSettings(new EntitySettings(WorkflowEventTaskEntity, w => import('./Workflow/WorkflowEventTask')));

    Constructor.registerConstructor(WorkflowEntity, () => WorkflowEntity.New({ mainEntityStrategy: WorkflowMainEntityStrategy.value("CreateNew") }));
    Constructor.registerConstructor(WorkflowConditionEntity, () => WorkflowConditionEntity.New({ eval: WorkflowConditionEval.New() }));
    Constructor.registerConstructor(WorkflowActionEntity, () => WorkflowActionEntity.New({ eval: WorkflowActionEval.New() }));
    Constructor.registerConstructor(WorkflowScriptEntity, () => WorkflowScriptEntity.New({ eval: WorkflowScriptEval.New() }));
    Constructor.registerConstructor(WorkflowTimerEmbedded, () => Constructor.construct(TimeSpanEmbedded).then(ep => ep && WorkflowTimerEmbedded.New({ duration: ep.entity })));

    registerCustomContexts();

    TypeHelpButtonBarComponent.getTypeHelpButtons.push(props => [({
        element: <WorkflowHelpComponent typeName={props.typeName} mode={props.mode} />,
        order: 0,
    })]);
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
            columnName: "State",
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
                if (!pr || pr.Model.replacements.length == 0)
                    eoc.defaultClick(model);
                else
                    Navigator.view(pr.Model, { extraComponentProps: { previewTasks: pr.NewTasks } }).then(replacementModel => {
                        if (!replacementModel)
                            return;

                        eoc.defaultClick(model, replacementModel);
                    }).done();
            }).done();
        }).done();
}

export function executeWorkflowJumpContextual(coc: Operations.ContextualOperationContext<CaseActivityEntity>) {

    Navigator.API.fetchAndForget(coc.context.lites[0])
        .then(ca => {
            const jumps = ca.workflowActivity.jumps;

            getWorkflowJumpSelector(jumps)
                .then(dest => dest && coc.defaultContextualClick(dest.to));
        })
        .done();
}

export function executeWorkflowJump(eoc: Operations.EntityOperationContext<CaseActivityEntity>) {

    eoc.closeRequested = true;
    var jumps = eoc.entity.workflowActivity.jumps;

    getWorkflowJumpSelector(jumps)
        .then(dest => dest && eoc.defaultClick(dest.to))
        .done();
}

function getWorkflowJumpSelector(jumps: MListElement<WorkflowJumpEmbedded>[]): Promise<WorkflowJumpEmbedded | undefined> {

    var opts = jumps.map(j => j.element);
    return SelectorModal.chooseElement(opts,
        {
            title: WorkflowActivityMessage.ChooseADestinationForWorkflowJumping.niceToString(),
            buttonDisplay: a => a.to!.toStr || "",
            forceShow: true
        });
}

export function executeAndClose(eoc: Operations.EntityOperationContext<CaseActivityEntity>) {

    confirmInNecessary(eoc).then(conf => {
        if (!conf)
            return;

        Operations.API.executeEntity(eoc.entity, eoc.operationInfo.key)
            .then(pack => { eoc.frame.onClose(); return notifySuccess(); })
            .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "request.entity")))
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
            if (mainEntityStrategy == "SelectByUser")
                return Finder.find({ queryName: wf.mainEntityType!.cleanName })
                    .then(lite => {
                        if (!lite)
                            return Promise.resolve(undefined);

                        return Navigator.API.fetchAndForget(lite!)
                            .then(entity => Operations.API.constructFromEntity(wf, CaseActivityOperation.CreateCaseActivityFromWorkflow, entity))
                    });

            return Operations.API.constructFromEntity(wf, CaseActivityOperation.CreateCaseActivityFromWorkflow);
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

    return ca.workflowActivity.lane!.pool!.workflow!.name == workflowName && ca.workflowActivity.name == activityName;
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

    export function getWorkflowModel(workflow: Lite<WorkflowEntity>): Promise<WorkflowModel> {
        return ajaxGet<WorkflowModel>({ url: `~/api/workflow/workflowModel/${workflow.id} ` });
    }

    export function previewChanges(workflow: Lite<WorkflowEntity>, model: WorkflowModel): Promise<PreviewResult> {
        return ajaxPost<PreviewResult>({ url: `~/api/workflow/previewChanges/${workflow.id} ` }, model);
    }

    export function findMainEntityType(request: { subString: string, count: number }, abortController?: FetchAbortController): Promise<Lite<TypeEntity>[]> {
        return ajaxGet<Lite<TypeEntity>[]>({
            url: "~/api/workflow/findMainEntityType?" + QueryString.stringify(request),
            abortController
        });
    }

    export function findNode(request: WorkflowFindNodeRequest, abortController?: FetchAbortController): Promise<Lite<IWorkflowNodeEntity>[]> {
        return ajaxPost<Lite<IWorkflowNodeEntity>[]>({ url: "~/api/workflow/findNode", abortController }, request);
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
    decisionResult?: DecisionResult;
}

export interface WorkflowConditionTestResponse {
    compileError?: string;
    validationException?: string;
    validationResult?: boolean;
}

export const DecisionResultValues = ["Approve", "Decline"];

export interface PreviewResult {
    Model: WorkflowReplacementModel;
    NewTasks: PreviewTask[];
}

export interface PreviewTask {
    BpmnId: string;
    Name: string;
    SubWorkflow: Lite<WorkflowEntity>;
}

export interface CaseEntityPack {
    activity: CaseActivityEntity;
    canExecuteActivity: { [key: string]: string };
    canExecuteMainEntity: { [key: string]: string };
}

export interface WorkflowScriptRunnerState {
    ScriptRunnerPeriod: number;
    Running: boolean;
    IsCancelationRequested: boolean;
    NextPlannedExecution: string;
    QueuedItems: number;
    CurrentProcessIdentifier: string;
}

export interface CaseActivityStats {
    CaseActivity: Lite<CaseActivityEntity>;
    PreviousActivity: Lite<CaseActivityEntity>;
    WorkflowActivity: Lite<WorkflowActivityEntity>;
    WorkflowActivityType: WorkflowActivityType;
    SubWorkflow: Lite<WorkflowEntity>;
    Notifications: number;
    StartDate: string;
    DoneDate?: string;
    DoneType?: DoneType;
    DoneBy: Lite<IUserEntity>;
    Duration?: number;
    AverageDuration?: number;
    EstimatedDuration?: number;

}
export interface CaseConnectionStats {
    Connection?: Lite<WorkflowConnectionEntity>;
    DoneDate: string;
    DoneBy: Lite<IUserEntity>;
    DoneType: DoneType;

    BpmnElementId?: string;
    FromBpmnElementId: string;
    ToBpmnElementId: string;
}

export interface CaseFlow {
    Activities: { [bpmnElementId: string]: CaseActivityStats[] };
    Connections: { [bpmnElementId: string]: CaseConnectionStats[] };
    Jumps: CaseConnectionStats[];
    AllNodes: string[];
}

export interface WorkflowActivityMonitorRequest {
    workflow: Lite<WorkflowEntity>;
    filters: FilterRequest[];
    columns: ColumnRequest[];
}
    
export interface WorkflowActivityStats {
    WorkflowActivity: Lite<WorkflowActivityEntity>;
    CaseActivityCount: number;
    CustomValues: any[];
}

export interface WorkflowActivityMonitor {
    Workflow: Lite<WorkflowEntity>;
    CustomColumns: string[];
    Activities: WorkflowActivityStats[];
}

