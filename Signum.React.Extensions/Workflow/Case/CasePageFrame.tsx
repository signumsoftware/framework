
import * as React from 'react'
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals'
import { TypeContext, StyleOptions, EntityFrame  } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { TypeInfo, getTypeInfo, parseId, GraphExplorer, PropertyRoute, ReadonlyBinding, } from '../../../../Framework/Signum.React/Scripts/Reflection'
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator'
import * as Operations from '../../../../Framework/Signum.React/Scripts/Operations'
import { EntityPack, Entity, Lite, JavascriptMessage, entityInfo, getToString } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { renderWidgets, renderEmbeddedWidgets, WidgetContext } from '../../../../Framework/Signum.React/Scripts/Frames/Widgets'
import ValidationErrors from '../../../../Framework/Signum.React/Scripts/Frames/ValidationErrors'
import ButtonBar from '../../../../Framework/Signum.React/Scripts/Frames/ButtonBar'
import { CaseActivityEntity, WorkflowEntity, ICaseMainEntity, CaseActivityOperation } from '../Signum.Entities.Workflow'
import * as WorkflowClient from '../WorkflowClient'
import CaseFromSenderInfo from './CaseFromSenderInfo'
import CaseButtonBar from './CaseButtonBar'

require("!style!css!../../../../Framework/Signum.React/Scripts/Frames/Frames.css");
require("!style!css!./Case.css");

interface CasePageFrameProps extends ReactRouter.RouteComponentProps<{}, { workflowId: string; caseActivityId?: string }> {
}

interface CasePageFrameState {
    pack?: WorkflowClient.CaseEntityPack;
    getComponent?: (ctx: TypeContext<Entity>) => React.ReactElement<any>;
}

export default class CasePageFrame extends React.Component<CasePageFrameProps, CasePageFrameState> {

    constructor(props: any) {
        super(props);
        this.state = this.calculateState(props);

    }

    getCaseActivity() {
        return this.state.pack && this.state.pack.activity;
    }

    componentWillMount() {
        this.load(this.props);
    }
    
    calculateState(props: CasePageFrameProps) {
        return { getComponent: undefined } as CasePageFrameState;
    }
    
    componentWillReceiveProps(newProps: CasePageFrameProps) {
        this.setState(this.calculateState(newProps), () => {
            this.load(newProps);
        });
    }

    load(props: CasePageFrameProps) {
        this.loadEntity(props)
            .then(() => this.loadComponent())
            .done();
    }

    loadEntity(props: CasePageFrameProps): Promise<void> {

        const routeParams = props.routeParams!;
        if (routeParams.caseActivityId) {
            return WorkflowClient.API.fetchActivityForViewing(routeParams.caseActivityId!)
                .then(pack => this.setState({ pack: pack  }));

        } else if (routeParams.workflowId) {
            const ti = getTypeInfo(WorkflowEntity);
            return WorkflowClient.createNewCase(parseId(ti, routeParams.workflowId))
                .then(pack => this.setState({ pack }));

        } else
            throw new Error("No caseActivityId or workflowId set");
    }

    loadComponent(): Promise<void> {
        const a = this.state.pack!.activity;
        if (a.workflowActivity) {
            return WorkflowClient.getViewPromise(a.case.mainEntity, a.workflowActivity.viewName).promise
                .then(c => this.setState({ getComponent: c }));
        }
        else {
            return Navigator.getViewPromise(a.case.mainEntity).promise
                .then(c => this.setState({ getComponent: c }));
        }
    }

    onClose() {
        Navigator.currentHistory.push(WorkflowClient.getDefaultInboxUrl());
    }

    entityComponent: React.Component<any, any>;

    setComponent(c: React.Component<any, any>) {
        if (c && this.entityComponent != c) {
            this.entityComponent = c;
            this.forceUpdate();
        }
    }

    render() {
        
        if (!this.state.pack) {
            return (
                <div className="normal-control">
                    {this.renderTitle() }
                </div>
            );
        }

        var pack = this.state.pack;
        
        const activityFrame: EntityFrame<CaseActivityEntity> = {
            frameComponent: this,
            entityComponent: this.entityComponent,
            onReload: newPack => {
                if (pack.activity.isNew && !newPack.entity.isNew)
                    Navigator.currentHistory.push("~/workflow/activity/" + newPack.entity.id);
                else {
                    pack.activity = newPack.entity;
                    pack.canExecuteActivity = newPack.canExecute;
                    this.forceUpdate();
                }
            },
            onClose: () => this.onClose(),
            revalidate: () => { throw new Error("Not implemented"); },
            setError: (ms, initialPrefix) => {
                GraphExplorer.setModelState(pack.activity, ms, initialPrefix || "");
                this.forceUpdate()
            },
        };

     
        var activityPack = { entity: pack.activity, canExecute: pack.canExecuteActivity };
      
        return (
            <div className="normal-control">
                {this.renderTitle()}     
                <CaseFromSenderInfo current={pack.activity} />         
                <div className="sf-main-control form-horizontal" data-test-ticks={new Date().valueOf() } data-activity-entity={entityInfo(pack.activity) }>
                    {this.renderMainEntity() }
                </div>
                {this.entityComponent && <CaseButtonBar frame={activityFrame} pack={activityPack} />}
            </div>
        );
    }

    renderTitle() {

        if (!this.state.pack)
            return <h3>{JavascriptMessage.loading.niceToString() }</h3>;

        const activity = this.state.pack.activity;

        return (
            <h3>
                <span className="sf-entity-title">{ getToString(activity) }</span>
                <br/>
                <small className="sf-type-nice-name">{Navigator.getTypeTitle(activity, undefined)}</small>
            </h3>
        );
    }

    validationErrors: ValidationErrors;

    getMainTypeInfo(): TypeInfo {
        return getTypeInfo(this.state.pack!.activity.case.mainEntity.Type);
    }

    renderMainEntity() {

        var pack = this.state.pack!;
        var mainEntity = pack.activity.case.mainEntity;
        const mainFrame: EntityFrame<ICaseMainEntity> = {
            frameComponent: this,
            entityComponent: this.entityComponent,
            onReload: newPack => {
                pack.activity.case.mainEntity = newPack.entity;
                pack.canExecuteMainEntity = newPack.canExecute;
                this.forceUpdate();
            },
            onClose: () => this.onClose(),
            revalidate: () => this.validationErrors && this.validationErrors.forceUpdate(),
            setError: (ms, initialPrefix) => {
                GraphExplorer.setModelState(mainEntity, ms, initialPrefix || "");
                this.forceUpdate()
            },
        };

        var ti = this.getMainTypeInfo();

        const styleOptions: StyleOptions = {
            readOnly: Navigator.isReadOnly(ti) || Boolean(pack.activity.doneDate),
            frame: mainFrame
        };

        const ctx = new TypeContext<ICaseMainEntity>(undefined, styleOptions, PropertyRoute.root(ti), new ReadonlyBinding(mainEntity, ""));

        var mainPack = { entity: mainEntity, canExecute: pack.canExecuteMainEntity };

        const wc: WidgetContext = {
            ctx: ctx,
            pack: mainPack,
        };

        return (
            <div className="sf-main-entity case-main-entity" data-main-entity={entityInfo(mainEntity) }>
                { renderWidgets(wc) }
                { this.entityComponent && !mainEntity.isNew && !pack.activity.doneBy && <ButtonBar frame={mainFrame} pack={mainPack} /> }
                <ValidationErrors entity={mainEntity} ref={ve => this.validationErrors = ve}/>
                {this.state.getComponent && React.cloneElement(this.state.getComponent(ctx), { ref: (c: React.Component<any, any>) => this.setComponent(c) })}
            </div>
        );
    }
   
}