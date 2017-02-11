import * as React from 'react'
import { Route, Link } from 'react-router'
import { ifError, Dic } from '../../../Framework/Signum.React/Scripts/Globals';
import { ajaxPost, ajaxGet, ValidationError } from '../../../Framework/Signum.React/Scripts/Services';
import { EntitySettings, ViewPromise } from '../../../Framework/Signum.React/Scripts/Navigator'
import { EntityPack, Lite, toLite, JavascriptMessage, EntityControlMessage, newMListElement, liteKey, getMixin, Entity, ExecuteSymbol } from '../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeEntity } from '../../../Framework/Signum.React/Scripts/Signum.Entities.Basics'
import { Type, PropertyRoute } from '../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../Framework/Signum.React/Scripts/Navigator'
import * as Finder from '../../../Framework/Signum.React/Scripts/Finder'
import { EntityOperationSettings, addSettings } from '../../../Framework/Signum.React/Scripts/Operations'
import * as Operations from '../../../Framework/Signum.React/Scripts/Operations'
import { confirmInNecessary, notifySuccess, defaultExecuteEntity } from '../../../Framework/Signum.React/Scripts/Operations/EntityOperations'
import { TypeContext } from '../../../Framework/Signum.React/Scripts/Lines'

import { UserEntity } from '../../../Extensions/Signum.React.Extensions/Authorization/Signum.Entities.Authorization'
import * as DynamicViewClient from '../../../Extensions/Signum.React.Extensions/Dynamic/DynamicViewClient'

import { ValueLine, EntityLine, EntityCombo, EntityList, EntityDetail, EntityStrip, EntityRepeater } from '../../../Framework/Signum.React/Scripts/Lines'
import { WorkflowConditionEval, WorkflowActionEval, DecisionResult } from './Signum.Entities.Workflow'

import CaseEntityLink from './Case/CaseEntityLink'
import ActivityWithRemarks from './Case/ActivityWithRemarks'
import CaseModalFrame from './Case/CaseModalFrame'
export { CaseModalFrame };

import CasePageFrame from './Case/CaseModalFrame'
export { CasePageFrame };

import * as Constructor from '../../../Framework/Signum.React/Scripts/Constructor'

import {
    WorkflowEntity, WorkflowLaneEntity, WorkflowActivityEntity, WorkflowConnectionEntity, WorkflowConditionEntity, WorkflowActionEntity, CaseActivityQuery, CaseActivityEntity,
    CaseActivityOperation, CaseEntity, CaseNotificationEntity, CaseNotificationState, InboxFilterModel, WorkflowOperation, WorkflowPoolEntity,
    WorkflowActivityOperation, WorkflowReplacementModel, WorkflowModel, BpmnEntityPair, WorkflowActivityModel, ICaseMainEntity, WorkflowGatewayEntity, WorkflowEventEntity,
    WorkflowLaneModel, WorkflowConnectionModel, IWorkflowNodeEntity
} from './Signum.Entities.Workflow'

import InboxFilter from './Case/InboxFilter'
import Workflow from './Workflow/Workflow'

export function start(options: { routes: JSX.Element[] }) {
    
    options.routes.push(<Route path="workflow">
        <Route path="activity/:caseActivityId" getComponent={(loc, cb) => require(["./Case/CasePageFrame"], (Comp) => cb(null, Comp.default)) } />
        <Route path="new/:workflowId" getComponent={(loc, cb) => require(["./Case/CasePageFrame"], (Comp) => cb(null, Comp.default)) } />
    </Route>);

    Finder.addSettings({
        queryName: CaseActivityQuery.Inbox,
        hiddenColumns: [
            { columnName: "State" },
        ],
        entityFormatter: (row, columns, sc) => <CaseEntityLink lite={row.entity as Lite<CaseActivityEntity>} inSearch={true} onNavigated={sc && sc.handleOnNavigated}>{EntityControlMessage.View.niceToString()}</CaseEntityLink>,
        onDoubleClick: (e, row) => navigateCase(row.entity as Lite<CaseActivityEntity>),
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
        simpleFilterBuilder: (qd, fo) => {
            var model = InboxFilter.extract(fo);

            if (!model)
                return undefined;

            return <InboxFilter ctx={TypeContext.root(InboxFilterModel, model) }/>;
        }
    });

    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Register, { hideOnCanExecute: true, style: "primary" }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Delete, { hideOnCanExecute: true, isVisible: ctx => false, contextual: { isVisible: ctx => true } }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.Undo, { hideOnCanExecute: true, style: "danger" }));
    Operations.addSettings(new EntityOperationSettings(CaseActivityOperation.MarkAsUnread, { hideOnCanExecute: true, isVisible: ctx => false, contextual: { isVisible: ctx => true } }));
    caseActivityOperation(CaseActivityOperation.Next, "primary");
    caseActivityOperation(CaseActivityOperation.Approve, "success");
    caseActivityOperation(CaseActivityOperation.Decline, "warning");
    Operations.addSettings(new EntityOperationSettings(WorkflowOperation.Save, { style: "primary", onClick: executeWorkflowSave }));

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
    Navigator.addSettings(new EntitySettings(WorkflowLaneModel, w => new ViewPromise(m => require(['./Workflow/WorkflowLaneModel'], m))));

    Constructor.registerConstructor(WorkflowConditionEntity, () => WorkflowConditionEntity.New({ eval: WorkflowConditionEval.New() }));
    Constructor.registerConstructor(WorkflowActionEntity, () => WorkflowActionEntity.New({ eval: WorkflowActionEval.New() }));
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

export function executeWorkflowSave(eoc: Operations.EntityOperationContext<Entity>) {

    let wf = eoc.frame.entityComponent as Workflow;
    wf.getXml()
        .then(xml => {
            var model = WorkflowModel.New({
                diagramXml : xml,
                entities: Dic.map(wf.state.entities!, (bpmnId, model) => newMListElement(BpmnEntityPair.New({
                    bpmnElementId: bpmnId,
                    model: model
                })))
            });

            var promise = eoc.entity.isNew ?
                Promise.resolve<PreviewResult | undefined>(undefined) :
                API.previewChanges(eoc.entity.id, model);

            promise.then(pr => {
                if (!pr || pr.Model.replacements.length == 0)
                    defaultExecuteEntity(eoc, model);
                else
                    Navigator.view(pr.Model, { extraComponentProps: { previewTasks: pr.NewTasks } }).then(replacementModel => {
                        if (!replacementModel)
                            return;

                        defaultExecuteEntity(eoc, model, replacementModel);
                    }).done();
            }).done();
        }).done();
}

export function executeAndClose(eoc: Operations.EntityOperationContext<CaseActivityEntity>) {

    if (!confirmInNecessary(eoc))
        return;
    
    Operations.API.executeEntity(eoc.entity, eoc.operationInfo.key)
        .then(pack => { eoc.frame.onClose(); return notifySuccess(); })
        .catch(ifError(ValidationError, e => eoc.frame.setError(e.modelState, "request.entity")))
        .done();
}

export function navigateCase(entityOrPack: Lite<CaseActivityEntity> | CaseActivityEntity | CaseEntityPack, readOnly?: boolean): Promise<void> {

    return new Promise<void>((resolve, reject) => {
        require(["./Case/CaseModalFrame"], function (NP: { default: typeof CaseModalFrame }) {
            NP.default.openNavigate(entityOrPack, readOnly).then(resolve, reject);
        });
    });
} 

export function createNewCase(workflowId: number | string): Promise<CaseEntityPack>{
    return Navigator.API.fetchEntity(WorkflowEntity, workflowId)
        .then(wf => Operations.API.constructFromEntity(wf, CaseActivityOperation.CreateCaseFromWorkflow))
        .then(ep => ({
            activity: ep.entity,
            canExecuteActivity: ep.canExecute,
            canExecuteMainEntity: {}
        }) as CaseEntityPack);
}

export function toEntityPackWorkflow(entityOrEntityPack: Lite<CaseActivityEntity> | CaseActivityEntity | CaseEntityPack): Promise<CaseEntityPack> {
    if ((entityOrEntityPack as CaseEntityPack).canExecuteActivity)
        return Promise.resolve(entityOrEntityPack);

    const id = (entityOrEntityPack as CaseActivityEntity | Lite<CaseActivityEntity>).id!;

    return API.fetchActivityForViewing(id);
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
    export function fetchActivityForViewing(caseActivityId: string | number): Promise<CaseEntityPack> {
        return ajaxGet<CaseEntityPack>({ url: `~/api/workflow/fetchForViewing/${caseActivityId}` });
    }

    export function starts(): Promise<Array<Lite<WorkflowEntity>>> {
        return ajaxGet<Array<Lite<WorkflowEntity>>>({ url: `~/api/workflow/starts` });
    }

    export function getWorkflowModel(workflowId: string | number): Promise<WorkflowModel> {
        return ajaxGet<WorkflowModel>({ url: `~/api/workflow/workflowModel/${workflowId} ` });
    }

    export function previewChanges(workflowId: string | number, model: WorkflowModel): Promise<PreviewResult> {
        return ajaxPost<PreviewResult>({ url: `~/api/workflow/previewChanges/${workflowId} ` }, model);
    }

    export function findMainEntityType(request: { subString: string, count: number }): Promise<Lite<TypeEntity>[]> {
        return ajaxGet<Lite<TypeEntity>[]>({
            url: Navigator.currentHistory.createHref({ pathname: "~/api/workflow/findMainEntityType", query: request })
        });
    }

    export function findNode(request: { workflowId: string | number, subString: string, count: number }): Promise<Lite<IWorkflowNodeEntity>[]> {
        return ajaxGet<Lite<IWorkflowNodeEntity>[]>({
            url: Navigator.currentHistory.createHref({ pathname: "~/api/workflow/findNode", query: request })
        });
    }

    export function conditionTest(request: WorkflowConditionTestRequest): Promise<WorkflowConditionTestResponse> {
        return ajaxPost<WorkflowConditionTestResponse>({ url: `~/api/workflow/condition/test` }, request);
    }
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
}

export interface CaseEntityPack {
    activity: CaseActivityEntity;
    canExecuteActivity: { [key: string]: string };
    canExecuteMainEntity: { [key: string]: string };
}
