import * as React from 'react'
import { Route, Link } from 'react-router'
import { ifError, Dic } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxGet, ValidationError } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import {
    EntityPack, Lite, toLite, MListElement, JavascriptMessage, EntityControlMessage,
    newMListElement, liteKey, getMixin, Entity, ExecuteSymbol, isEntityPack, isEntity
} from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeEntity, IUserEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { Type, PropertyRoute } from '../../../Framework/Signum.React/Scripts/Reflection'
import { EntityFrame, TypeContext } from '../../../Framework/Signum.React/Scripts/TypeContext'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { EntityOperationSettings, addSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { confirmInNecessary, notifySuccess } from '../../../Framework/Signum.React/Scripts/Operations/EntityOperations'

import * as EntityOperations from '../../../Framework/Signum.React/Scripts/Operations/EntityOperations'
import * as ContextualOperations from '../../../Framework/Signum.React/Scripts/Operations/ContextualOperations'

import { UserEntity } from '../../../Extensions/Signum.React.Extensions/Authorization/Signum.Entities.Authorization'
import * as DynamicViewClient from '../../../Extensions/Signum.React.Extensions/Dynamic/DynamicViewClient'
import { CodeContext } from '../../../Extensions/Signum.React.Extensions/Dynamic/View/NodeUtils'
import { TimeSpanEmbedded } from '../../../Extensions/Signum.React.Extensions/Basics/Signum.Entities.Basics'

import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '../../../Framework/Signum.React/Scripts/Lines'
import { WorkflowConditionEval, WorkflowActionEval, WorkflowJumpEmbedded, DecisionResult } from './Signum.Entities.Workflow'

import ActivityWithRemarks from './Case/ActivityWithRemarks'
import CaseFrameModal from './Case/CaseFrameModal'
export { CaseFrameModal };

import CaseFramePage from './Case/CaseFrameModal'
export { CaseFramePage };

import * as QuickLinks from '../../../Framework/Signum.React/Scripts/QuickLinks'
import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'
import SelectorModal from '../../../Framework/Signum.React/Scripts/SelectorModal'
import ValueLineModal from '../../../Framework/Signum.React/Scripts/ValueLineModal'

import {
    WorkflowEntity, WorkflowLaneEntity, WorkflowActivityEntity, WorkflowConnectionEntity, WorkflowConditionEntity, WorkflowActionEntity, CaseActivityQuery, CaseActivityEntity,
    CaseActivityOperation, CaseEntity, CaseNotificationEntity, CaseNotificationState, InboxFilterModel, WorkflowOperation, WorkflowPoolEntity, WorkflowScriptEntity, WorkflowScriptEval,
    WorkflowActivityOperation, WorkflowReplacementModel, WorkflowModel, BpmnEntityPairEmbedded, WorkflowActivityModel, ICaseMainEntity, WorkflowGatewayEntity, WorkflowEventEntity,
    WorkflowLaneModel, WorkflowConnectionModel, IWorkflowNodeEntity, WorkflowActivityMessage, WorkflowTimeoutEmbedded, CaseTagEntity, CaseTagsModel, CaseTagTypeEntity,
    WorkflowScriptRunnerPanelPermission, WorkflowEventModel, WorkflowEventTaskEntity, DoneType, CaseOperation, WorkflowMainEntityStrategy, WorkflowActivityType
} from './Signum.Entities.Workflow'

import InboxFilter from './Case/InboxFilter'
import Workflow from './Workflow/Workflow'
import * as AuthClient from '../Authorization/AuthClient'
import * as OmniboxClient from '../Omnibox/OmniboxClient'
import { SearchControl } from "../../../Framework/Signum.React/Scripts/Search";
import { getTypeInfo } from "../../../Framework/Signum.React/Scripts/Reflection";

export function start(options: { routes: JSX.Element[] }) {

    options.routes.push(<Route path="workflow">
        <Route path="activity/:caseActivityId" getComponent={(loc, cb) => require(["./Case/CaseFramePage"], (Comp) => cb(null, Comp.default))} />
        <Route path="new/:workflowId/:mainEntityStrategy" getComponent={(loc, cb) => require(["./Case/CaseFramePage"], (Comp) => cb(null, Comp.default))} />
    </Route>);

    QuickLinks.registerQuickLink(CaseActivityEntity, ctx => [
        new QuickLinks.QuickLinkAction("caseFlow", WorkflowActivityMessage.CaseFlow.niceToString(), e => {
            Navigator.API.fetchAndForget(ctx.lite)
                .then(ca => Navigator.navigate(ca.case, { extraComponentProps: { caseActivity: ca } }))
                .then(() => ctx.contextualContext && ctx.contextualContext.markRows({}))
                .done();
        }, { icon: "fa fa-random", iconColor: "green" })
    ]);

    options.routes.push(<Route path="workflow">
        <Route path="panel" getComponent={(loc, cb) => require(["./Workflow/WorkflowScriptRunnerPanelPage"], (Comp) => cb(undefined, Comp.default))} />
    </Route>);

    Link

    OmniboxClient.registerSpecialAction({
        allowed: () => AuthClient.isPermissionAuthorized(WorkflowScriptRunnerPanelPermission.ViewWorkflowScriptRunnerPanel),
        key: "WorkflowScriptRunnerPanel",
        onClick: () => Promise.resolve(Navigator.currentHistory.createHref("~/workflow/panel"))
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

    Navigator.addSettings(new EntitySettings(CaseEntity, w => new ViewPromise(m => require(['./Case/Case'], m))));
    Navigator.addSettings(new EntitySettings(CaseTagTypeEntity, w => new ViewPromise(m => require(['./Case/CaseTagType'], m))));
    Navigator.addSettings(new EntitySettings(CaseTagsModel, w => new ViewPromise(m => require(['./Case/CaseTagsModel'], m))));

    Navigator.addSettings(new EntitySettings(CaseActivityEntity, undefined, {
        onNavigateRoute: (typeName, id) => Navigator.currentHistory.createHref("~/workflow/activity/" + id),
        onNavigate: (entityOrPack, options) => navigateCase(isEntityPack(entityOrPack) ? entityOrPack.entity : entityOrPack, options && options.readOnly),
        onView: (entityOrPack, options) => viewCase(isEntityPack(entityOrPack) ? entityOrPack.entity : entityOrPack, options && options.readOnly),
    }));

    Operations.addSettings(new EntityOperationSettings(CaseOperation.SetTags, { isVisible: ctx => false }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Register, { hideOnCanExecute: true, style: "primary" }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Delete, { hideOnCanExecute: true, isVisible: ctx => false, contextual: { isVisible: ctx => true } }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Undo, { hideOnCanExecute: true, style: "danger" }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Jump, { onClick: executeWorkflowJump, contextual: { isVisible: ctx => true, onClick: executeWorkflowJumpContextual } }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Reject, { contextual: { isVisible: ctx => true } }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Timeout, { isVisible: ctx => false }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.MarkAsUnread, { hideOnCanExecute: true, isVisible: ctx => false, contextual: { isVisible: ctx => true } }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.ScriptExecute, { isVisible: ctx => false }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.ScriptFailureJump, { isVisible: ctx => false }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.ScriptScheduleRetry, { isVisible: ctx => false }));
    caseActivityOperation(CaseActivityOperation.Next, "primary");
    caseActivityOperation(CaseActivityOperation.Approve, "success");
    caseActivityOperation(CaseActivityOperation.Decline, "warning");

    Operations.addSettings(new EntityOperationSettings(WorkflowOperation.Save, { style: "primary", onClick: executeWorkflowSave }));
    Operations.addSettings(new EntityOperationSettings(WorkflowOperation.Delete, { contextualFromMany: { isVisible: ctx => false } }));
    Navigator.addSettings(new EntitySettings(WorkflowEntity, w => new ViewPromise(m => require(['./Workflow/Workflow'], m)), { avoidPopup: true }));

    hide(WorkflowPoolEntity);
    hide(WorkflowLaneEntity);
    hide(WorkflowActivityEntity);
    hide(WorkflowGatewayEntity);
    hide(WorkflowEventEntity);
    hide(WorkflowConnectionEntity);

    Navigator.addSettings(new EntitySettings(WorkflowActivityModel, w => new ViewPromise(m => require(['./Workflow/WorkflowActivityModel'], m))));
    Navigator.addSettings(new EntitySettings(WorkflowConnectionModel, w => new ViewPromise(m => require(['./Workflow/WorkflowConnectionModel'], m))));
    Navigator.addSettings(new EntitySettings(WorkflowReplacementModel, w => new ViewPromise(m => require(['./Workflow/WorkflowReplacementComponent'], m))));
    Navigator.addSettings(new EntitySettings(WorkflowConditionEntity, w => new ViewPromise(m => require(['./Workflow/WorkflowCondition'], m))));
    Navigator.addSettings(new EntitySettings(WorkflowActionEntity, w => new ViewPromise(m => require(['./Workflow/WorkflowAction'], m))));
    Navigator.addSettings(new EntitySettings(WorkflowScriptEntity, w => new ViewPromise(m => require(['./Workflow/WorkflowScript'], m))));
    Navigator.addSettings(new EntitySettings(WorkflowLaneModel, w => new ViewPromise(m => require(['./Workflow/WorkflowLaneModel'], m))));
    Navigator.addSettings(new EntitySettings(WorkflowEventModel, w => new ViewPromise(m => require(['./Workflow/WorkflowEventModel'], m))));
    Navigator.addSettings(new EntitySettings(WorkflowEventTaskEntity, w => new ViewPromise(m => require(['./Workflow/WorkflowEventTask'], m))));

    Constructor.registerConstructor(WorkflowEntity, () => WorkflowEntity.New({ mainEntityStrategy: WorkflowMainEntityStrategy.value("CreateNew") }));
    Constructor.registerConstructor(WorkflowConditionEntity, () => WorkflowConditionEntity.New({ eval: WorkflowConditionEval.New() }));
    Constructor.registerConstructor(WorkflowActionEntity, () => WorkflowActionEntity.New({ eval: WorkflowActionEval.New() }));
    Constructor.registerConstructor(WorkflowScriptEntity, () => WorkflowScriptEntity.New({ eval: WorkflowScriptEval.New() }));
    Constructor.registerConstructor(WorkflowTimeoutEmbedded, () => Constructor.construct(TimeSpanEmbedded).then(ep => ep && WorkflowTimeoutEmbedded.New({ timeout: ep.entity })));

    registerCustomContexts();
}

function registerCustomContexts() {

    function addActx(cc: CodeContext) {
        if (!cc.assignments["actx"]) {
            cc.assignments["actx"] = "getCaseActivityContext(ctx)";
            cc.imports.push("import { getCaseActivityContext } as WorkflowClient from '../../../../Extensions/Signum.React.Extensions/Workflow/WorkflowClient'");
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

function caseActivityOperation(operation: ExecuteSymbol<CaseActivityEntity>, style: Operations.BsStyle) {
    Operations.addSettings(new EntityOperationSettings(operation, {
        hideOnCanExecute: true,
        style: style,
        onClick: executeAndClose,
        contextual: { isVisible: ctx => true },
        contextualFromMany: { isVisible: ctx => true },
    }));
}

function hide<T extends Entity>(type: Type<T>) {
    Navigator.addSettings(new EntitySettings(type, undefined, { isNavigable: "Never", isViewable: false, isCreable: "Never" }));
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

    var jumps = eoc.entity.workflowActivity.jumps;

    getWorkflowJumpSelector(jumps)
        .then(dest => dest && eoc.defaultClick(dest.to))
        .done();
}

function getWorkflowJumpSelector(jumps: MListElement<WorkflowJumpEmbedded>[]): Promise<WorkflowJumpEmbedded | undefined> {

    var opts = jumps.map(j => j.element);
    return SelectorModal.chooseElement(opts,
        {
            display: a => a.to!.toStr || "",
            title: WorkflowActivityMessage.ChooseADestinationForWorkflowJumping.niceToString(),
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

    return new Promise<void>((resolve, reject) => {
        require(["./Case/CaseFrameModal"], function (NP: { default: typeof CaseFrameModal }) {
            NP.default.openNavigate(entityOrPack, readOnly).then(resolve, reject);
        });
    });
}

export function viewCase(entityOrPack: Lite<CaseActivityEntity> | CaseActivityEntity | CaseEntityPack, readOnly?: boolean): Promise<CaseActivityEntity> {

    return new Promise<CaseActivityEntity>((resolve, reject) => {
        require(["./Case/CaseFrameModal"], function (NP: { default: typeof CaseFrameModal }) {
            NP.default.openView(entityOrPack, readOnly).then(resolve, reject);
        });
    });
}

export function createNewCase(workflowId: number | string, mainEntityStrategy: WorkflowMainEntityStrategy): Promise<CaseEntityPack | undefined> {
    return Navigator.API.fetchEntity(WorkflowEntity, workflowId)
        .then(wf => {
            if (mainEntityStrategy == "SelectByUser")
                return Finder.find({ queryName: wf.mainEntityType!.cleanName })
                    .then(lite => {
                        if (!lite)
                            return undefined;

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
        return Promise.resolve(entityOrEntityPack);

    const lite = isEntity(entityOrEntityPack) ? toLite(entityOrEntityPack) : entityOrEntityPack as Lite<CaseActivityEntity>;

    return API.fetchActivityForViewing(lite);
}


interface TypeViewDictionary {
    [activityViewName: string]: ActivityViewSettings<ICaseMainEntity>
}

export class ActivityViewSettings<T extends ICaseMainEntity> {
    type: Type<T>

    activityViewName: string;

    getViewPromise: (entity: T) => ViewPromise<T>;

    constructor(type: Type<T>, activityViewName: string, getViewPromise: (entity: T) => ViewPromise<any>) {
        this.type = type;
        this.activityViewName = activityViewName;
        this.getViewPromise = getViewPromise;
    }
}

export const registeredActivityViews: { [typeName: string]: TypeViewDictionary } = {};

export function registerActivityView<T extends ICaseMainEntity>(settings: ActivityViewSettings<T>) {
    const tvDic = registeredActivityViews[settings.type.typeName] || (registeredActivityViews[settings.type.typeName] = {});

    tvDic[settings.activityViewName] = settings;
}

export function getSettings(typeName: string, activityViewName: string): ActivityViewSettings<ICaseMainEntity> | undefined {

    const dict = registeredActivityViews[typeName];

    if (!dict)
        return undefined;

    return dict[activityViewName];
}

export function getViewPromise<T extends ICaseMainEntity>(entity: T, activityViewName: string | undefined | null): ViewPromise<T> {

    var settings = activityViewName && getSettings(entity.Type, activityViewName);
    if (settings)
        return settings.getViewPromise(entity);

    const promise = activityViewName == undefined ?
        DynamicViewClient.createDefaultDynamicView(entity.Type) :
        DynamicViewClient.API.getDynamicView(entity.Type, activityViewName);

    return ViewPromise.flat(promise.then(dv => new ViewPromise(resolve => require(['../../../Extensions/Signum.React.Extensions/Dynamic/View/DynamicViewComponent'], resolve))
        .withProps({ initialDynamicView: dv })));
}

export function getViewNames(typeName: string) {
    return Dic.getKeys(registeredActivityViews[typeName] || {});
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

    export function findMainEntityType(request: { subString: string, count: number }): Promise<Lite<TypeEntity>[]> {
        return ajaxGet<Lite<TypeEntity>[]>({
            url: Navigator.currentHistory.createHref({ pathname: "~/api/workflow/findMainEntityType", query: request })
        });
    }

    export function findNode(request: WorkflowFindNodeRequest): Promise<Lite<IWorkflowNodeEntity>[]> {
        return ajaxPost<Lite<IWorkflowNodeEntity>[]>({ url: "~/api/workflow/findNode" }, request);
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
