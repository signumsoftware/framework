import * as moment from 'moment'
import * as numbro from 'numbro'
import * as React from 'react'
import { Modal, ModalProps, ModalClass, ButtonToolbar, Tabs, Tab } from 'react-bootstrap'
import { openModal, IModalProps } from '../../../../Framework/Signum.React/Scripts/Modals';
import * as Finder from '../../../../Framework/Signum.React/Scripts/Finder';
import * as Navigator from '../../../../Framework/Signum.React/Scripts/Navigator';
import * as Operations from '../../../../Framework/Signum.React/Scripts/Operations';
import { Dic } from '../../../../Framework/Signum.React/Scripts/Globals';
import { JavascriptMessage, toLite } from '../../../../Framework/Signum.React/Scripts/Signum.Entities'
import { TypeInfo, TypeReference, Binding } from '../../../../Framework/Signum.React/Scripts/Reflection'
import { FormGroupStyle, TypeContext } from '../../../../Framework/Signum.React/Scripts/TypeContext'
import { ValueLineType, ValueLine } from '../../../../Framework/Signum.React/Scripts/Lines/ValueLine'
import { WorkflowActivityStats } from "../WorkflowClient";
import { FormGroup, StyleContext, FormControlStatic } from "../../../../Framework/Signum.React/Scripts/Lines";
import { WorkflowActivityEntity, WorkflowActivityMessage, CaseNotificationEntity, WorkflowActivityType, WorkflowOperation, WorkflowEntity, WorkflowActivityModel, WorkflowActivityMonitorMessage, CaseActivityEntity } from "../Signum.Entities.Workflow";
import { SearchControl, ColumnOption, FilterOption } from "../../../../Framework/Signum.React/Scripts/Search";
import * as WorkflowClient from '../WorkflowClient';
import { WorkflowActivityMonitorConfig } from './WorkflowActivityMonitorPage';


interface WorkflowActivityStatsModalProps extends React.Props<WorkflowActivityStatsModal>, IModalProps {
    stats: WorkflowActivityStats;
    config: WorkflowActivityMonitorConfig;
    activity: WorkflowActivityModel;
}

export default class WorkflowActivityStatsModal extends React.Component<WorkflowActivityStatsModalProps, { show: boolean }>  {

    constructor(props: WorkflowActivityStatsModalProps) {
        super(props);

        this.state = {
            show: true,
        };
    }

    handleCloseClicked = () => {
        this.setState({ show: false });
    }

    handleOnExited = () => {
        this.props.onExited!(undefined);
    }

    render() {

        var ctx = new StyleContext(undefined, { labelColumns: 3 });
        var activity = this.props.activity;
        var config = this.props.config;
        var stats = this.props.stats;
        return <Modal bsSize="lg" onHide={this.handleCloseClicked} show={this.state.show} onExited={this.handleOnExited}>

            <Modal.Header closeButton={true}>
                <h4 className="modal-title">
                    {stats.WorkflowActivity.toStr}
                </h4>
            </Modal.Header>

            <Modal.Body>
                {
                    <div className="form-horizontal">
                        <FormGroup ctx={ctx} labelText={CaseActivityEntity.nicePluralName()}><FormControlStatic ctx={ctx}>{stats.CaseActivityCount}</FormControlStatic></FormGroup>
                        {config.columns.map((col, i) =>
                            <FormGroup ctx={ctx} labelText={col.displayName || col.token!.niceName}><FormControlStatic ctx={ctx}>{stats.CustomValues[i]}</FormControlStatic></FormGroup>
                        )}
                        {activity.type == "CallWorkflow" || activity.type == "DecompositionWorkflow" ?
                            this.renderSubWorkflowExtra(ctx) :
                            this.renderTaskExtra()
                        }
                    </div>
                }
            </Modal.Body>
            <Modal.Footer>
                <button className="btn btn-primary sf-entity-button sf-ok-button" onClick={this.handleCloseClicked}>
                    {JavascriptMessage.ok.niceToString()}
                </button>
            </Modal.Footer>
        </Modal>;
    }

    renderTaskExtra() {
        var stats = this.props.stats;

        return (
            <div>
                <h3>{CaseActivityEntity.nicePluralName()}</h3>
                <SearchControl
                    showGroupButton={true}
                    findOptions={{
                        queryName: CaseActivityEntity,
                        parentColumn: "Entity.WorkflowActivity",
                        parentValue: stats.WorkflowActivity,
                        filterOptions: this.props.config.filters
                            .filter(f => f.token && f.token.queryTokenType != "Aggregate")
                            .map(a => ({ columnName: a.token!.fullKey, operation: a.operation, value: a.value, frozen: true }) as FilterOption),
                        columnOptionsMode: "Add",
                        columnOptions: this.props.config.columns
                                .filter(c => c.token && c.token.fullKey.contains("."))
                                .map(c => ({ columnName: c.token!.fullKey.beforeLast(".") }) as ColumnOption),
                    }} />
            </div>
        );
    }

    renderSubWorkflowExtra(ctx: StyleContext) {
        var stats = this.props.stats;

        return (
            <FormGroup ctx={ctx}>
                <button className="btn btn-default" onClick={this.handleClick}>
                    <i className="fa fa-tachometer" style={{ color: "green" }} /> {WorkflowActivityMonitorMessage.WorkflowActivityMonitor.niceToString()}
                </button>
            </FormGroup>
        );
    }

    handleClick = (e: React.MouseEvent<HTMLButtonElement>) => {
        e.preventDefault();
        
        Navigator.API.fetchAndForget(this.props.stats.WorkflowActivity)
            .then(wa => window.open(WorkflowClient.workflowActivityMonitorUrl(toLite(wa.subWorkflow!.workflow!))))
            .done();
    }

    static show(stats: WorkflowActivityStats, config: WorkflowActivityMonitorConfig, activity: WorkflowActivityModel): Promise<any> {
        return openModal<any>(<WorkflowActivityStatsModal stats={stats} config={config} activity={activity} />);
    }
}

function formatDuration(duration: number | undefined, unit: string | undefined) {
    if (duration == undefined)
        return undefined;

    if (unit == "min")


    var unit = WorkflowActivityEntity.memberInfo(a => a.estimatedDuration).unit;

    return <span>{numbro(duration).format("0.00")} {unit} <mark>({moment.duration(duration, "minutes").format("d[d] h[h] m[m] s[s]")})</mark></span>
}




